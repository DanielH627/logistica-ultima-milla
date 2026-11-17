namespace LogisticaUltimaMilla.Domain.Shared;

/// <summary>Hecho ocurrido en el dominio. Se nombra en pasado: "entrega asignada".</summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
