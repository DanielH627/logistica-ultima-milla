using LogisticaUltimaMilla.Domain.Shared;

namespace LogisticaUltimaMilla.Domain.Ruteo;

/// <summary>Punto de entrega dentro de una ruta. Se refiere a la entrega solo por su identificador.</summary>
public sealed record Stop
{
    public Stop(DeliveryId deliveryId, int position)
    {
        ArgumentNullException.ThrowIfNull(deliveryId);

        if (position < 1)
        {
            throw new InvalidValueException("La posición de la parada debe ser mayor que cero.");
        }

        DeliveryId = deliveryId;
        Position = position;
    }

    public DeliveryId DeliveryId { get; }

    public int Position { get; }
}
