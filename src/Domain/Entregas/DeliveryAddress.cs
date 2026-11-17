using LogisticaUltimaMilla.Domain.Shared;

namespace LogisticaUltimaMilla.Domain.Entregas;

/// <summary>Dirección de entrega. Se compara por valor y no puede estar vacía (INV07).</summary>
public sealed record DeliveryAddress
{
    public DeliveryAddress(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidValueException("La dirección es obligatoria.");
        }

        Value = value.Trim();
    }

    public string Value { get; }

    public override string ToString() => Value;
}
