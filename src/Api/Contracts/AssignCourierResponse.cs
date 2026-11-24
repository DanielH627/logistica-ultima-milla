namespace LogisticaUltimaMilla.Api.Contracts;

/// <summary>Respuesta de una asignación exitosa.</summary>
/// <param name="NoticeQueued">El aviso al repartidor quedó encolado (no significa que ya haya llegado).</param>
public sealed record AssignCourierResponse(
    Guid DeliveryId,
    Guid CourierId,
    DateTimeOffset AssignedAt,
    bool NoticeQueued);
