using LogisticaUltimaMilla.Application.Ports;

namespace LogisticaUltimaMilla.Infrastructure.Time;

/// <summary>Adaptador del puerto IClock que usa el reloj del sistema.</summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
