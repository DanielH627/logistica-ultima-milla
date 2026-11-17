namespace LogisticaUltimaMilla.Domain.Shared;

// Identificadores compartidos entre contextos. Son lo único que un contexto
// conoce de un objeto de otro contexto: se referencian por identificador, nunca por objeto.

public sealed record DeliveryId
{
    public DeliveryId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new InvalidValueException("El identificador de la entrega no puede estar vacío.");
        }

        Value = value;
    }

    public Guid Value { get; }

    public static DeliveryId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}

public sealed record CourierId
{
    public CourierId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new InvalidValueException("El identificador del repartidor no puede estar vacío.");
        }

        Value = value;
    }

    public Guid Value { get; }

    public static CourierId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}

public sealed record OrderId
{
    public OrderId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new InvalidValueException("El identificador del pedido no puede estar vacío.");
        }

        Value = value;
    }

    public Guid Value { get; }

    public static OrderId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}

public sealed record RouteId
{
    public RouteId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new InvalidValueException("El identificador de la ruta no puede estar vacío.");
        }

        Value = value;
    }

    public Guid Value { get; }

    public static RouteId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
