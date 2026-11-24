using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace LogisticaUltimaMilla.Integration.Tests.Api;

/// <summary>
/// Arranca la aplicación completa en memoria, apuntando a una base de datos de prueba.
/// Usa el entorno "Testing" para no cargar la configuración de desarrollo.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string ApiKey = "clave-de-prueba";

    private readonly string _connectionString;
    private readonly bool _prepareDatabase;

    public ApiFactory(string connectionString, bool prepareDatabase)
    {
        _connectionString = connectionString;
        _prepareDatabase = prepareDatabase;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Logistica"] = _connectionString,
                ["Security:ApiKey"] = ApiKey,
                ["Database:MigrateOnStartup"] = _prepareDatabase.ToString(),
                ["Database:SeedDemoData"] = _prepareDatabase.ToString()
            });
        });
    }
}
