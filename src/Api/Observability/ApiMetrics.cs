using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace LogisticaUltimaMilla.Api.Observability;

/// <summary>
/// Métricas de la API (System.Diagnostics.Metrics). Etiquetas de baja cardinalidad:
/// plantilla de ruta (no la URL real), clase de estado HTTP y resultado de negocio.
/// </summary>
public static class ApiMetrics
{
    public const string MeterName = "LogisticaUltimaMilla.Api";
    public const string RequestsCounterName = "logistica.http.requests";
    public const string DurationName = "logistica.http.request.duration";
    public const string AssignmentsCounterName = "logistica.assignments";

    private static readonly Meter Meter = new(MeterName);

    private static readonly Counter<long> Requests = Meter.CreateCounter<long>(
        RequestsCounterName, unit: "{request}", description: "Solicitudes HTTP atendidas.");

    private static readonly Histogram<double> Duration = Meter.CreateHistogram<double>(
        DurationName, unit: "ms", description: "Duración de las solicitudes HTTP.");

    private static readonly Counter<long> Assignments = Meter.CreateCounter<long>(
        AssignmentsCounterName, unit: "{assignment}", description: "Intentos de asignación, por resultado.");

    public static void RecordRequest(string route, string method, int statusCode, double elapsedMs)
    {
        var tags = new TagList
        {
            { "route", route },
            { "method", method },
            { "status_class", $"{statusCode / 100}xx" }
        };

        Requests.Add(1, tags);
        Duration.Record(elapsedMs, tags);
    }

    /// <param name="result">"success" o el código de error de negocio (por ejemplo ROUTE_SATURATED).</param>
    public static void RecordAssignment(string result) =>
        Assignments.Add(1, new KeyValuePair<string, object?>("result", result));
}
