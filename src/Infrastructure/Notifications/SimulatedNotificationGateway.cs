using LogisticaUltimaMilla.Application.Ports;
using LogisticaUltimaMilla.Domain.Shared;
using Microsoft.Extensions.Logging;

namespace LogisticaUltimaMilla.Infrastructure.Notifications;

/// <summary>
/// Simula el sistema de notificaciones externo (contrato C5): solo escribe un registro con identificadores.
/// Con simulateFailure = true siempre falla, para demostrar los reintentos y los mensajes "Dead".
/// En un entorno real, este adaptador se reemplaza por el cliente del servicio de avisos.
/// </summary>
public sealed class SimulatedNotificationGateway : INotificationRequester
{
    private readonly bool _simulateFailure;
    private readonly ILogger<SimulatedNotificationGateway> _logger;

    public SimulatedNotificationGateway(bool simulateFailure, ILogger<SimulatedNotificationGateway> logger)
    {
        _simulateFailure = simulateFailure;
        _logger = logger;
    }

    public Task RequestCourierAssignedNoticeAsync(
        CourierId courierId, DeliveryId deliveryId, CancellationToken cancellationToken = default)
    {
        if (_simulateFailure)
        {
            throw new NotificationRequestFailedException("El sistema de notificaciones simulado está configurado para fallar.");
        }

        // Solo identificadores: ni nombres ni direcciones ni datos de contacto.
        _logger.LogInformation(
            "Aviso de asignación solicitado: repartidor {CourierId}, entrega {DeliveryId}", courierId, deliveryId);
        return Task.CompletedTask;
    }
}
