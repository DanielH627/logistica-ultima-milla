namespace LogisticaUltimaMilla.Domain.Shared;

/// <summary>Base de los errores que expresan una regla de negocio violada.</summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }
}

/// <summary>Se lanza cuando se intenta crear un objeto de valor con datos inválidos (INV07).</summary>
public sealed class InvalidValueException : DomainException
{
    public InvalidValueException(string message) : base(message)
    {
    }
}
