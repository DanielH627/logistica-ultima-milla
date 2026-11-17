using LogisticaUltimaMilla.Domain.Shared;

namespace LogisticaUltimaMilla.Application.Ports;

/// <summary>Puerto de salida: registra o publica los hechos ocurridos (contrato C4, historial de eventos).</summary>
public interface IDomainEventPublisher
{
    Task PublishAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken cancellationToken = default);
}
