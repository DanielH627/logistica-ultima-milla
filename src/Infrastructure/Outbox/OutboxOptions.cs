namespace LogisticaUltimaMilla.Infrastructure.Outbox;

/// <summary>Parámetros del procesador de la bandeja de salida (sección "Outbox" de la configuración).</summary>
public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    /// <summary>Si es falso, el servicio en segundo plano no se ejecuta (las pruebas lo invocan a mano).</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Intentos totales antes de marcar el mensaje como "Dead".</summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>Espera tras el primer fallo; se duplica en cada intento (5 s, 10 s, 20 s…).</summary>
    public int BaseDelaySeconds { get; set; } = 5;

    public int BatchSize { get; set; } = 20;

    public int PollIntervalSeconds { get; set; } = 2;
}
