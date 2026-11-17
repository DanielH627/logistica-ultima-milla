using LogisticaUltimaMilla.Domain.Shared;

namespace LogisticaUltimaMilla.Domain.Entregas;

/// <summary>Evento de dominio: a una entrega se le asignó un repartidor (INV06).</summary>
public sealed record DeliveryAssigned(
    DeliveryId DeliveryId,
    CourierId CourierId,
    DateTimeOffset OccurredAt,
    string RequestedBy) : IDomainEvent;
