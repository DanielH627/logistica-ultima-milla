using LogisticaUltimaMilla.Infrastructure.Migrations;
using Npgsql;

namespace LogisticaUltimaMilla.Integration.Tests.Postgres;

/// <summary>
/// Base de datos temporal para una prueba. Cada clase de prueba crea la suya, con un nombre único,
/// y la elimina al terminar: las pruebas no dependen unas de otras ni dejan datos.
/// Se conecta con la cadena indicada en la variable de entorno LOGISTICA_TEST_POSTGRES.
/// </summary>
public sealed class TestDatabase : IAsyncDisposable
{
    public const string EnvironmentVariable = "LOGISTICA_TEST_POSTGRES";

    private readonly string _adminConnectionString;

    private TestDatabase(string adminConnectionString, string name, NpgsqlDataSource dataSource)
    {
        _adminConnectionString = adminConnectionString;
        Name = name;
        DataSource = dataSource;
    }

    public string Name { get; }

    public NpgsqlDataSource DataSource { get; }

    public static string? ConnectionStringFromEnvironment() =>
        Environment.GetEnvironmentVariable(EnvironmentVariable);

    public static async Task<TestDatabase> CreateAsync(bool applyMigrations)
    {
        var admin = ConnectionStringFromEnvironment()
            ?? throw new InvalidOperationException(
                $"Defina la variable de entorno {EnvironmentVariable} para ejecutar las pruebas con PostgreSQL.");

        var name = $"logistica_test_{Guid.NewGuid():N}";

        await using (var adminDataSource = NpgsqlDataSource.Create(admin))
        {
            await using var create = adminDataSource.CreateCommand($"CREATE DATABASE {name}");
            await create.ExecuteNonQueryAsync();
        }

        // Se limita el grupo de conexiones para que las pruebas en paralelo no agoten las conexiones del servidor.
        var builder = new NpgsqlConnectionStringBuilder(admin) { Database = name, MaxPoolSize = 20 };
        var dataSource = NpgsqlDataSource.Create(builder.ConnectionString);

        if (applyMigrations)
        {
            await new MigrationRunner(dataSource).MigrateAsync();
        }

        return new TestDatabase(admin, name, dataSource);
    }

    public async ValueTask DisposeAsync()
    {
        await DataSource.DisposeAsync();

        await using var adminDataSource = NpgsqlDataSource.Create(_adminConnectionString);
        await using var drop = adminDataSource.CreateCommand($"DROP DATABASE IF EXISTS {Name} WITH (FORCE)");
        await drop.ExecuteNonQueryAsync();
    }
}
