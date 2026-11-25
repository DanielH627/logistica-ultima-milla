namespace LogisticaUltimaMilla.Infrastructure.Outbox;

/// <summary>Un mensaje de la bandeja de salida, tal como lo recibe un manejador.</summary>
/// <param name="Attempts">Número de intento actual (1 en el primer intento).</param>
public sealed record OutboxMessage(
    Guid Id,
    Guid AggregateId,
    string MessageType,
    string Payload,
    DateTimeOffset OccurredAt,
    int Attempts);

/// <summary>
/// Quien sabe qué hacer con un tipo de mensaje. Un mismo tipo puede tener varios manejadores
/// (por ejemplo, historial y aviso). Como la entrega es "al menos una vez", cada manejador DEBE ser idempotente:
/// procesar dos veces el mismo mensaje no puede producir un efecto distinto al de procesarlo una.
/// </summary>
public interface IOutboxMessageHandler
{
    string MessageType { get; }

    /// <summary>Lanza una excepción si no pudo procesar el mensaje; el procesador lo reintentará.</summary>
    Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken);
}
