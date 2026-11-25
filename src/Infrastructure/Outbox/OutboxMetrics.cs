using System.Diagnostics.Metrics;

namespace LogisticaUltimaMilla.Infrastructure.Outbox;

/// <summary>
/// Métricas de la bandeja de salida. Usan System.Diagnostics.Metrics, que es estándar de .NET:
/// se pueden observar con "dotnet-counters" o exportar a cualquier sistema de monitoreo sin cambiar este código.
/// Las etiquetas son de baja cardinalidad (nunca identificadores de entregas ni de repartidores).
/// </summary>
public static class OutboxMetrics
{
    public const string MeterName = "LogisticaUltimaMilla.Outbox";
    public const string MessagesCounterName = "logistica.outbox.messages";

    private static readonly Meter Meter = new(MeterName);

    private static readonly Counter<long> Messages = Meter.CreateCounter<long>(
        MessagesCounterName,
        unit: "{message}",
        description: "Mensajes procesados por la bandeja de salida, por resultado (sent, retry, dead).");

    public static void Record(string outcome) => Messages.Add(1, new KeyValuePair<string, object?>("outcome", outcome));
}
