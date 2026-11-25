using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LogisticaUltimaMilla.Integration.Tests.Api;

/// <summary>
/// Arranca la aplicación completa en memoria, apuntando a una base de datos de prueba.
/// Usa el entorno "Testing" para no cargar la configuración de desarrollo.
/// El procesador de la bandeja de salida queda desactivado por defecto: así las pruebas controlan cuándo se ejecuta.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string ApiKey = "clave-de-prueba";

    /// <summary>Captura todo lo que la aplicación registra, para comprobar qué se escribe en los logs.</summary>
    public CapturingLoggerProvider Logs { get; } = new();

    private readonly string _connectionString;
    private readonly bool _prepareDatabase;
    private readonly IReadOnlyDictionary<string, string?> _extraSettings;

    public ApiFactory(
        string connectionString,
        bool prepareDatabase,
        IReadOnlyDictionary<string, string?>? extraSettings = null)
    {
        _connectionString = connectionString;
        _prepareDatabase = prepareDatabase;
        _extraSettings = extraSettings ?? new Dictionary<string, string?>();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureLogging(logging => logging.AddProvider(Logs));

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:Logistica"] = _connectionString,
                ["Security:ApiKey"] = ApiKey,
                ["Database:MigrateOnStartup"] = _prepareDatabase.ToString(),
                ["Database:SeedDemoData"] = _prepareDatabase.ToString(),
                ["Outbox:Enabled"] = "false"
            };

            foreach (var (key, value) in _extraSettings)
            {
                settings[key] = value;
            }

            configuration.AddInMemoryCollection(settings);
        });
    }
}
