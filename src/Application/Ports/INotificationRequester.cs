using LogisticaUltimaMilla.Domain.Shared;

namespace LogisticaUltimaMilla.Application.Ports;

/// <summary>
/// Puerto de salida: solicita un aviso al repartidor (contrato C5).
/// La aplicación decide qué avisar; el canal lo decide el sistema de notificaciones.
/// </summary>
public interface INotificationRequester
{
    /// <exception cref="NotificationRequestFailedException">No se pudo solicitar el aviso.</exception>
    Task RequestCourierAssignedNoticeAsync(CourierId courierId, DeliveryId deliveryId, CancellationToken cancellationToken = default);
}

public sealed class NotificationRequestFailedException : Exception
{
    public NotificationRequestFailedException(string message) : base(message)
    {
    }

    public NotificationRequestFailedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
