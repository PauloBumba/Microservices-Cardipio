namespace Order.Domain.Primitives;
public abstract class Entity
{
    private readonly List<SharedIDomainEvent> _events = [];
    public Guid Id { get; protected init; }
    public IReadOnlyCollection<SharedIDomainEvent> DomainEvents => _events.AsReadOnly();
    protected void Raise(SharedIDomainEvent ev) => _events.Add(ev);
    public void ClearDomainEvents() => _events.Clear();
}
