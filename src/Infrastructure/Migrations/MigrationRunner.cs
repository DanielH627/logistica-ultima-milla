using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Npgsql;

namespace LogisticaUltimaMilla.Infrastructure.Migrations;

/// <summary>Error general durante la migración del esquema.</summary>
public class MigrationException : Exception
{
    public MigrationException(string message) : base(message)
    {
    }

    public MigrationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>Un script ya aplicado fue modificado después de aplicarse.</summary>
public sealed class MigrationChecksumMismatchException : MigrationException
{
    public MigrationChecksumMismatchException(int version, string name)
        : base($"El script V{version:000}__{name} ya fue aplicado y su contenido cambió. " +
               "Un script aplicado no se modifica: se agrega una migración nueva.")
    {
    }
}

/// <summary>
/// Ejecutor de migraciones versionadas y reproducibles.
/// Los scripts se llaman V001__nombre.sql, viajan dentro del ensamblado y se aplican en orden.
/// Cada script se aplica una sola vez, dentro de una transacción, y queda registrado con su huella.
/// Un bloqueo asesor evita que dos instancias migren a la vez.
/// </summary>
public sealed class MigrationRunner
{
    private const long AdvisoryLockKey = 7_272_401;

    private static readonly Regex ScriptNamePattern =
        new(@"Scripts\.V(?<version>\d+)__(?<name>.+)\.sql$", RegexOptions.Compiled);

    private readonly NpgsqlDataSource _dataSource;

    public MigrationRunner(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
    }

    /// <summary>Aplica las migraciones pendientes y devuelve los nombres de las que aplicó en esta ejecución.</summary>
    public async Task<IReadOnlyList<string>> MigrateAsync(CancellationToken cancellationToken = default)
    {
        var scripts = LoadScripts();

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);

        await using (var lockCommand = new NpgsqlCommand("SELECT pg_advisory_lock(@key)", connection))
        {
            lockCommand.Parameters.AddWithValue("key", AdvisoryLockKey);
            await lockCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        try
        {
            await EnsureHistoryTableAsync(connection, cancellationToken);
            var applied = await LoadAppliedAsync(connection, cancellationToken);

            foreach (var version in applied.Keys.Where(v => scripts.All(s => s.Version != v)))
            {
                throw new MigrationException(
                    $"La base de datos tiene aplicada la migración V{version:000}, que este código no contiene.");
            }

            var appliedNow = new List<string>();

            foreach (var script in scripts)
            {
                if (applied.TryGetValue(script.Version, out var storedChecksum))
                {
                    if (!string.Equals(storedChecksum, script.Checksum, StringComparison.Ordinal))
                    {
                        throw new MigrationChecksumMismatchException(script.Version, script.Name);
                    }

                    continue;
                }

                await ApplyAsync(connection, script, cancellationToken);
                appliedNow.Add($"V{script.Version:000}__{script.Name}");
            }

            return appliedNow;
        }
        finally
        {
            await using var unlockCommand = new NpgsqlCommand("SELECT pg_advisory_unlock(@key)", connection);
            unlockCommand.Parameters.AddWithValue("key", AdvisoryLockKey);
            await unlockCommand.ExecuteNonQueryAsync(CancellationToken.None);
        }
    }

    private static async Task EnsureHistoryTableAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS public.historial_migraciones (
                version     integer     PRIMARY KEY,
                nombre      text        NOT NULL,
                checksum    text        NOT NULL,
                aplicada_en timestamptz NOT NULL DEFAULT now()
            )
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<Dictionary<int, string>> LoadAppliedAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken)
    {
        var applied = new Dictionary<int, string>();

        await using var command = new NpgsqlCommand(
            "SELECT version, checksum FROM public.historial_migraciones", connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            applied[reader.GetInt32(0)] = reader.GetString(1);
        }

        return applied;
    }

    private static async Task ApplyAsync(
        NpgsqlConnection connection,
        MigrationScript script,
        CancellationToken cancellationToken)
    {
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using (var command = new NpgsqlCommand(script.Sql, connection, transaction))
            {
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var history = new NpgsqlCommand(
                "INSERT INTO public.historial_migraciones (version, nombre, checksum) VALUES (@version, @name, @checksum)",
                connection,
                transaction))
            {
                history.Parameters.AddWithValue("version", script.Version);
                history.Parameters.AddWithValue("name", script.Name);
                history.Parameters.AddWithValue("checksum", script.Checksum);
                await history.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new MigrationException(
                $"Falló la migración V{script.Version:000}__{script.Name}. No se aplicó ningún cambio de ese script.",
                exception);
        }
    }

    private static List<MigrationScript> LoadScripts()
    {
        var assembly = typeof(MigrationRunner).Assembly;
        var scripts = new List<MigrationScript>();

        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            var match = ScriptNamePattern.Match(resourceName);
            if (!match.Success)
            {
                continue;
            }

            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new MigrationException($"No se pudo leer el script {resourceName}.");
            using var reader = new StreamReader(stream, Encoding.UTF8);

            var sql = reader.ReadToEnd();

            scripts.Add(new MigrationScript(
                int.Parse(match.Groups["version"].Value),
                match.Groups["name"].Value,
                sql,
                ComputeChecksum(sql)));
        }

        var duplicated = scripts.GroupBy(s => s.Version).FirstOrDefault(g => g.Count() > 1);
        if (duplicated is not null)
        {
            throw new MigrationException($"Hay más de un script con la versión V{duplicated.Key:000}.");
        }

        return scripts.OrderBy(s => s.Version).ToList();
    }

    // Los saltos de línea se normalizan para que la huella no dependa del sistema operativo.
    private static string ComputeChecksum(string sql)
    {
        var normalized = sql.Replace("\r\n", "\n");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }

    private sealed record MigrationScript(int Version, string Name, string Sql, string Checksum);
}
