using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace LogisticaUltimaMilla.Api.Security;

/// <summary>
/// Exige una clave de API válida en el encabezado X-Api-Key.
/// La comparación toma el mismo tiempo sin importar en qué posición falle, y la respuesta
/// no revela si la clave faltaba o era incorrecta. Es una protección mínima: no identifica a cada usuario.
/// </summary>
public sealed class ApiKeyFilter : IEndpointFilter
{
    public const string HeaderName = "X-Api-Key";

    private readonly byte[] _expectedKey;

    public ApiKeyFilter(IOptions<SecurityOptions> options)
    {
        _expectedKey = Encoding.UTF8.GetBytes(options.Value.ApiKey);
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var providedKey = Encoding.UTF8.GetBytes(context.HttpContext.Request.Headers[HeaderName].ToString());

        if (!CryptographicOperations.FixedTimeEquals(providedKey, _expectedKey))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "No autorizado",
                detail: "La clave de API es obligatoria y debe ser válida.",
                extensions: new Dictionary<string, object?> { ["code"] = "UNAUTHORIZED" });
        }

        return await next(context);
    }
}
