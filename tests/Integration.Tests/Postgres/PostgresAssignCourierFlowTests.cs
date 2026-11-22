using LogisticaUltimaMilla.Application.Entregas;
using LogisticaUltimaMilla.Domain.Entregas;
using LogisticaUltimaMilla.Infrastructure.Events;
using LogisticaUltimaMilla.Infrastructure.Notifications;
using LogisticaUltimaMilla.Infrastructure.Persistence;
using LogisticaUltimaMilla.Infrastructure.Routing;
using LogisticaUltimaMilla.Infrastructure.Time;
using Xunit;

namespace LogisticaUltimaMilla.Integration.Tests.Postgres;

/// <summary>
/// El flujo completo con persistencia real. El caso de uso y el dominio son los mismos de la Actividad 3:
/// solo cambian los adaptadores del repositorio y de Ruteo.
/// </summary>
public class PostgresAssignCourierFlowTests : PostgresTestBase
{
    private readonly InMemoryDomainEventPublisher _events = new();
    private readonly InMemoryNotificationRequester _notifier = new();

    private AssignCourierUseCase CreateUseCase(out PostgresDeliveryRepository deliveries)
    {
        deliveries = new PostgresDeliveryRepository(DataSource);

        return new AssignCourierUseCase(
            deliveries,
            new PostgresCourierRoutes(DataSource),
            _events,
            _notifier,
            new SystemClock());
    }

    // EA01
    [RequiresPostgresFact]
    public async Task FullFlow_AssignsDeliveryAndPersistsTheStop()
    {
        var useCase = CreateUseCase(out var deliveries);
        var deliveryId = await PostgresTestData.SeedPendingDeliveryAsync(deliveries);
        var courier = await PostgresTestData.InsertCourierWithRouteAsync(DataSource, "Andrés", capacity: 10, occupiedStops: 6);

        var result = await useCase.ExecuteAsync(new AssignCourierCommand(deliveryId.Value, courier.Value, "Sandra"));

        Assert.True(result.IsSuccess);

        var stored = await deliveries.GetAsync(deliveryId);
        Assert.Equal(DeliveryStatus.Assigned, stored!.Status);
        Assert.Equal(courier, stored.CourierId);
        Assert.Equal(7, await PostgresTestData.CountStopsAsync(DataSource, courier));
        Assert.IsType<DeliveryAssigned>(Assert.Single(_events.Published));
    }

    // EA09
    [RequiresPostgresFact]
    public async Task SaturatedRoute_RejectsAndLeavesTheDeliveryPending()
    {
        var useCase = CreateUseCase(out var deliveries);
        var deliveryId = await PostgresTestData.SeedPendingDeliveryAsync(deliveries);
        var courier = await PostgresTestData.InsertCourierWithRouteAsync(DataSource, "Andrés", capacity: 10, occupiedStops: 10);

        var result = await useCase.ExecuteAsync(new AssignCourierCommand(deliveryId.Value, courier.Value, "Sandra"));

        Assert.False(result.IsSuccess);
        Assert.Equal(AssignCourierErrorCodes.RouteSaturated, result.ErrorCode);

        var stored = await deliveries.GetAsync(deliveryId);
        Assert.Equal(DeliveryStatus.PendingAssignment, stored!.Status);
        Assert.Empty(_events.Published);
    }

    // EA08
    [RequiresPostgresFact]
    public async Task InactiveCourier_RejectsTheAssignment()
    {
        var useCase = CreateUseCase(out var deliveries);
        var deliveryId = await PostgresTestData.SeedPendingDeliveryAsync(deliveries);
        var courier = await PostgresTestData.InsertCourierWithRouteAsync(
            DataSource, "Paola", capacity: 10, occupiedStops: 0, isActive: false);

        var result = await useCase.ExecuteAsync(new AssignCourierCommand(deliveryId.Value, courier.Value, "Sandra"));

        Assert.False(result.IsSuccess);
        Assert.Equal(AssignCourierErrorCodes.CourierNotActive, result.ErrorCode);
    }

    // EA11
    [RequiresPostgresFact]
    public async Task SimultaneousAssignments_OfSameDelivery_OnlyOneWins()
    {
        var useCase = CreateUseCase(out var deliveries);
        var deliveryId = await PostgresTestData.SeedPendingDeliveryAsync(deliveries);
        var andres = await PostgresTestData.InsertCourierWithRouteAsync(DataSource, "Andrés", capacity: 10, occupiedStops: 3);
        var luisa = await PostgresTestData.InsertCourierWithRouteAsync(DataSource, "Luisa", capacity: 10, occupiedStops: 3);

        var results = await Task.WhenAll(
            Task.Run(() => useCase.ExecuteAsync(new AssignCourierCommand(deliveryId.Value, andres.Value, "Sandra"))),
            Task.Run(() => useCase.ExecuteAsync(new AssignCourierCommand(deliveryId.Value, luisa.Value, "Julián"))));

        Assert.Equal(1, results.Count(result => result.IsSuccess));

        var failed = Assert.Single(results, result => !result.IsSuccess);
        Assert.Equal(AssignCourierErrorCodes.DeliveryAlreadyAssigned, failed.ErrorCode);

        var stored = await deliveries.GetAsync(deliveryId);
        Assert.Equal(DeliveryStatus.Assigned, stored!.Status);

        // Solo una de las rutas ganó una parada: la otra fue liberada.
        var stops = await PostgresTestData.CountStopsAsync(DataSource, andres)
                    + await PostgresTestData.CountStopsAsync(DataSource, luisa);
        Assert.Equal(7, stops);
    }

    // EA12
    [RequiresPostgresFact]
    public async Task SimultaneousAssignments_ForLastSlot_OnlyOneWins()
    {
        var useCase = CreateUseCase(out var deliveries);
        var first = await PostgresTestData.SeedPendingDeliveryAsync(deliveries);
        var second = await PostgresTestData.SeedPendingDeliveryAsync(deliveries);
        var andres = await PostgresTestData.InsertCourierWithRouteAsync(DataSource, "Andrés", capacity: 10, occupiedStops: 9);

        var results = await Task.WhenAll(
            Task.Run(() => useCase.ExecuteAsync(new AssignCourierCommand(first.Value, andres.Value, "Sandra"))),
            Task.Run(() => useCase.ExecuteAsync(new AssignCourierCommand(second.Value, andres.Value, "Julián"))));

        Assert.Equal(1, results.Count(result => result.IsSuccess));

        var failed = Assert.Single(results, result => !result.IsSuccess);
        Assert.Equal(AssignCourierErrorCodes.RouteSaturated, failed.ErrorCode);

        Assert.Equal(10, await PostgresTestData.CountStopsAsync(DataSource, andres));
    }
}
