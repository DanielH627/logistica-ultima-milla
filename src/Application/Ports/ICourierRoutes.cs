using LogisticaUltimaMilla.Domain.Shared;

namespace LogisticaUltimaMilla.Application.Ports;

/// <summary>
/// Puerto de salida hacia el contexto Ruteo (contratos C2 y C3 del documento 05).
/// Expresa lo que Entregas necesita saber y pedir, en el lenguaje de Entregas:
/// no expone rutas, capacidades ni paradas.
/// </summary>
public interface ICourierRoutes
{
    /// <summary>C2: consulta si el repartidor puede recibir otra entrega.</summary>
    /// <exception cref="CourierRoutesUnavailableException">Ruteo no respondió.</exception>
    Task<CourierAvailability> GetAvailabilityAsync(CourierId courierId, CancellationToken cancellationToken = default);

    /// <summary>
    /// C3: reserva un cupo en la ruta del repartidor para la entrega. Ruteo protege su capacidad (INV04)
    /// de forma atómica, por lo que dos reservas simultáneas no pueden superar el cupo.
    /// </summary>
    /// <exception cref="CourierRoutesUnavailableException">Ruteo no respondió.</exception>
    Task<StopReservationStatus> ReserveStopAsync(CourierId courierId, DeliveryId deliveryId, CancellationToken cancellationToken = default);

    /// <summary>C3: libera un cupo reservado. Se usa si la asignación no pudo completarse.</summary>
    Task ReleaseStopAsync(CourierId courierId, DeliveryId deliveryId, CancellationToken cancellationToken = default);
}

public enum UnavailabilityReason
{
    None,
    CourierNotActive,
    OutOfShift,
    RouteSaturated
}

/// <summary>Respuesta de Ruteo a la consulta de disponibilidad, expresada en el lenguaje de Entregas.</summary>
public sealed record CourierAvailability(bool IsAvailable, UnavailabilityReason Reason)
{
    public static CourierAvailability Available() => new(true, UnavailabilityReason.None);

    public static CourierAvailability Unavailable(UnavailabilityReason reason)
    {
        if (reason == UnavailabilityReason.None)
        {
            throw new ArgumentException("Un repartidor no disponible debe indicar el motivo.", nameof(reason));
        }

        return new CourierAvailability(false, reason);
    }
}

public enum StopReservationStatus
{
    Reserved,
    CourierNotActive,
    RouteSaturated
}

/// <summary>Ruteo no respondió o no pudo atender la solicitud (política POL01).</summary>
public sealed class CourierRoutesUnavailableException : Exception
{
    public CourierRoutesUnavailableException(string message) : base(message)
    {
    }

    public CourierRoutesUnavailableException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
