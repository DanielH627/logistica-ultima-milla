using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using LogisticaUltimaMilla.Integration.Tests.Api;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LogisticaUltimaMilla.Integration.Tests.Architecture;

/// <summary>Reglas de seguridad que se verifican automáticamente: un descuido nuevo rompe la compilación.</summary>
public class SecurityRulesTests
{
    [Fact]
    public void Sql_IsNeverBuiltWithStringInterpolationInProductionCode()
    {
        // Todo el acceso a datos usa parámetros. Una cadena interpolada que empiece con una sentencia SQL es sospechosa.
        var interpolatedSql = new Regex(
            "\\$@?\"\"?\"?\\s*(SELECT|INSERT|UPDATE|DELETE|CREATE|DROP|ALTER|TRUNCATE|WITH)\\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        var offending = SourceTree.CodeIn("src")
            .Where(source => interpolatedSql.IsMatch(source.Code))
            .Select(source => SourceTree.RelativeName(source.File))
            .ToList();

        Assert.True(offending.Count == 0,
            $"SQL construido con interpolación (use parámetros): {string.Join(", ", offending)}.");
    }

    [Fact]
    public void ProductionCode_DoesNotContainCredentialsLiterals()
    {
        string[] forbidden = ["Password=", "Pwd=", "api_key", "secret="];

        var offending = SourceTree.CodeIn("src")
            .Where(source => forbidden.Any(text => source.Code.Contains(text, StringComparison.OrdinalIgnoreCase)))
            .Select(source => SourceTree.RelativeName(source.File))
            .ToList();

        Assert.True(offending.Count == 0,
            $"Credenciales escritas en el código: {string.Join(", ", offending)}.");
    }

    [Fact]
    public void BaseConfiguration_DoesNotContainSecrets()
    {
        // La configuración base se versiona y se despliega: los secretos vienen del entorno o de un gestor de secretos.
        var settings = File.ReadAllText(SourceTree.Path("src", "Api", "appsettings.json"));

        Assert.DoesNotContain("Password", settings, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ApiKey", settings, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ConnectionStrings", settings, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EveryEndpointOutsideHealth_RejectsRequestsWithoutAnApiKey()
    {
        // Un endpoint nuevo sin protección hace fallar esta prueba. No necesita base de datos:
        // la clave se verifica antes de ejecutar cualquier acción.
        const string unreachable = "Host=localhost;Port=1;Username=postgres;Password=x;Database=logistica;Timeout=2";
        using var factory = new ApiFactory(unreachable, prepareDatabase: false);
        using var client = factory.CreateClient();

        var endpoints = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText is { } route
                && !route.StartsWith("/health", StringComparison.Ordinal))
            .Select(endpoint => (
                Route: endpoint.RoutePattern.RawText!,
                Method: endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.FirstOrDefault()))
            .Where(endpoint => endpoint.Method is not null)
            .ToList();

        Assert.NotEmpty(endpoints);

        var unprotected = new List<string>();
        foreach (var (route, method) in endpoints)
        {
            var path = Regex.Replace(route, "\\{[^}]+\\}", _ => Guid.NewGuid().ToString());

            using var request = new HttpRequestMessage(new HttpMethod(method!), path);
            if (method != "GET")
            {
                request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
            }

            var response = await client.SendAsync(request);
            if (response.StatusCode != HttpStatusCode.Unauthorized)
            {
                unprotected.Add($"{method} {route} respondió {(int)response.StatusCode}");
            }
        }

        Assert.True(unprotected.Count == 0,
            $"Endpoints sin clave de API: {string.Join("; ", unprotected)}.");
    }
}
