using LogisticaUltimaMilla.Infrastructure.Migrations;
using LogisticaUltimaMilla.Infrastructure.Seeding;
using Npgsql;

namespace LogisticaUltimaMilla.Api.Startup;

/// <summary>
/// Prepara la base de datos al iniciar, según la configuración:
/// aplica las migraciones pendientes y, solo en desarrollo, carga los datos de demostración.
/// En un entorno real las migraciones se ejecutan como un paso controlado del despliegue.
/// </summary>
public sealed class DatabaseInitializer : IHostedService
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        NpgsqlDataSource dataSource,
        IConfiguration configuration,
        ILogger<DatabaseInitializer> logger)
    {
        _dataSource = dataSource;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_configuration.GetValue<bool>("Database:MigrateOnStartup"))
        {
            var applied = await new MigrationRunner(_dataSource).MigrateAsync(cancellationToken);
            _logger.LogInformation("Migraciones aplicadas al iniciar: {AppliedCount}", applied.Count);
        }

        if (_configuration.GetValue<bool>("Database:SeedDemoData"))
        {
            await new DemoDataSeeder(_dataSource).SeedAsync(cancellationToken);
            _logger.LogInformation("Datos de demostración cargados");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
