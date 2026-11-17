using LogisticaUltimaMilla.Domain.Shared;

namespace LogisticaUltimaMilla.Domain.Entregas;

/// <summary>Intervalo prometido al cliente. El inicio debe ser anterior al fin (INV07).</summary>
public sealed record DeliveryWindow
{
    public DeliveryWindow(DateTimeOffset start, DateTimeOffset end)
    {
        if (start >= end)
        {
            throw new InvalidValueException("El inicio de la ventana de entrega debe ser anterior a su fin.");
        }

        Start = start;
        End = end;
    }

    public DateTimeOffset Start { get; }

    public DateTimeOffset End { get; }
}
