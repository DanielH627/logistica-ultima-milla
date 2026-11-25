using LogisticaUltimaMilla.Application.Ports;
using LogisticaUltimaMilla.Infrastructure.Postgres;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace LogisticaUltimaMilla.Infrastructure.Outbox;

/// <summary>
/// Entrega los mensajes de la bandeja de salida a sus manejadores (AJ-01, ADR-005).
/// - Reclama un mensaje con FOR UPDATE SKIP LOCKED: dos procesadores nunca toman el mismo mensaje.
/// - Si un manejador falla, reintenta con espera exponencial; al agotar los intentos lo marca "Dead".
/// - Un mensaje sin manejador también se marca "Dead": no se descarta en silencio.
/// - Solo guarda y registra el tipo y el mensaje de la excepción, nunca el contenido del mensaje.
/// Limitación conocida: los manejadores se ejecutan con la fila bloqueada. Es simple y suficiente a esta escala;
/// con manejadores lentos convendría reclamar con una marca de tiempo y liberar la fila.
/// </summary>
public sealed class OutboxProcessor
{
    private const int MaxErrorLength = 500;

    private readonly NpgsqlDataSource _dataSource;
    private readonly IReadOnlyList<IOutboxMessageHandler> _handlers;
    private readonly IClock _clock;
    private readonly OutboxOptions _options;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(
        NpgsqlDataSource dataSource,
        IEnumerable<IOutboxMessageHandler> handlers,
        IClock clock,
        OutboxOptions options,
        ILogger<OutboxProcessor> logger)
    {
        _dataSource = dataSource;
        _handlers = handlers.ToList();
        _clock = clock;
        _options = options;
        _logger = logger;
    }

    /// <summary>Procesa los mensajes pendientes y vencidos, hasta BatchSize. Devuelve cuántos atendió.</summary>
    public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken = default)
    {
        var handled = 0;

        while (handled < _options.BatchSize && await ProcessOneAsync(cancellationToken))
        {
            handled++;
        }

        return handled;
    }

    private async Task<bool> ProcessOneAsync(CancellationToken cancellationToken)
    {
        const string claimSql = """
            SELECT id, aggregate_id, message_type, payload::text, occurred_at, attempts
            FROM transversal.bandeja_salida
            WHERE status = 'Pending' AND next_attempt_at <= @now
            ORDER BY next_attempt_at, occurred_at
            LIMIT 1
            FOR UPDATE SKIP LOCKED
            """;

        var now = _clock.UtcNow;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        OutboxMessage message;
        await using (var claim = new NpgsqlCommand(claimSql, connection, transaction))
        {
            claim.AddTimestampTz("now", now);
            await using var reader = await claim.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return false;
            }

            message = new OutboxMessage(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetFieldValue<DateTimeOffset>(4),
                reader.GetInt32(5) + 1);
        }

        var matching = _handlers.Where(h => h.MessageType == message.MessageType).ToList();

        if (matching.Count == 0)
        {
            await MarkDeadAsync(connection, transaction, message, "No hay manejador para el tipo de mensaje.", cancellationToken);
            OutboxMetrics.Record("dead");
            _logger.LogError(
                "Mensaje {MessageId} de tipo {MessageType} sin manejador; marcado como Dead",
                message.Id, message.MessageType);
        }
        else
        {
            try
            {
                foreach (var handler in matching)
                {
                    await handler.HandleAsync(message, cancellationToken);
                }

                await MarkSentAsync(connection, transaction, message, now, cancellationToken);
                OutboxMetrics.Record("sent");
                _logger.LogInformation(
                    "Mensaje {MessageId} de tipo {MessageType} procesado en el intento {Attempt}",
                    message.Id, message.MessageType, message.Attempts);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Apagado de la aplicación: se deshace la transacción y el mensaje queda como estaba.
                throw;
            }
            catch (Exception exception)
            {
                var error = DescribeError(exception);

                if (message.Attempts >= _options.MaxAttempts)
                {
                    await MarkDeadAsync(connection, transaction, message, error, cancellationToken);
                    OutboxMetrics.Record("dead");
                    _logger.LogError(
                        "Mensaje {MessageId} de tipo {MessageType} agotó {Attempts} intentos; marcado como Dead. Error: {Error}",
                        message.Id, message.MessageType, message.Attempts, error);
                }
                else
                {
                    var delay = TimeSpan.FromSeconds(_options.BaseDelaySeconds * Math.Pow(2, message.Attempts - 1));
                    await ScheduleRetryAsync(connection, transaction, message, now + delay, error, cancellationToken);
                    OutboxMetrics.Record("retry");
                    _logger.LogWarning(
                        "Mensaje {MessageId} de tipo {MessageType} falló en el intento {Attempt}; se reintentará en {DelaySeconds} s. Error: {Error}",
                        message.Id, message.MessageType, message.Attempts, delay.TotalSeconds, error);
                }
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private static string DescribeError(Exception exception)
    {
        // Solo el tipo y el mensaje: nunca el contenido del mensaje ni la traza completa.
        var text = $"{exception.GetType().Name}: {exception.Message}";
        return text.Length <= MaxErrorLength ? text : text[..MaxErrorLength];
    }

    private static async Task MarkSentAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, OutboxMessage message,
        DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            UPDATE transversal.bandeja_salida
            SET status = 'Sent', attempts = @attempts, processed_at = @now, last_error = NULL
            WHERE id = @id
            """,
            connection, transaction);
        command.AddUuid("id", message.Id);
        command.AddInteger("attempts", message.Attempts);
        command.AddTimestampTz("now", now);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task MarkDeadAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, OutboxMessage message,
        string error, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            UPDATE transversal.bandeja_salida
            SET status = 'Dead', attempts = @attempts, last_error = @error
            WHERE id = @id
            """,
            connection, transaction);
        command.AddUuid("id", message.Id);
        command.AddInteger("attempts", message.Attempts);
        command.AddText("error", error);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task ScheduleRetryAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, OutboxMessage message,
        DateTimeOffset nextAttemptAt, string error, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            UPDATE transversal.bandeja_salida
            SET attempts = @attempts, next_attempt_at = @next, last_error = @error
            WHERE id = @id
            """,
            connection, transaction);
        command.AddUuid("id", message.Id);
        command.AddInteger("attempts", message.Attempts);
        command.AddTimestampTz("next", nextAttemptAt);
        command.AddText("error", error);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
