using System.Text.Json;
using LogisticaUltimaMilla.Domain.Entregas;
using LogisticaUltimaMilla.Domain.Shared;

namespace LogisticaUltimaMilla.Infrastructure.Outbox;

/// <summary>Un mensaje listo para guardarse en la bandeja de salida.</summary>
public sealed record OutboxMessageData(
    Guid AggregateId,
    string MessageType,
    string Payload,
    DateTimeOffset OccurredAt);

/// <summary>
/// Contenido versionado del mensaje DeliveryAssigned.v1 (contrato C4).
/// Solo lleva identificadores y la hora: nunca direcciones, nombres ni datos de contacto.
/// </summary>
public sealed record DeliveryAssignedV1Payload(
    Guid DeliveryId,
    Guid CourierId,
    DateTimeOffset OccurredAt,
    string RequestedBy);

/// <summary>
/// Traduce los eventos de dominio a mensajes con un contrato propio y versionado.
/// El contrato del mensaje es independiente del evento: el dominio puede cambiar sin romper a los consumidores.
/// </summary>
public static class OutboxEventSerializer
{
    public const string DeliveryAssignedV1 = "DeliveryAssigned.v1";

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static OutboxMessageData ToMessage(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        switch (domainEvent)
        {
            case DeliveryAssigned assigned:
                var payload = new DeliveryAssignedV1Payload(
                    assigned.DeliveryId.Value,
                    assigned.CourierId.Value,
                    assigned.OccurredAt,
                    assigned.RequestedBy);

                return new OutboxMessageData(
                    assigned.DeliveryId.Value,
                    DeliveryAssignedV1,
                    JsonSerializer.Serialize(payload, Options),
                    assigned.OccurredAt);

            default:
                throw new NotSupportedException(
                    $"El evento {domainEvent.GetType().Name} no tiene un contrato de mensaje definido.");
        }
    }
}
