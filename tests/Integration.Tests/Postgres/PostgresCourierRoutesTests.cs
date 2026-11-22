using LogisticaUltimaMilla.Application.Ports;
using LogisticaUltimaMilla.Domain.Shared;
using LogisticaUltimaMilla.Infrastructure.Routing;
using Xunit;

namespace LogisticaUltimaMilla.Integration.Tests.Postgres;

public class PostgresCourierRoutesTests : PostgresTestBase
{
    [RequiresPostgresFact]
    public async Task GetAvailability_ActiveCourierWithRoom_IsAvailable()
    {
        var routes = new PostgresCourierRoutes(DataSource);
        var courier = await PostgresTestData.InsertCourierWithRouteAsync(DataSource, "Andrés", capacity: 10, occupiedStops: 6);

        var availability = await routes.GetAvailabilityAsync(courier);

        Assert.True(availability.IsAvailable);
    }

    [RequiresPostgresFact]
    public async Task GetAvailability_InactiveCourier_IsNotActive()
    {
        var routes = new PostgresCourierRoutes(DataSource);
        var courier = await PostgresTestData.InsertCourierWithRouteAsync(
            DataSource, "Paola", capacity: 10, occupiedStops: 0, isActive: false);

        var availability = await routes.GetAvailabilityAsync(courier);

        Assert.False(availability.IsAvailable);
        Assert.Equal(UnavailabilityReason.CourierNotActive, availability.Reason);
    }

    [RequiresPostgresFact]
    public async Task GetAvailability_UnknownCourier_IsNotActive()
    {
        var routes = new PostgresCourierRoutes(DataSource);

        var availability = await routes.GetAvailabilityAsync(CourierId.New());

        Assert.False(availability.IsAvailable);
        Assert.Equal(UnavailabilityReason.CourierNotActive, availability.Reason);
    }

    [RequiresPostgresFact]
    public async Task GetAvailability_CourierWithoutRoute_IsOutOfShift()
    {
        var routes = new PostgresCourierRoutes(DataSource);
        var courier = await PostgresTestData.InsertCourierWithoutRouteAsync(DataSource, "Camilo");

        var availability = await routes.GetAvailabilityAsync(courier);

        Assert.False(availability.IsAvailable);
        Assert.Equal(UnavailabilityReason.OutOfShift, availability.Reason);
    }

    [RequiresPostgresFact]
    public async Task GetAvailability_SaturatedRoute_IsSaturated()
    {
        var routes = new PostgresCourierRoutes(DataSource);
        var courier = await PostgresTestData.InsertCourierWithRouteAsync(DataSource, "Andrés", capacity: 10, occupiedStops: 10);

        var availability = await routes.GetAvailabilityAsync(courier);

        Assert.False(availability.IsAvailable);
        Assert.Equal(UnavailabilityReason.RouteSaturated, availability.Reason);
    }

    [RequiresPostgresFact]
    public async Task ReserveStop_TakesTheLastSlot_ThenRouteIsSaturated()
    {
        var routes = new PostgresCourierRoutes(DataSource);
        var courier = await PostgresTestData.InsertCourierWithRouteAsync(DataSource, "Andrés", capacity: 10, occupiedStops: 9);

        var first = await routes.ReserveStopAsync(courier, DeliveryId.New());
        var second = await routes.ReserveStopAsync(courier, DeliveryId.New());

        Assert.Equal(StopReservationStatus.Reserved, first);
        Assert.Equal(StopReservationStatus.RouteSaturated, second);
        Assert.Equal(10, await PostgresTestData.CountStopsAsync(DataSource, courier));
    }

    [RequiresPostgresFact]
    public async Task ReserveStop_SameDeliveryTwice_IsRepeatable()
    {
        var routes = new PostgresCourierRoutes(DataSource);
        var courier = await PostgresTestData.InsertCourierWithRouteAsync(DataSource, "Andrés", capacity: 10, occupiedStops: 2);
        var deliveryId = DeliveryId.New();

        var first = await routes.ReserveStopAsync(courier, deliveryId);
        var second = await routes.ReserveStopAsync(courier, deliveryId);

        Assert.Equal(StopReservationStatus.Reserved, first);
        Assert.Equal(StopReservationStatus.Reserved, second);
        Assert.Equal(3, await PostgresTestData.CountStopsAsync(DataSource, courier));
    }

    [RequiresPostgresFact]
    public async Task ReserveStop_InactiveCourier_IsRejected()
    {
        var routes = new PostgresCourierRoutes(DataSource);
        var courier = await PostgresTestData.InsertCourierWithRouteAsync(
            DataSource, "Paola", capacity: 10, occupiedStops: 0, isActive: false);

        var result = await routes.ReserveStopAsync(courier, DeliveryId.New());

        Assert.Equal(StopReservationStatus.CourierNotActive, result);
    }

    [RequiresPostgresFact]
    public async Task ReleaseStop_FreesTheSlot()
    {
        var routes = new PostgresCourierRoutes(DataSource);
        var courier = await PostgresTestData.InsertCourierWithRouteAsync(DataSource, "Andrés", capacity: 10, occupiedStops: 9);
        var deliveryId = DeliveryId.New();

        await routes.ReserveStopAsync(courier, deliveryId);
        await routes.ReleaseStopAsync(courier, deliveryId);

        Assert.Equal(9, await PostgresTestData.CountStopsAsync(DataSource, courier));
        Assert.Equal(StopReservationStatus.Reserved, await routes.ReserveStopAsync(courier, DeliveryId.New()));
    }

    // Límite crítico: la capacidad no se supera ni con muchas reservas simultáneas (INV04).
    [RequiresPostgresFact]
    public async Task ReserveStop_ManyAtTheSameTime_NeverExceedsCapacity()
    {
        var routes = new PostgresCourierRoutes(DataSource);
        var courier = await PostgresTestData.InsertCourierWithRouteAsync(DataSource, "Andrés", capacity: 10, occupiedStops: 0);

        var results = await Task.WhenAll(
            Enumerable.Range(0, 25).Select(_ => Task.Run(() => routes.ReserveStopAsync(courier, DeliveryId.New()))));

        Assert.Equal(10, results.Count(result => result == StopReservationStatus.Reserved));
        Assert.Equal(15, results.Count(result => result == StopReservationStatus.RouteSaturated));
        Assert.Equal(10, await PostgresTestData.CountStopsAsync(DataSource, courier));
    }
}
