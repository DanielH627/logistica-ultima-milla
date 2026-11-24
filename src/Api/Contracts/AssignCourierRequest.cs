namespace LogisticaUltimaMilla.Api.Contracts;

/// <summary>Cuerpo de la solicitud: el repartidor elegido. La entrega va en la ruta y quién solicita, en un encabezado.</summary>
public sealed record AssignCourierRequest(Guid CourierId);
