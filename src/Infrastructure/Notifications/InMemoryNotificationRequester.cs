using LogisticaUltimaMilla.Application.Ports;
using LogisticaUltimaMilla.Domain.Shared;

namespace LogisticaUltimaMilla.Infrastructure.Notifications;

/// <summary>Adaptador en memoria del puerto INotificationRequester: registra los avisos solicitados.</summary>
public sealed class InMemoryNotificationRequester : INotificationRequester
{
    private readonly List<(CourierId CourierId, DeliveryId DeliveryId)> _requests = new();
    private readonly object _gate = new();

    public IReadOnlyList<(CourierId CourierId, DeliveryId DeliveryId)> Requests
    {
        get
        {
            lock (_gate)
            {
                return _requests.ToList();
            }
        }
    }

    public Task RequestCourierAssignedNoticeAsync(CourierId courierId, DeliveryId deliveryId, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _requests.Add((courierId, deliveryId));
        }

        return Task.CompletedTask;
    }
}
