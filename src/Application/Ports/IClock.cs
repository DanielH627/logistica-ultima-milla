namespace LogisticaUltimaMilla.Application.Ports;

/// <summary>Puerto de salida: la hora actual. Permite que el dominio no consulte el reloj del sistema.</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
