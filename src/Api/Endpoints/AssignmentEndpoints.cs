using LogisticaUltimaMilla.Api.Contracts;
using LogisticaUltimaMilla.Api.Security;
using LogisticaUltimaMilla.Application.Entregas;
using Microsoft.AspNetCore.Mvc;

namespace LogisticaUltimaMilla.Api.Endpoints;

/// <summary>
/// Punto de entrada para asignar un repartidor a una entrega.
/// Solo valida la forma de los datos, los traduce al comando del caso de uso y convierte el resultado
/// en una respuesta HTTP. No contiene reglas de negocio ni conoce el dominio.
/// </summary>
public static class AssignmentEndpoints
{
    public const string RequestedByHeader = "X-Requested-By";

    public static IEndpointRouteBuilder MapAssignmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1").AddEndpointFilter<ApiKeyFilter>();

        group.MapPost("/deliveries/{deliveryId:guid}/assignment", AssignCourierAsync)
            .WithName("AssignCourier");

        return app;
    }

    private static async Task<IResult> AssignCourierAsync(
        Guid deliveryId,
        AssignCourierRequest? request,
        [FromHeader(Name = RequestedByHeader)] string? requestedBy,
        AssignCourierUseCase useCase,
        CancellationToken cancellationToken)
    {
        var errors = Validate(deliveryId, request, requestedBy);
        if (errors.Count > 0)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Solicitud inválida",
                detail: "Los datos enviados no cumplen el formato esperado.",
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = "INVALID_REQUEST",
                    ["errors"] = errors
                });
        }

        var command = new AssignCourierCommand(deliveryId, request!.CourierId, requestedBy!.Trim());
        var result = await useCase.ExecuteAsync(command, cancellationToken);

        if (result.IsSuccess)
        {
            return Results.Ok(new AssignCourierResponse(
                deliveryId,
                request.CourierId,
                result.AssignedAt!.Value,
                result.NoticeQueued));
        }

        return ToProblem(result);
    }

    private static Dictionary<string, string[]> Validate(Guid deliveryId, AssignCourierRequest? request, string? requestedBy)
    {
        var errors = new Dictionary<string, string[]>();

        if (deliveryId == Guid.Empty)
        {
            errors["deliveryId"] = new[] { "El identificador de la entrega es obligatorio." };
        }

        if (request is null || request.CourierId == Guid.Empty)
        {
            errors["courierId"] = new[] { "El identificador del repartidor es obligatorio." };
        }

        if (string.IsNullOrWhiteSpace(requestedBy))
        {
            errors[RequestedByHeader] = new[] { "Debe indicarse quién solicita la asignación." };
        }
        else if (requestedBy.Length > 100 || requestedBy.Any(char.IsControl))
        {
            errors[RequestedByHeader] = new[] { "El valor es demasiado largo o contiene caracteres no permitidos." };
        }

        return errors;
    }

    private static IResult ToProblem(AssignCourierResult result)
    {
        var status = result.ErrorCode switch
        {
            AssignCourierErrorCodes.DeliveryNotFound => StatusCodes.Status404NotFound,
            AssignCourierErrorCodes.DeliveryAlreadyAssigned => StatusCodes.Status409Conflict,
            AssignCourierErrorCodes.DeliveryNotPending => StatusCodes.Status409Conflict,
            AssignCourierErrorCodes.RouteSaturated => StatusCodes.Status409Conflict,
            AssignCourierErrorCodes.CourierNotActive => StatusCodes.Status422UnprocessableEntity,
            AssignCourierErrorCodes.AvailabilityUnverifiable => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Problem(
            statusCode: status,
            title: "No se pudo completar la asignación",
            detail: result.ErrorMessage,
            extensions: new Dictionary<string, object?> { ["code"] = result.ErrorCode });
    }
}
