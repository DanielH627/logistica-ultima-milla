using System.Text.Json;
using LogisticaUltimaMilla.Application.Ports;
using LogisticaUltimaMilla.Domain.Shared;

namespace LogisticaUltimaMilla.Infrastructure.Outbox;

/// <summary>
/// Solicita al repartidor el aviso de asignación (contrato C5) a partir del mensaje DeliveryAssigned.v1.
/// Si la solicitud falla, la excepción sube al procesador, que reintenta; la asignación ya guardada NO se deshace (ADR-005).
/// Entrega "al menos una vez": un reintento puede repetir el aviso, lo que se acepta porque el aviso es informativo.
/// </summary>
public sealed class DeliveryAssignedNotificationHandler : IOutboxMessageHandler
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    private readonly INotificationRequester _notifications;

    public DeliveryAssignedNotificationHandler(INotificationRequester notifications) => _notifications = notifications;

    public string MessageType => OutboxEventSerializer.DeliveryAssignedV1;

    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<DeliveryAssignedV1Payload>(message.Payload, Options)
            ?? throw new InvalidOperationException("El mensaje no tiene contenido.");

        await _notifications.RequestCourierAssignedNoticeAsync(
            new CourierId(payload.CourierId),
            new DeliveryId(payload.DeliveryId),
            cancellationToken);
    }
}
