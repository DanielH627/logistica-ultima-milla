using LogisticaUltimaMilla.Domain.Ruteo;
using LogisticaUltimaMilla.Domain.Shared;
using Xunit;

namespace LogisticaUltimaMilla.Domain.Tests;

public class RouteTests
{
    private static Route NewRoute(int capacity, int occupiedStops)
    {
        var route = new Route(RouteId.New(), CourierId.New(), new DateOnly(2026, 11, 13), new RouteCapacity(capacity));

        for (var i = 0; i < occupiedStops; i++)
        {
            route.AddStop(DeliveryId.New());
        }

        return route;
    }

    // EA10
    [Fact]
    public void AddStop_TakingLastSlot_RouteBecomesSaturated()
    {
        var route = NewRoute(capacity: 10, occupiedStops: 9);

        route.AddStop(DeliveryId.New());

        Assert.Equal(10, route.Stops.Count);
        Assert.True(route.IsSaturated);
    }

    // EA09
    [Fact]
    public void AddStop_OnSaturatedRoute_Throws()
    {
        var route = NewRoute(capacity: 10, occupiedStops: 10);

        Assert.Throws<RouteSaturatedException>(() => route.AddStop(DeliveryId.New()));

        Assert.Equal(10, route.Stops.Count);
    }

    [Fact]
    public void RemoveStop_FreesSlotAndRenumbersTheRest()
    {
        var route = NewRoute(capacity: 3, occupiedStops: 0);
        var first = DeliveryId.New();
        var second = DeliveryId.New();
        var third = DeliveryId.New();
        route.AddStop(first);
        route.AddStop(second);
        route.AddStop(third);

        route.RemoveStop(second);

        Assert.False(route.IsSaturated);
        Assert.Equal(2, route.Stops.Count);
        Assert.Equal(1, route.Stops[0].Position);
        Assert.Equal(third, route.Stops[1].DeliveryId);
        Assert.Equal(2, route.Stops[1].Position);
    }

    [Fact]
    public void AddStop_SameDeliveryTwice_Throws()
    {
        var route = NewRoute(capacity: 5, occupiedStops: 0);
        var deliveryId = DeliveryId.New();
        route.AddStop(deliveryId);

        Assert.Throws<StopAlreadyRegisteredException>(() => route.AddStop(deliveryId));
    }
}
