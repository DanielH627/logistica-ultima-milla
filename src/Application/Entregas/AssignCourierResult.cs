namespace LogisticaUltimaMilla.Application.Entregas;

/// <summary>Códigos de rechazo estables del contrato de aplicación (documento 05, sección 5).</summary>
public static class AssignCourierErrorCodes
{
    public const string DeliveryNotFound = "DELIVERY_NOT_FOUND";
    public const string DeliveryAlreadyAssigned = "DELIVERY_ALREADY_ASSIGNED";
    public const string DeliveryNotPending = "DELIVERY_NOT_PENDING";
    public const string CourierNotActive = "COURIER_NOT_ACTIVE";
    public const string RouteSaturated = "ROUTE_SATURATED";
    public const string AvailabilityUnverifiable = "AVAILABILITY_UNVERIFIABLE";
}

/// <summary>
/// Contrato de salida del caso de uso. Un rechazo de negocio es un resultado esperado,
/// no una excepción: el consumidor recibe un código estable y un mensaje legible.
/// </summary>
public sealed record AssignCourierResult(
    bool IsSuccess,
    string? ErrorCode,
    string? ErrorMessage,
    DateTimeOffset? AssignedAt,
    bool NoticeRequested)
{
    public static AssignCourierResult Success(DateTimeOffset assignedAt, bool noticeRequested)
        => new(true, null, null, assignedAt, noticeRequested);

    public static AssignCourierResult Failure(string errorCode, string errorMessage)
        => new(false, errorCode, errorMessage, null, false);
}
