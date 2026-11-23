using LogisticaUltimaMilla.Application.Ports;
using LogisticaUltimaMilla.Domain.Entregas;
using LogisticaUltimaMilla.Domain.Shared;
using LogisticaUltimaMilla.Infrastructure.Outbox;
using LogisticaUltimaMilla.Infrastructure.Postgres;
using Npgsql;

namespace LogisticaUltimaMilla.Infrastructure.Persistence;

/// <summary>
/// Adaptador del puerto IDeliveryRepository sobre PostgreSQL.
/// El dominio no cambia para conectarse a la base: este adaptador solo traduce entre filas y objetos.
/// El control de concurrencia es optimista: se guarda únicamente si la versión no cambió desde la lectura.
/// Los eventos de dominio de la entrega se guardan en la bandeja de salida dentro de la MISMA transacción:
/// o se guardan la entrega y sus eventos, o no se guarda nada.
/// </summary>
public sealed class PostgresDeliveryRepository : IDeliveryRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public PostgresDeliveryRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
    }

    public async Task<Delivery?> GetAsync(DeliveryId id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, order_id, address, window_start, window_end, priority,
                   status, courier_id, assigned_at, version
            FROM entregas.entrega
            WHERE id = @id
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.AddUuid("id", id.Value);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var courierId = reader.IsDBNull(7) ? null : new CourierId(reader.GetGuid(7));
        DateTimeOffset? assignedAt = reader.IsDBNull(8) ? null : reader.GetFieldValue<DateTimeOffset>(8);

        return Delivery.Restore(
            new DeliveryId(reader.GetGuid(0)),
            new OrderId(reader.GetGuid(1)),
            new DeliveryAddress(reader.GetString(2)),
            new DeliveryWindow(
                reader.GetFieldValue<DateTimeOffset>(3),
                reader.GetFieldValue<DateTimeOffset>(4)),
            Enum.Parse<DeliveryPriority>(reader.GetString(5)),
            Enum.Parse<DeliveryStatus>(reader.GetString(6)),
            courierId,
            assignedAt,
            reader.GetInt32(9));
    }

    public async Task SaveAsync(Delivery delivery, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(delivery);

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        if (delivery.Version == 0)
        {
            await InsertAsync(connection, transaction, delivery, cancellationToken);
        }
        else
        {
            await UpdateAsync(connection, transaction, delivery, cancellationToken);
        }

        // Si la entrega no se pudo guardar, la excepción ya salió y esta línea no se ejecuta:
        // los eventos de una operación fallida nunca llegan a la bandeja de salida.
        await InsertOutboxMessagesAsync(connection, transaction, delivery, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task InsertAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Delivery delivery,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO entregas.entrega
                (id, order_id, address, window_start, window_end, priority,
                 status, courier_id, assigned_at, version)
            VALUES
                (@id, @order_id, @address, @window_start, @window_end, @priority,
                 @status, @courier_id, @assigned_at, 1)
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        AddDeliveryParameters(command, delivery);

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            // Otra operación ya creó esta entrega (o un pedido con entrega).
            throw new DeliveryConcurrencyException(delivery.Id);
        }
    }

    private static async Task UpdateAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Delivery delivery,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE entregas.entrega
            SET status = @status,
                courier_id = @courier_id,
                assigned_at = @assigned_at,
                version = version + 1
            WHERE id = @id AND version = @version
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.AddUuid("id", delivery.Id.Value);
        command.AddText("status", delivery.Status.ToString());
        command.AddUuid("courier_id", delivery.CourierId?.Value);
        command.AddTimestampTz("assigned_at", delivery.AssignedAt);
        command.AddInteger("version", delivery.Version);

        var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);

        if (affectedRows == 0)
        {
            // La entrega cambió desde que se leyó (control de concurrencia optimista).
            throw new DeliveryConcurrencyException(delivery.Id);
        }
    }

    private static async Task InsertOutboxMessagesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Delivery delivery,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO transversal.bandeja_salida
                (id, aggregate_id, message_type, payload, occurred_at)
            VALUES
                (@id, @aggregate_id, @message_type, @payload, @occurred_at)
            """;

        foreach (var domainEvent in delivery.DomainEvents)
        {
            var message = OutboxEventSerializer.ToMessage(domainEvent);

            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.AddUuid("id", Guid.NewGuid());
            command.AddUuid("aggregate_id", message.AggregateId);
            command.AddText("message_type", message.MessageType);
            command.AddJsonb("payload", message.Payload);
            command.AddTimestampTz("occurred_at", message.OccurredAt);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static void AddDeliveryParameters(NpgsqlCommand command, Delivery delivery)
    {
        command.AddUuid("id", delivery.Id.Value);
        command.AddUuid("order_id", delivery.OrderId.Value);
        command.AddText("address", delivery.Address.Value);
        command.AddTimestampTz("window_start", delivery.Window.Start);
        command.AddTimestampTz("window_end", delivery.Window.End);
        command.AddText("priority", delivery.Priority.ToString());
        command.AddText("status", delivery.Status.ToString());
        command.AddUuid("courier_id", delivery.CourierId?.Value);
        command.AddTimestampTz("assigned_at", delivery.AssignedAt);
    }
}
