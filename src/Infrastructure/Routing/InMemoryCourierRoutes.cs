using LogisticaUltimaMilla.Application.Ports;
using LogisticaUltimaMilla.Domain.Ruteo;
using LogisticaUltimaMilla.Domain.Shared;

namespace LogisticaUltimaMilla.Infrastructure.Routing;

/// <summary>
/// Adaptador en memoria del puerto ICourierRoutes. Representa el contrato público del contexto Ruteo.
/// Traduce el modelo de Ruteo (Courier, Route) al lenguaje de Entregas: disponible o no, con motivo.
/// Es la capa anticorrupción del lado de Entregas.
/// </summary>
public sealed class InMemoryCourierRoutes : ICourierRoutes
{
    private readonly Dictionary<CourierId, Courier> _couriers = new();
    private readonly Dictionary<CourierId, Route> _routes = new();
    private readonly object _gate = new();

    /// <summary>Registra un repartidor con su ruta (un repartidor tiene una ruta activa a la vez, A3-S03).</summary>
    public void AddCourier(Courier courier, Route route)
    {
        ArgumentNullException.ThrowIfNull(courier);
        ArgumentNullException.ThrowIfNull(route);

        if (route.CourierId != courier.Id)
        {
            throw new ArgumentException("La ruta debe pertenecer al repartidor indicado.", nameof(route));
        }

        lock (_gate)
        {
            _couriers[courier.Id] = courier;
            _routes[courier.Id] = route;
        }
    }

    /// <summary>Cantidad de paradas actuales de la ruta del repartidor. Útil para las pruebas.</summary>
    public int StopCount(CourierId courierId)
    {
        lock (_gate)
        {
            return _routes.TryGetValue(courierId, out var route) ? route.Stops.Count : 0;
        }
    }

    public Task<CourierAvailability> GetAvailabilityAsync(CourierId courierId, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (!_couriers.TryGetValue(courierId, out var courier) || !courier.IsActive)
            {
                return Task.FromResult(CourierAvailability.Unavailable(UnavailabilityReason.CourierNotActive));
            }

            var isSaturated = _routes.TryGetValue(courierId, out var route) && route.IsSaturated;

            return Task.FromResult(
                isSaturated
                    ? CourierAvailability.Unavailable(UnavailabilityReason.RouteSaturated)
                    : CourierAvailability.Available());
        }
    }

    public Task<StopReservationStatus> ReserveStopAsync(CourierId courierId, DeliveryId deliveryId, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (!_couriers.TryGetValue(courierId, out var courier) || !courier.IsActive || !_routes.TryGetValue(courierId, out var route))
            {
                return Task.FromResult(StopReservationStatus.CourierNotActive);
            }

            try
            {
                route.AddStop(deliveryId);
                return Task.FromResult(StopReservationStatus.Reserved);
            }
            catch (RouteSaturatedException)
            {
                return Task.FromResult(StopReservationStatus.RouteSaturated);
            }
        }
    }

    public Task ReleaseStopAsync(CourierId courierId, DeliveryId deliveryId, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (_routes.TryGetValue(courierId, out var route))
            {
                route.RemoveStop(deliveryId);
            }
        }

        return Task.CompletedTask;
    }
}
