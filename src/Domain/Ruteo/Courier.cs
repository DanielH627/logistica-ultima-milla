using LogisticaUltimaMilla.Domain.Shared;

namespace LogisticaUltimaMilla.Domain.Ruteo;

/// <summary>Repartidor visto desde Ruteo: un recurso que puede estar activo o inactivo.</summary>
public sealed class Courier
{
    public Courier(CourierId id, string name, bool isActive)
    {
        ArgumentNullException.ThrowIfNull(id);

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidValueException("El nombre del repartidor es obligatorio.");
        }

        Id = id;
        Name = name.Trim();
        IsActive = isActive;
    }

    public CourierId Id { get; }

    public string Name { get; }

    public bool IsActive { get; private set; }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
