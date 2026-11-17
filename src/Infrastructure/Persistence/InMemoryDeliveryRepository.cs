using LogisticaUltimaMilla.Application.Ports;
using LogisticaUltimaMilla.Domain.Entregas;
using LogisticaUltimaMilla.Domain.Shared;

namespace LogisticaUltimaMilla.Infrastructure.Persistence;

/// <summary>
/// Adaptador en memoria del puerto IDeliveryRepository. Sirve para probar el flujo sin base de datos.
/// Guarda copias y controla la concurrencia con un número de versión, como lo haría una base real.
/// </summary>
public sealed class InMemoryDeliveryRepository : IDeliveryRepository
{
    private sealed record StoredDelivery(
        DeliveryId Id,
        OrderId OrderId,
        DeliveryAddress Address,
        DeliveryWindow Window,
        DeliveryPriority Priority,
        DeliveryStatus Status,
        CourierId? AssignedCourier,
        DateTimeOffset? AssignedAt,
        int Version);

    private readonly Dictionary<DeliveryId, StoredDelivery> _store = new();
    private readonly object _gate = new();

    public Task<Delivery?> GetAsync(DeliveryId id, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (!_store.TryGetValue(id, out var stored))
            {
                return Task.FromResult<Delivery?>(null);
            }

            var delivery = Delivery.Restore(
                stored.Id,
                stored.OrderId,
                stored.Address,
                stored.Window,
                stored.Priority,
                stored.Status,
                stored.AssignedCourier,
                stored.AssignedAt,
                stored.Version);

            return Task.FromResult<Delivery?>(delivery);
        }
    }

    public Task SaveAsync(Delivery delivery, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(delivery);

        lock (_gate)
        {
            var currentVersion = _store.TryGetValue(delivery.Id, out var current) ? current.Version : 0;

            if (currentVersion != delivery.Version)
            {
                throw new DeliveryConcurrencyException(delivery.Id);
            }

            _store[delivery.Id] = new StoredDelivery(
                delivery.Id,
                delivery.OrderId,
                delivery.Address,
                delivery.Window,
                delivery.Priority,
                delivery.Status,
                delivery.CourierId,
                delivery.AssignedAt,
                currentVersion + 1);
        }

        return Task.CompletedTask;
    }
}
