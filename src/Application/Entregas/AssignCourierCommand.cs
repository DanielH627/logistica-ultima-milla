namespace LogisticaUltimaMilla.Application.Entregas;

/// <summary>
/// Contrato de entrada del caso de uso. Usa tipos simples, sin objetos del dominio,
/// para que quien lo invoque (una API, una pantalla, una prueba) no dependa del modelo.
/// </summary>
/// <param name="DeliveryId">Entrega a asignar.</param>
/// <param name="CourierId">Repartidor elegido.</param>
/// <param name="RequestedBy">Quién solicita la asignación (por ejemplo, el coordinador).</param>
public sealed record AssignCourierCommand(Guid DeliveryId, Guid CourierId, string RequestedBy);
