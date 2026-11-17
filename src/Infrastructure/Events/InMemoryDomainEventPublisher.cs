using LogisticaUltimaMilla.Application.Ports;
using LogisticaUltimaMilla.Domain.Shared;

namespace LogisticaUltimaMilla.Infrastructure.Events;

/// <summary>Adaptador en memoria del puerto IDomainEventPublisher: guarda los eventos como un historial simple.</summary>
public sealed class InMemoryDomainEventPublisher : IDomainEventPublisher
{
    private readonly List<IDomainEvent> _published = new();
    private readonly object _gate = new();

    public IReadOnlyList<IDomainEvent> Published
    {
        get
        {
            lock (_gate)
            {
                return _published.ToList();
            }
        }
    }

    public Task PublishAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(events);

        lock (_gate)
        {
            _published.AddRange(events);
        }

        return Task.CompletedTask;
    }
}
