using LogisticaUltimaMilla.Application.Ports;
using LogisticaUltimaMilla.Domain.Entregas;
using LogisticaUltimaMilla.Domain.Shared;

namespace LogisticaUltimaMilla.Application.Entregas;

/// <summary>
/// Caso de uso: asignar un repartidor a una entrega.
/// Coordina la intención. Las reglas del negocio viven en el dominio (Delivery, Route);
/// este caso de uso solo las invoca, consulta los puertos y decide ante las fallas.
/// Recibe sus dependencias por constructor y solo conoce interfaces.
/// </summary>
public sealed class AssignCourierUseCase
{
    private readonly IDeliveryRepository _deliveries;
    private readonly ICourierRoutes _courierRoutes;
    private readonly IDomainEventPublisher _events;
    private readonly INotificationRequester _notifications;
    private readonly IClock _clock;

    public AssignCourierUseCase(
        IDeliveryRepository deliveries,
        ICourierRoutes courierRoutes,
        IDomainEventPublisher events,
        INotificationRequester notifications,
        IClock clock)
    {
        _deliveries = deliveries;
        _courierRoutes = courierRoutes;
        _events = events;
        _notifications = notifications;
        _clock = clock;
    }

    public async Task<AssignCourierResult> ExecuteAsync(
        AssignCourierCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var deliveryId = new DeliveryId(command.DeliveryId);
        var courierId = new CourierId(command.CourierId);

        // 1 y 2. Cargar la entrega y verificar que exista (PRE01).
        var delivery = await _deliveries.GetAsync(deliveryId, cancellationToken);
        if (delivery is null)
        {
            return AssignCourierResult.Failure(
                AssignCourierErrorCodes.DeliveryNotFound,
                $"No se encontró la entrega {command.DeliveryId}. Verifique el código e inténtelo de nuevo.");
        }

        // 3. Consultar la disponibilidad mediante un puerto, no mediante un servicio concreto (INV05, POL01).
        CourierAvailability availability;
        try
        {
            availability = await _courierRoutes.GetAvailabilityAsync(courierId, cancellationToken);
        }
        catch (CourierRoutesUnavailableException)
        {
            return AvailabilityUnverifiable();
        }

        if (!availability.IsAvailable)
        {
            return RejectBecauseUnavailable(availability.Reason, command.CourierId);
        }

        // 4. El dominio protege las invariantes de la entrega (INV01, INV02, INV03).
        try
        {
            delivery.AssignCourier(courierId, _clock.UtcNow, command.RequestedBy);
        }
        catch (DeliveryAlreadyAssignedException exception)
        {
            return AssignCourierResult.Failure(
                AssignCourierErrorCodes.DeliveryAlreadyAssigned,
                $"La entrega ya tiene asignado al repartidor {exception.CurrentCourierId}. Para cambiarlo debe solicitar una reasignación.");
        }
        catch (DeliveryNotPendingException exception)
        {
            return AssignCourierResult.Failure(
                AssignCourierErrorCodes.DeliveryNotPending,
                $"La entrega se encuentra en estado {exception.CurrentStatus}. Solo se pueden asignar entregas pendientes de asignación.");
        }

        // 5. Reservar el cupo en la ruta. Ruteo protege su capacidad (INV04) de forma atómica.
        //    Si no hay cupo, la entrega no se guarda y no queda ningún cambio.
        StopReservationStatus reservation;
        try
        {
            reservation = await _courierRoutes.ReserveStopAsync(courierId, deliveryId, cancellationToken);
        }
        catch (CourierRoutesUnavailableException)
        {
            return AvailabilityUnverifiable();
        }

        if (reservation != StopReservationStatus.Reserved)
        {
            return reservation == StopReservationStatus.RouteSaturated
                ? RejectBecauseUnavailable(UnavailabilityReason.RouteSaturated, command.CourierId)
                : RejectBecauseUnavailable(UnavailabilityReason.CourierNotActive, command.CourierId);
        }

        // 6. Guardar. Si otra operación modificó la entrega, se libera el cupo reservado (compensación).
        try
        {
            await _deliveries.SaveAsync(delivery, cancellationToken);
        }
        catch (DeliveryConcurrencyException)
        {
            await _courierRoutes.ReleaseStopAsync(courierId, deliveryId, cancellationToken);

            return AssignCourierResult.Failure(
                AssignCourierErrorCodes.DeliveryAlreadyAssigned,
                "La entrega fue asignada por otra operación al mismo tiempo. Verifique su estado actual.");
        }
        catch
        {
            await _courierRoutes.ReleaseStopAsync(courierId, deliveryId, cancellationToken);
            throw;
        }

        // 7. Registrar el resultado mediante un adaptador (INV06).
        await _events.PublishAsync(delivery.DomainEvents, cancellationToken);
        delivery.ClearDomainEvents();

        // 8. Solicitar el aviso al repartidor. Si falla, la asignación se mantiene (contrato C5).
        var noticeRequested = true;
        try
        {
            await _notifications.RequestCourierAssignedNoticeAsync(courierId, deliveryId, cancellationToken);
        }
        catch (NotificationRequestFailedException)
        {
            noticeRequested = false;
        }

        return AssignCourierResult.Success(delivery.AssignedAt ?? _clock.UtcNow, noticeRequested);
    }

    private static AssignCourierResult AvailabilityUnverifiable()
        => AssignCourierResult.Failure(
            AssignCourierErrorCodes.AvailabilityUnverifiable,
            "No fue posible verificar la disponibilidad del repartidor en este momento. La entrega continúa pendiente; intente nuevamente en unos minutos.");

    private static AssignCourierResult RejectBecauseUnavailable(UnavailabilityReason reason, Guid courierId)
    {
        if (reason == UnavailabilityReason.RouteSaturated)
        {
            return AssignCourierResult.Failure(
                AssignCourierErrorCodes.RouteSaturated,
                $"La ruta del repartidor {courierId} ya alcanzó su capacidad. Seleccione otro repartidor.");
        }

        return AssignCourierResult.Failure(
            AssignCourierErrorCodes.CourierNotActive,
            $"El repartidor {courierId} no se encuentra activo. Seleccione otro repartidor.");
    }
}
