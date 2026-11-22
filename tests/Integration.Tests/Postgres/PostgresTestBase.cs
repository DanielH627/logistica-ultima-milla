using Npgsql;
using Xunit;

namespace LogisticaUltimaMilla.Integration.Tests.Postgres;

/// <summary>Base de las pruebas con PostgreSQL: crea una base temporal y la elimina al terminar.</summary>
public abstract class PostgresTestBase : IAsyncLifetime
{
    private TestDatabase? _database;

    protected virtual bool ApplyMigrations => true;

    protected NpgsqlDataSource DataSource =>
        _database?.DataSource ?? throw new InvalidOperationException("La base de datos de prueba no está disponible.");

    public async Task InitializeAsync()
    {
        if (TestDatabase.ConnectionStringFromEnvironment() is null)
        {
            return;
        }

        _database = await TestDatabase.CreateAsync(ApplyMigrations);
    }

    public async Task DisposeAsync()
    {
        if (_database is not null)
        {
            await _database.DisposeAsync();
        }
    }
}
