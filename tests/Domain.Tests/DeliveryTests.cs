using LogisticaUltimaMilla.Domain.Entregas;
using LogisticaUltimaMilla.Domain.Shared;
using Xunit;

namespace LogisticaUltimaMilla.Domain.Tests;

public class DeliveryTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 13, 10, 0, 0, TimeSpan.Zero);

    private static DeliveryWindow Window() =>
        new(new DateTimeOffset(2026, 11, 13, 14, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 11, 13, 16, 0, 0, TimeSpan.Zero));

    private static Delivery NewPending() =>
        Delivery.CreatePending(
            DeliveryId.New(),
            OrderId.New(),
            new DeliveryAddress("Carrera 70 # 44-18"),
            Window(),
            DeliveryPriority.Normal);

    private static Delivery Restored(DeliveryStatus status, CourierId? courierId) =>
        Delivery.Restore(
            DeliveryId.New(),
            OrderId.New(),
            new DeliveryAddress("Carrera 70 # 44-18"),
            Window(),
            DeliveryPriority.Normal,
            status,
            courierId,
            courierId is null ? null : Now,
            1);

    // EA01 (parte del dominio)
    [Fact]
    public void AssignCourier_OnPendingDelivery_BecomesAssignedAndRaisesEvent()
    {
        var delivery = NewPending();
        var courierId = CourierId.New();

        delivery.AssignCourier(courierId, Now, "Sandra");

        Assert.Equal(DeliveryStatus.Assigned, delivery.Status);
        Assert.Equal(courierId, delivery.CourierId);
        Assert.Equal(Now, delivery.AssignedAt);

        var assigned = Assert.IsType<DeliveryAssigned>(Assert.Single(delivery.DomainEvents));
        Assert.Equal(delivery.Id, assigned.DeliveryId);
        Assert.Equal(courierId, assigned.CourierId);
        Assert.Equal("Sandra", assigned.RequestedBy);
    }

    // EA02
    [Fact]
    public void AssignCourier_WhenAlreadyAssigned_Throws()
    {
        var delivery = NewPending();
        var first = CourierId.New();
        delivery.AssignCourier(first, Now, "Sandra");

        Assert.Throws<DeliveryAlreadyAssignedException>(
            () => delivery.AssignCourier(CourierId.New(), Now, "Sandra"));

        Assert.Equal(first, delivery.CourierId);
        Assert.Single(delivery.DomainEvents);
    }

    // EA03
    [Fact]
    public void AssignCourier_SameCourierTwice_Throws()
    {
        var delivery = NewPending();
        var courierId = CourierId.New();
        delivery.AssignCourier(courierId, Now, "Sandra");

        Assert.Throws<DeliveryAlreadyAssignedException>(
            () => delivery.AssignCourier(courierId, Now, "Sandra"));
    }

    // EA04
    [Theory]
    [InlineData(DeliveryStatus.InTransit)]
    [InlineData(DeliveryStatus.Delivered)]
    [InlineData(DeliveryStatus.Failed)]
    [InlineData(DeliveryStatus.Cancelled)]
    public void AssignCourier_WhenNotPending_Throws(DeliveryStatus status)
    {
        var courierId = status == DeliveryStatus.Cancelled ? null : CourierId.New();
        var delivery = Restored(status, courierId);

        var exception = Assert.Throws<DeliveryNotPendingException>(
            () => delivery.AssignCourier(CourierId.New(), Now, "Sandra"));

        Assert.Equal(status, exception.CurrentStatus);
        Assert.Equal(status, delivery.Status);
        Assert.Empty(delivery.DomainEvents);
    }

    // EA05
    [Fact]
    public void NewDelivery_IsPendingWithoutCourier()
    {
        var delivery = NewPending();

        Assert.Equal(DeliveryStatus.PendingAssignment, delivery.Status);
        Assert.Null(delivery.CourierId);
    }

    // INV03
    [Fact]
    public void Restore_PendingWithCourier_Throws()
    {
        Assert.Throws<InvalidValueException>(() => Restored(DeliveryStatus.PendingAssignment, CourierId.New()));
    }

    // INV03
    [Fact]
    public void Restore_AssignedWithoutCourier_Throws()
    {
        Assert.Throws<InvalidValueException>(() => Restored(DeliveryStatus.Assigned, null));
    }

    [Fact]
    public void AssignCourier_WithoutRequester_Throws()
    {
        var delivery = NewPending();

        Assert.Throws<InvalidValueException>(() => delivery.AssignCourier(CourierId.New(), Now, " "));
    }
}
