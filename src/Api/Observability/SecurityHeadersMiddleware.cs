namespace LogisticaUltimaMilla.Api.Observability;

/// <summary>Encabezados de seguridad básicos para una API: sin sniffing de tipo, sin caché de respuestas, sin referer.</summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["Cache-Control"] = "no-store";
            headers["Referrer-Policy"] = "no-referrer";
            headers["X-Frame-Options"] = "DENY";
            return Task.CompletedTask;
        });

        return _next(context);
    }
}
