using LogisticaUltimaMilla.Domain.Entregas;
using LogisticaUltimaMilla.Domain.Shared;

namespace LogisticaUltimaMilla.Application.Ports;

/// <summary>
/// Puerto de salida: lo que el caso de uso necesita para obtener y guardar entregas.
/// Quién lo implementa (base de datos, memoria, etc.) es un detalle de la infraestructura.
/// </summary>
public interface IDeliveryRepository
{
    Task<Delivery?> GetAsync(DeliveryId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Guarda la entrega y, en la misma operación, los eventos de dominio que tenga pendientes
    /// (bandeja de salida). Si la entrega no se puede guardar, tampoco se guardan sus eventos.
    /// </summary>
    /// <exception cref="DeliveryConcurrencyException">
    /// La entrega cambió desde que fue leída (control de concurrencia optimista).
    /// </exception>
    Task SaveAsync(Delivery delivery, CancellationToken cancellationToken = default);
}

/// <summary>Otra operación modificó la entrega entre su lectura y su guardado.</summary>
public sealed class DeliveryConcurrencyException : Exception
{
    public DeliveryConcurrencyException(DeliveryId deliveryId)
        : base($"La entrega {deliveryId} fue modificada por otra operación.")
    {
        DeliveryId = deliveryId;
    }

    public DeliveryId DeliveryId { get; }
}
