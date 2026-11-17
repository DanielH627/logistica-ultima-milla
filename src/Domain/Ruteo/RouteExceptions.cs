using LogisticaUltimaMilla.Domain.Shared;

namespace LogisticaUltimaMilla.Domain.Ruteo;

/// <summary>INV04: la ruta ya alcanzó su capacidad.</summary>
public sealed class RouteSaturatedException : DomainException
{
    public RouteSaturatedException(RouteId routeId, RouteCapacity capacity)
        : base($"La ruta {routeId} ya alcanzó su capacidad de {capacity.MaxStops} paradas.")
    {
        RouteId = routeId;
        Capacity = capacity;
    }

    public RouteId RouteId { get; }

    public RouteCapacity Capacity { get; }
}

/// <summary>La entrega ya figura como parada de la ruta.</summary>
public sealed class StopAlreadyRegisteredException : DomainException
{
    public StopAlreadyRegisteredException(RouteId routeId, DeliveryId deliveryId)
        : base($"La entrega {deliveryId} ya es una parada de la ruta {routeId}.")
    {
    }
}
