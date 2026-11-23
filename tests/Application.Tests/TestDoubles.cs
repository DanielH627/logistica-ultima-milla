using LogisticaUltimaMilla.Application.Ports;
using LogisticaUltimaMilla.Domain.Entregas;
using LogisticaUltimaMilla.Domain.Shared;

namespace LogisticaUltimaMilla.Application.Tests;

// Dobles de prueba de los puertos. Permiten probar el caso de uso sin base de datos ni servicios reales.

internal sealed class FakeDeliveryRepository : IDeliveryRepository
{
    private readonly Dictionary<DeliveryId, Delivery> _items = new();

    public int SaveCount { get; private set; }

    public bool ThrowConcurrencyOnSave { get; set; }

    /// <summary>Eventos guardados junto con las entregas. Equivale a la bandeja de salida.</summary>
    public List<IDomainEvent> SavedEvents { get; } = new();

    public void Add(Delivery delivery) => _items[delivery.Id] = delivery;

    public Delivery? Find(DeliveryId id) => _items.TryGetValue(id, out var delivery) ? delivery : null;

    public Task<Delivery?> GetAsync(DeliveryId id, CancellationToken cancellationToken = default)
        => Task.FromResult<Delivery?>(Find(id));

    public Task SaveAsync(Delivery delivery, CancellationToken cancellationToken = default)
    {
        if (ThrowConcurrencyOnSave)
        {
            // Como el repositorio real: si no se puede guardar la entrega, tampoco se guardan sus eventos.
            throw new DeliveryConcurrencyException(delivery.Id);
        }

        SaveCount++;
        _items[delivery.Id] = delivery;
        SavedEvents.AddRange(delivery.DomainEvents);
        return Task.CompletedTask;
    }
}

internal sealed class FakeCourierRoutes : ICourierRoutes
{
    public CourierAvailability Availability { get; set; } = CourierAvailability.Available();

    public StopReservationStatus Reservation { get; set; } = StopReservationStatus.Reserved;

    public bool ThrowOnAvailability { get; set; }

    public int ReserveCalls { get; private set; }

    public int ReleaseCalls { get; private set; }

    public Task<CourierAvailability> GetAvailabilityAsync(CourierId courierId, CancellationToken cancellationToken = default)
    {
        if (ThrowOnAvailability)
        {
            throw new CourierRoutesUnavailableException("Ruteo no responde.");
        }

        return Task.FromResult(Availability);
    }

    public Task<StopReservationStatus> ReserveStopAsync(CourierId courierId, DeliveryId deliveryId, CancellationToken cancellationToken = default)
    {
        ReserveCalls++;
        return Task.FromResult(Reservation);
    }

    public Task ReleaseStopAsync(CourierId courierId, DeliveryId deliveryId, CancellationToken cancellationToken = default)
    {
        ReleaseCalls++;
        return Task.CompletedTask;
    }
}

internal sealed class FixedClock : IClock
{
    public FixedClock(DateTimeOffset now) => UtcNow = now;

    public DateTimeOffset UtcNow { get; }
}
