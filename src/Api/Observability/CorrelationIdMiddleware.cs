namespace LogisticaUltimaMilla.Api.Observability;

/// <summary>
/// Asigna a cada solicitud un identificador de correlación, lo devuelve en la respuesta y lo agrega
/// a todos los registros de esa solicitud. Permite seguir una solicitud a través de los logs.
/// Un valor recibido solo se acepta si es corto y de caracteres seguros: así no se puede inyectar texto en los logs.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ScopeKey = "CorrelationId";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers[HeaderName].ToString();
        var correlationId = IsAcceptable(incoming) ? incoming : Guid.NewGuid().ToString("N");

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (_logger.BeginScope(new Dictionary<string, object> { [ScopeKey] = correlationId }))
        {
            await _next(context);
        }
    }

    internal static bool IsAcceptable(string value) =>
        value.Length is > 0 and <= 64
        && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
}
