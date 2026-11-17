using LogisticaUltimaMilla.Domain.Shared;

namespace LogisticaUltimaMilla.Domain.Entregas;

/// <summary>
/// Raíz del agregado Entrega. Protege sus propias invariantes:
/// INV01 (un solo repartidor activo), INV02 (solo se asigna si está pendiente)
/// e INV03 (estado y repartidor coherentes).
/// No conoce bases de datos, servicios externos ni al repartidor: solo su identificador.
/// </summary>
public sealed class Delivery
{
    private readonly List<IDomainEvent> _domainEvents = new();

    private Delivery(
        DeliveryId id,
        OrderId orderId,
        DeliveryAddress address,
        DeliveryWindow window,
        DeliveryPriority priority,
        DeliveryStatus status,
        CourierId? courierId,
        DateTimeOffset? assignedAt,
        int version)
    {
        Id = id;
        OrderId = orderId;
        Address = address;
        Window = window;
        Priority = priority;
        Status = status;
        CourierId = courierId;
        AssignedAt = assignedAt;
        Version = version;
    }

    public DeliveryId Id { get; }

    public OrderId OrderId { get; }

    public DeliveryAddress Address { get; }

    public DeliveryWindow Window { get; }

    public DeliveryPriority Priority { get; }

    public DeliveryStatus Status { get; private set; }

    /// <summary>Repartidor activo. Vacío mientras la entrega esté pendiente de asignación.</summary>
    public CourierId? CourierId { get; private set; }

    public DateTimeOffset? AssignedAt { get; private set; }

    /// <summary>
    /// Token de concurrencia optimista. Lo establece la persistencia al reconstruir la entrega;
    /// el dominio nunca lo modifica.
    /// </summary>
    public int Version { get; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>Crea una entrega nueva, pendiente de asignación y sin repartidor.</summary>
    public static Delivery CreatePending(
        DeliveryId id,
        OrderId orderId,
        DeliveryAddress address,
        DeliveryWindow window,
        DeliveryPriority priority)
    {
        return new Delivery(id, orderId, address, window, priority, DeliveryStatus.PendingAssignment, null, null, 0);
    }

    /// <summary>
    /// Reconstruye una entrega ya existente (por ejemplo, desde el almacenamiento).
    /// No ejecuta transiciones, pero verifica que el estado y el repartidor sean coherentes (INV03).
    /// </summary>
    public static Delivery Restore(
        DeliveryId id,
        OrderId orderId,
        DeliveryAddress address,
        DeliveryWindow window,
        DeliveryPriority priority,
        DeliveryStatus status,
        CourierId? courierId,
        DateTimeOffset? assignedAt,
        int version)
    {
        if (status == DeliveryStatus.PendingAssignment && courierId is not null)
        {
            throw new InvalidValueException("Una entrega pendiente de asignación no puede tener repartidor.");
        }

        if ((status == DeliveryStatus.Assigned || status == DeliveryStatus.InTransit) && courierId is null)
        {
            throw new InvalidValueException("Una entrega asignada o en camino debe tener repartidor.");
        }

        return new Delivery(id, orderId, address, window, priority, status, courierId, assignedAt, version);
    }

    /// <summary>
    /// Asigna un repartidor a la entrega. La hora y quién solicita entran como parámetros:
    /// el dominio no consulta el reloj del sistema.
    /// </summary>
    public void AssignCourier(CourierId courierId, DateTimeOffset assignedAt, string requestedBy)
    {
        ArgumentNullException.ThrowIfNull(courierId);

        if (string.IsNullOrWhiteSpace(requestedBy))
        {
            throw new InvalidValueException("Debe indicarse quién solicita la asignación.");
        }

        // INV01: no puede haber más de un repartidor activo.
        if (Status == DeliveryStatus.Assigned && CourierId is not null)
        {
            throw new DeliveryAlreadyAssignedException(Id, CourierId);
        }

        // INV02: solo se asigna una entrega pendiente.
        if (Status != DeliveryStatus.PendingAssignment)
        {
            throw new DeliveryNotPendingException(Id, Status);
        }

        CourierId = courierId;
        Status = DeliveryStatus.Assigned;
        AssignedAt = assignedAt;

        // INV06: la asignación deja un hecho registrable (quién, qué y cuándo).
        _domainEvents.Add(new DeliveryAssigned(Id, courierId, assignedAt, requestedBy));
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}
