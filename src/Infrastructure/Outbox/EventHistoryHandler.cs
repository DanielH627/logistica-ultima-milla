using LogisticaUltimaMilla.Infrastructure.Postgres;
using Npgsql;

namespace LogisticaUltimaMilla.Infrastructure.Outbox;

/// <summary>
/// Registra cada mensaje en el historial de eventos (trazabilidad, R04). Es idempotente:
/// la restricción UNIQUE sobre message_id hace que procesar el mismo mensaje dos veces deje una sola fila.
/// </summary>
public sealed class EventHistoryHandler : IOutboxMessageHandler
{
    private readonly NpgsqlDataSource _dataSource;

    public EventHistoryHandler(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public string MessageType => OutboxEventSerializer.DeliveryAssignedV1;

    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            """
            INSERT INTO transversal.historial_eventos (id, message_id, aggregate_id, event_type, payload, occurred_at)
            VALUES (@id, @message_id, @aggregate_id, @event_type, @payload, @occurred_at)
            ON CONFLICT (message_id) DO NOTHING
            """);
        command.AddUuid("id", Guid.NewGuid());
        command.AddUuid("message_id", message.Id);
        command.AddUuid("aggregate_id", message.AggregateId);
        command.AddText("event_type", message.MessageType);
        command.AddJsonb("payload", message.Payload);
        command.AddTimestampTz("occurred_at", message.OccurredAt);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
