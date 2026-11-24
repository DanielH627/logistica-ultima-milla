using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace LogisticaUltimaMilla.Integration.Tests.Api;

/// <summary>
/// Comprueba cómo responde la API cuando la base de datos no está disponible.
/// No necesita PostgreSQL: apunta a un puerto cerrado a propósito.
/// </summary>
public class ApiFailureTests
{
    [Fact]
    public async Task Post_WhenTheDatabaseIsDown_Returns500WithoutLeakingInternalDetails()
    {
        const string unreachable = "Host=localhost;Port=1;Username=postgres;Password=secreto-de-prueba;Database=logistica;Timeout=2";

        using var factory = new ApiFactory(unreachable, prepareDatabase: false);
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/deliveries/{Guid.NewGuid()}/assignment")
        {
            Content = JsonContent.Create(new { courierId = Guid.NewGuid() })
        };
        request.Headers.Add("X-Api-Key", ApiFactory.ApiKey);
        request.Headers.Add("X-Requested-By", "sandra");

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("traceId", body);

        // La respuesta no revela la cadena de conexión, el servidor ni la excepción original.
        Assert.DoesNotContain("secreto-de-prueba", body);
        Assert.DoesNotContain("localhost", body);
        Assert.DoesNotContain("Npgsql", body);
        Assert.DoesNotContain("Exception", body);
    }
}
