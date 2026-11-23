using System.Text.Json;
using LogisticaUltimaMilla.Domain.Entregas;
using LogisticaUltimaMilla.Domain.Shared;
using LogisticaUltimaMilla.Infrastructure.Outbox;
using Xunit;

namespace LogisticaUltimaMilla.Integration.Tests.Outbox;

/// <summary>El contrato del mensaje es independiente de la base de datos: se prueba sin infraestructura.</summary>
public class OutboxEventSerializerTests
{
    private sealed record UnknownEvent(DateTimeOffset OccurredAt) : IDomainEvent;

    [Fact]
    public void ToMessage_DeliveryAssigned_ProducesAVersionedMessageWithOnlyIdentifiers()
    {
        var deliveryId = DeliveryId.New();
        var courierId = CourierId.New();
        var occurredAt = new DateTimeOffset(2026, 11, 13, 10, 0, 0, TimeSpan.Zero);
        var domainEvent = new DeliveryAssigned(deliveryId, courierId, occurredAt, "Sandra");

        var message = OutboxEventSerializer.ToMessage(domainEvent);

        Assert.Equal("DeliveryAssigned.v1", message.MessageType);
        Assert.Equal(deliveryId.Value, message.AggregateId);
        Assert.Equal(occurredAt, message.OccurredAt);

        using var payload = JsonDocument.Parse(message.Payload);
        var properties = payload.RootElement.EnumerateObject().Select(property => property.Name).ToList();

        Assert.Equal(new[] { "deliveryId", "courierId", "occurredAt", "requestedBy" }, properties);
        Assert.Equal(deliveryId.Value, payload.RootElement.GetProperty("deliveryId").GetGuid());
        Assert.Equal(courierId.Value, payload.RootElement.GetProperty("courierId").GetGuid());
    }

    [Fact]
    public void ToMessage_EventWithoutAContract_Throws()
    {
        var unknown = new UnknownEvent(DateTimeOffset.UtcNow);

        Assert.Throws<NotSupportedException>(() => OutboxEventSerializer.ToMessage(unknown));
    }
}
