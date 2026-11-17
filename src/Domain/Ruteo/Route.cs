using LogisticaUltimaMilla.Domain.Shared;

namespace LogisticaUltimaMilla.Domain.Ruteo;

/// <summary>
/// Raíz del agregado Ruta. Protege INV04: una ruta no puede superar su capacidad.
/// </summary>
public sealed class Route
{
    private readonly List<Stop> _stops = new();

    public Route(RouteId id, CourierId courierId, DateOnly date, RouteCapacity capacity)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(courierId);
        ArgumentNullException.ThrowIfNull(capacity);

        Id = id;
        CourierId = courierId;
        Date = date;
        Capacity = capacity;
    }

    public RouteId Id { get; }

    public CourierId CourierId { get; }

    public DateOnly Date { get; }

    public RouteCapacity Capacity { get; }

    public IReadOnlyList<Stop> Stops => _stops.AsReadOnly();

    public bool IsSaturated => _stops.Count >= Capacity.MaxStops;

    /// <summary>Agrega una parada al final de la ruta, si todavía hay cupo.</summary>
    public void AddStop(DeliveryId deliveryId)
    {
        ArgumentNullException.ThrowIfNull(deliveryId);

        if (_stops.Any(stop => stop.DeliveryId == deliveryId))
        {
            throw new StopAlreadyRegisteredException(Id, deliveryId);
        }

        if (IsSaturated)
        {
            throw new RouteSaturatedException(Id, Capacity);
        }

        _stops.Add(new Stop(deliveryId, _stops.Count + 1));
    }

    /// <summary>Retira una parada y renumera las restantes. Si no existe, no hace nada.</summary>
    public void RemoveStop(DeliveryId deliveryId)
    {
        ArgumentNullException.ThrowIfNull(deliveryId);

        var remaining = _stops
            .Where(stop => stop.DeliveryId != deliveryId)
            .Select((stop, index) => new Stop(stop.DeliveryId, index + 1))
            .ToList();

        _stops.Clear();
        _stops.AddRange(remaining);
    }
}
