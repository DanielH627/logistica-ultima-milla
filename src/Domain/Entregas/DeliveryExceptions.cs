using LogisticaUltimaMilla.Domain.Shared;

namespace LogisticaUltimaMilla.Domain.Entregas;

/// <summary>INV01: la entrega ya tiene un repartidor activo.</summary>
public sealed class DeliveryAlreadyAssignedException : DomainException
{
    public DeliveryAlreadyAssignedException(DeliveryId deliveryId, CourierId currentCourierId)
        : base($"La entrega {deliveryId} ya tiene un repartidor activo ({currentCourierId}).")
    {
        DeliveryId = deliveryId;
        CurrentCourierId = currentCourierId;
    }

    public DeliveryId DeliveryId { get; }

    public CourierId CurrentCourierId { get; }
}

/// <summary>INV02: la entrega no está pendiente de asignación.</summary>
public sealed class DeliveryNotPendingException : DomainException
{
    public DeliveryNotPendingException(DeliveryId deliveryId, DeliveryStatus currentStatus)
        : base($"La entrega {deliveryId} está en estado {currentStatus} y no puede asignarse.")
    {
        DeliveryId = deliveryId;
        CurrentStatus = currentStatus;
    }

    public DeliveryId DeliveryId { get; }

    public DeliveryStatus CurrentStatus { get; }
}
