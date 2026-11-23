using LogisticaUltimaMilla.Infrastructure.Migrations;
using Npgsql;
using Xunit;

namespace LogisticaUltimaMilla.Integration.Tests.Postgres;

public class MigrationTests : PostgresTestBase
{
    // Estas pruebas parten de una base vacía: la migración la aplica cada prueba.
    protected override bool ApplyMigrations => false;

    private async Task<bool> TableExistsAsync(string qualifiedName)
    {
        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand("SELECT to_regclass(@name) IS NOT NULL", connection);
        command.Parameters.AddWithValue("name", qualifiedName);
        return (bool)(await command.ExecuteScalarAsync())!;
    }

    [RequiresPostgresFact]
    public async Task Migrate_OnEmptyDatabase_AppliesScripts_AndSecondRunAppliesNothing()
    {
        var runner = new MigrationRunner(DataSource);

        var first = await runner.MigrateAsync();
        var second = await runner.MigrateAsync();

        Assert.NotEmpty(first);
        Assert.Empty(second);
        Assert.True(await TableExistsAsync("entregas.entrega"));
        Assert.True(await TableExistsAsync("ruteo.ruta"));
        Assert.True(await TableExistsAsync("ruteo.parada"));
        Assert.True(await TableExistsAsync("transversal.bandeja_salida"));
        Assert.True(await TableExistsAsync("transversal.historial_eventos"));
    }

    [RequiresPostgresFact]
    public async Task Migrate_WhenAnAppliedScriptWasModified_Throws()
    {
        var runner = new MigrationRunner(DataSource);
        await runner.MigrateAsync();

        await using (var connection = await DataSource.OpenConnectionAsync())
        await using (var tamper = new NpgsqlCommand(
            "UPDATE public.historial_migraciones SET checksum = 'ALTERADO' WHERE version = 1",
            connection))
        {
            await tamper.ExecuteNonQueryAsync();
        }

        await Assert.ThrowsAsync<MigrationChecksumMismatchException>(() => runner.MigrateAsync());
    }

    [RequiresPostgresFact]
    public async Task Migrate_RunningTwiceAtTheSameTime_AppliesEachScriptOnce()
    {
        var results = await Task.WhenAll(
            Task.Run(() => new MigrationRunner(DataSource).MigrateAsync()),
            Task.Run(() => new MigrationRunner(DataSource).MigrateAsync()));

        long recorded;
        await using (var connection = await DataSource.OpenConnectionAsync())
        await using (var count = new NpgsqlCommand("SELECT count(*) FROM public.historial_migraciones", connection))
        {
            recorded = (long)(await count.ExecuteScalarAsync())!;
        }

        Assert.Equal(recorded, results.Sum(applied => applied.Count));
    }
}
