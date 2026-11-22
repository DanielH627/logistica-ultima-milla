using LogisticaUltimaMilla.Application.Ports;
using LogisticaUltimaMilla.Domain.Entregas;
using LogisticaUltimaMilla.Domain.Shared;
using LogisticaUltimaMilla.Infrastructure.Persistence;
using Xunit;

namespace LogisticaUltimaMilla.Integration.Tests.Postgres;

public class PostgresDeliveryRepositoryTests : PostgresTestBase
{
    private static readonly DateTimeOffset Now = new(2026, 11, 13, 10, 0, 0, TimeSpan.Zero);

    [RequiresPostgresFact]
    public async Task SaveThenGet_ReturnsTheSameDelivery()
    {
        var repository = new PostgresDeliveryRepository(DataSource);
        var delivery = PostgresTestData.NewPendingDelivery(DeliveryPriority.Urgent);

        await repository.SaveAsync(delivery);
        var stored = await repository.GetAsync(delivery.Id);

        Assert.NotNull(stored);
        Assert.Equal(delivery.Id, stored!.Id);
        Assert.Equal(delivery.OrderId, stored.OrderId);
        Assert.Equal(delivery.Address, stored.Address);
        Assert.Equal(delivery.Window, stored.Window);
        Assert.Equal(DeliveryPriority.Urgent, stored.Priority);
        Assert.Equal(DeliveryStatus.PendingAssignment, stored.Status);
        Assert.Null(stored.CourierId);
        Assert.Equal(1, stored.Version);
    }

    [RequiresPostgresFact]
    public async Task Save_AssignedDelivery_PersistsCourierAndStatus()
    {
        var repository = new PostgresDeliveryRepository(DataSource);
        var deliveryId = await PostgresTestData.SeedPendingDeliveryAsync(repository);
        var courierId = CourierId.New();

        var delivery = await repository.GetAsync(deliveryId);
        delivery!.AssignCourier(courierId, Now, "Sandra");
        await repository.SaveAsync(delivery);

        var stored = await repository.GetAsync(deliveryId);
        Assert.Equal(DeliveryStatus.Assigned, stored!.Status);
        Assert.Equal(courierId, stored.CourierId);
        Assert.Equal(Now, stored.AssignedAt);
        Assert.Equal(2, stored.Version);
    }

    [RequiresPostgresFact]
    public async Task Save_WithStaleVersion_ThrowsConcurrencyException()
    {
        var repository = new PostgresDeliveryRepository(DataSource);
        var deliveryId = await PostgresTestData.SeedPendingDeliveryAsync(repository);

        var first = await repository.GetAsync(deliveryId);
        var second = await repository.GetAsync(deliveryId);

        first!.AssignCourier(CourierId.New(), Now, "Sandra");
        second!.AssignCourier(CourierId.New(), Now, "Julián");

        await repository.SaveAsync(first);

        await Assert.ThrowsAsync<DeliveryConcurrencyException>(() => repository.SaveAsync(second));
    }

    [RequiresPostgresFact]
    public async Task Save_SameNewDeliveryTwice_ThrowsConcurrencyException()
    {
        var repository = new PostgresDeliveryRepository(DataSource);
        var delivery = PostgresTestData.NewPendingDelivery();

        await repository.SaveAsync(delivery);

        await Assert.ThrowsAsync<DeliveryConcurrencyException>(() => repository.SaveAsync(delivery));
    }

    [RequiresPostgresFact]
    public async Task Get_UnknownDelivery_ReturnsNull()
    {
        var repository = new PostgresDeliveryRepository(DataSource);

        var stored = await repository.GetAsync(DeliveryId.New());

        Assert.Null(stored);
    }
}
