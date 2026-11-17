using LogisticaUltimaMilla.Domain.Shared;

namespace LogisticaUltimaMilla.Domain.Ruteo;

/// <summary>Máximo de paradas que admite una ruta (supuesto A3-S02). Debe ser mayor que cero (INV07).</summary>
public sealed record RouteCapacity
{
    public RouteCapacity(int maxStops)
    {
        if (maxStops < 1)
        {
            throw new InvalidValueException("La capacidad debe ser mayor que cero.");
        }

        MaxStops = maxStops;
    }

    public int MaxStops { get; }
}
