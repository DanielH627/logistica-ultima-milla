using LogisticaUltimaMilla.Application.Entregas;
using LogisticaUltimaMilla.Domain.Entregas;
using LogisticaUltimaMilla.Domain.Ruteo;
using LogisticaUltimaMilla.Domain.Shared;
using LogisticaUltimaMilla.Infrastructure.Persistence;
using LogisticaUltimaMilla.Infrastructure.Routing;
using LogisticaUltimaMilla.Infrastructure.Time;
using Xunit;

namespace LogisticaUltimaMilla.Integration.Tests;

/// <summary>
/// Pruebas del flujo completo con los adaptadores en memoria.
/// Comprueban la coordinación entre Entregas y Ruteo, incluida la concurrencia.
/// </summary>
public class AssignCourierFlowTests
{
    private readonly InMemoryDeliveryRepository _deliveries = new();
    private readonly InMemoryCourierRoutes _routes = new();
    private readonly AssignCourierUseCase _useCase;

    public AssignCourierFlowTests()
    {
        _useCase = new AssignCourierUseCase(_deliveries, _routes, new SystemClock());
    }

    private async Task<DeliveryId> SeedPendingDeliveryAsync()
    {
        var window = new DeliveryWindow(
            new DateTimeOffset(2026, 11, 13, 14, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 11, 13, 16, 0, 0, TimeSpan.Zero));

        var delivery = Delivery.CreatePending(
            DeliveryId.New(),
            OrderId.New(),
            new DeliveryAddress("Carrera 70 # 44-18"),
            window,
            DeliveryPriority.Normal);

        await _deliveries.SaveAsync(delivery);
        return delivery.Id;
    }

    private CourierId SeedCourier(string name, int capacity, int occupiedStops)
    {
        var courier = new Courier(CourierId.New(), name, isActive: true);
        var route = new Route(RouteId.New(), courier.Id, new DateOnly(2026, 11, 13), new RouteCapacity(capacity));

        for (var i = 0; i < occupiedStops; i++)
        {
            route.AddStop(DeliveryId.New());
        }

        _routes.AddCourier(courier, route);
        return courier.Id;
    }

    // EA01
    [Fact]
    public async Task FullFlow_AssignsDeliveryAndRegistersStopInRoute()
    {
        var deliveryId = await SeedPendingDeliveryAsync();
        var courierId = SeedCourier("Andrés", capacity: 10, occupiedStops: 6);

        var result = await _useCase.ExecuteAsync(new AssignCourierCommand(deliveryId.Value, courierId.Value, "Sandra"));

        Assert.True(result.IsSuccess);

        var stored = await _deliveries.GetAsync(deliveryId);
        Assert.NotNull(stored);
        Assert.Equal(DeliveryStatus.Assigned, stored!.Status);
        Assert.Equal(courierId, stored.CourierId);

        Assert.Equal(7, _routes.StopCount(courierId));
        Assert.IsType<DeliveryAssigned>(Assert.Single(_deliveries.SavedEvents));
    }

    // EA11
    [Fact]
    public async Task SimultaneousAssignments_OfSameDelivery_OnlyOneWins()
    {
        var deliveryId = await SeedPendingDeliveryAsync();
        var andres = SeedCourier("Andrés", capacity: 10, occupiedStops: 3);
        var luisa = SeedCourier("Luisa", capacity: 10, occupiedStops: 3);

        var results = await Task.WhenAll(
            Task.Run(() => _useCase.ExecuteAsync(new AssignCourierCommand(deliveryId.Value, andres.Value, "Sandra"))),
            Task.Run(() => _useCase.ExecuteAsync(new AssignCourierCommand(deliveryId.Value, luisa.Value, "Julián"))));

        Assert.Equal(1, results.Count(result => result.IsSuccess));

        var failed = Assert.Single(results, result => !result.IsSuccess);
        Assert.Equal(AssignCourierErrorCodes.DeliveryAlreadyAssigned, failed.ErrorCode);

        var stored = await _deliveries.GetAsync(deliveryId);
        Assert.Equal(DeliveryStatus.Assigned, stored!.Status);

        // Solo una de las dos rutas ganó una parada: la otra fue liberada (compensación).
        Assert.Equal(7, _routes.StopCount(andres) + _routes.StopCount(luisa));
        Assert.Single(_deliveries.SavedEvents);
    }

    // EA12
    [Fact]
    public async Task SimultaneousAssignments_ForLastSlot_OnlyOneWins()
    {
        var first = await SeedPendingDeliveryAsync();
        var second = await SeedPendingDeliveryAsync();
        var andres = SeedCourier("Andrés", capacity: 10, occupiedStops: 9);

        var results = await Task.WhenAll(
            Task.Run(() => _useCase.ExecuteAsync(new AssignCourierCommand(first.Value, andres.Value, "Sandra"))),
            Task.Run(() => _useCase.ExecuteAsync(new AssignCourierCommand(second.Value, andres.Value, "Julián"))));

        Assert.Equal(1, results.Count(result => result.IsSuccess));

        var failed = Assert.Single(results, result => !result.IsSuccess);
        Assert.Equal(AssignCourierErrorCodes.RouteSaturated, failed.ErrorCode);

        Assert.Equal(10, _routes.StopCount(andres));
    }
}
