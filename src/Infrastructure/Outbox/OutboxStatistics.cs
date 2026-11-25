using Npgsql;

namespace LogisticaUltimaMilla.Infrastructure.Outbox;

/// <summary>Resumen del estado de la bandeja de salida, para salud y operación.</summary>
public sealed record OutboxStatistics(int Pending, int Sent, int Dead, double? OldestPendingSeconds);

public static class OutboxStatisticsReader
{
    public static async Task<OutboxStatistics> ReadAsync(
        NpgsqlDataSource dataSource, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            """
            SELECT count(*) FILTER (WHERE status = 'Pending'),
                   count(*) FILTER (WHERE status = 'Sent'),
                   count(*) FILTER (WHERE status = 'Dead'),
                   min(occurred_at) FILTER (WHERE status = 'Pending')
            FROM transversal.bandeja_salida
            """);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);

        double? oldest = reader.IsDBNull(3)
            ? null
            : Math.Max(0, (now - reader.GetFieldValue<DateTimeOffset>(3)).TotalSeconds);

        return new OutboxStatistics(
            (int)reader.GetInt64(0), (int)reader.GetInt64(1), (int)reader.GetInt64(2), oldest);
    }
}
