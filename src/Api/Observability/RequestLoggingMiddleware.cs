using System.Diagnostics;

namespace LogisticaUltimaMilla.Api.Observability;

/// <summary>
/// Registra una línea estructurada por solicitud y actualiza las métricas.
/// Solo registra método, plantilla de ruta, estado y duración. NUNCA registra la URL real, la cadena de consulta,
/// los encabezados (X-Api-Key, X-Requested-By) ni el cuerpo: esos datos pueden ser sensibles o personales.
/// </summary>
public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var started = Stopwatch.GetTimestamp();
        var endpointBefore = context.GetEndpoint();

        try
        {
            await _next(context);
        }
        finally
        {
            var elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            var route = RouteOf(context.GetEndpoint() ?? endpointBefore);
            var statusCode = context.Response.StatusCode;

            ApiMetrics.RecordRequest(route, context.Request.Method, statusCode, elapsedMs);

            // Las sondas de salud son frecuentes: se registran en un nivel que normalmente no se muestra.
            var level = route.StartsWith("/health", StringComparison.Ordinal) ? LogLevel.Debug
                : statusCode >= 500 ? LogLevel.Error
                : statusCode >= 400 ? LogLevel.Warning
                : LogLevel.Information;

            _logger.Log(
                level,
                "HTTP {Method} {Route} respondió {StatusCode} en {ElapsedMs:F1} ms",
                context.Request.Method, route, statusCode, elapsedMs);
        }
    }

    private static string RouteOf(Endpoint? endpoint) =>
        (endpoint as RouteEndpoint)?.RoutePattern.RawText ?? "(sin ruta)";
}
