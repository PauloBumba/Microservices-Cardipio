namespace Shared.Domain.Primitives;

/// <summary>
/// Raiz de agregação com suporte a Domain Events e versionamento otimista.
/// </summary>
public abstract class AggregateRoot : EntityBase
{
    private readonly List<IDomainEvent> _events = [];

    /// <summary>Versão do agregado — usada para concorrência otimista.</summary>
    public int AggregateVersion { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _events.AsReadOnly();

    protected void Raise(IDomainEvent @event)
    {
        _events.Add(@event);
        AggregateVersion++;
    }

    public void ClearDomainEvents() => _events.Clear();
}
