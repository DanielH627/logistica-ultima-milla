using System.Text.Json;
using LogisticaUltimaMilla.Application.Ports;
using LogisticaUltimaMilla.Domain.Entregas;
using LogisticaUltimaMilla.Domain.Shared;
using LogisticaUltimaMilla.Infrastructure.Outbox;
using LogisticaUltimaMilla.Infrastructure.Persistence;
using Xunit;

namespace LogisticaUltimaMilla.Integration.Tests.Postgres;

/// <summary>
/// Comprueba que la bandeja de salida se escribe en la misma transacción que la entrega:
/// los eventos nunca se pierden y nunca se guardan los de una operación fallida (AJ-01).
/// </summary>
public class PostgresOutboxTests : PostgresTestBase
{
    private static readonly DateTimeOffset Now = new(2026, 11, 13, 10, 0, 0, TimeSpan.Zero);

    [RequiresPostgresFact]
    public async Task Save_AssignedDelivery_QueuesExactlyOnePendingMessage()
    {
        var repository = new PostgresDeliveryRepository(DataSource);
        var deliveryId = await PostgresTestData.SeedPendingDeliveryAsync(repository);
        var courierId = CourierId.New();

        var delivery = await repository.GetAsync(deliveryId);
        delivery!.AssignCourier(courierId, Now, "Sandra");
        await repository.SaveAsync(delivery);

        var message = Assert.Single(await PostgresTestData.GetOutboxRowsAsync(DataSource, deliveryId));
        Assert.Equal(OutboxEventSerializer.DeliveryAssignedV1, message.MessageType);
        Assert.Equal("Pending", message.Status);
        Assert.Equal(0, message.Attempts);

        using var payload = JsonDocument.Parse(message.Payload);
        Assert.Equal(deliveryId.Value, payload.RootElement.GetProperty("deliveryId").GetGuid());
        Assert.Equal(courierId.Value, payload.RootElement.GetProperty("courierId").GetGuid());
        Assert.Equal("Sandra", payload.RootElement.GetProperty("requestedBy").GetString());
    }

    [RequiresPostgresFact]
    public async Task Save_NewDelivery_QueuesNoMessages()
    {
        var repository = new PostgresDeliveryRepository(DataSource);

        var deliveryId = await PostgresTestData.SeedPendingDeliveryAsync(repository);

        Assert.Empty(await PostgresTestData.GetOutboxRowsAsync(DataSource, deliveryId));
    }

    [RequiresPostgresFact]
    public async Task Save_WithStaleVersion_QueuesNoMessageForTheFailedOperation()
    {
        var repository = new PostgresDeliveryRepository(DataSource);
        var deliveryId = await PostgresTestData.SeedPendingDeliveryAsync(repository);

        var first = await repository.GetAsync(deliveryId);
        var second = await repository.GetAsync(deliveryId);
        first!.AssignCourier(CourierId.New(), Now, "Sandra");
        second!.AssignCourier(CourierId.New(), Now, "Julián");

        await repository.SaveAsync(first);
        await Assert.ThrowsAsync<DeliveryConcurrencyException>(() => repository.SaveAsync(second));

        // Solo existe el mensaje de la operación que sí se guardó.
        var message = Assert.Single(await PostgresTestData.GetOutboxRowsAsync(DataSource, deliveryId));

        using var payload = JsonDocument.Parse(message.Payload);
        Assert.Equal("Sandra", payload.RootElement.GetProperty("requestedBy").GetString());
    }

    [RequiresPostgresFact]
    public async Task Payload_NeverContainsAddressesOrOtherPersonalData()
    {
        var repository = new PostgresDeliveryRepository(DataSource);
        var deliveryId = await PostgresTestData.SeedPendingDeliveryAsync(repository);

        var delivery = await repository.GetAsync(deliveryId);
        delivery!.AssignCourier(CourierId.New(), Now, "Sandra");
        await repository.SaveAsync(delivery);

        var message = Assert.Single(await PostgresTestData.GetOutboxRowsAsync(DataSource, deliveryId));

        Assert.DoesNotContain(delivery.Address.Value, message.Payload);
        Assert.DoesNotContain("Carrera", message.Payload);
    }
}
