namespace LogisticaUltimaMilla.Domain.Entregas;

/// <summary>Estados del ciclo de vida de una entrega.</summary>
public enum DeliveryStatus
{
    PendingAssignment,
    Assigned,
    InTransit,
    Delivered,
    Failed,
    Cancelled
}
