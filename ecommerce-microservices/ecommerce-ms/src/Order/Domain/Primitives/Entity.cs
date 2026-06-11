namespace Order.Domain.Primitives;
public abstract class Entity
{
    private readonly List<IDomainEvent> _events = [];
    public Guid Id { get; protected init; }
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _events.AsReadOnly();
    protected void Raise(IDomainEvent ev) => _events.Add(ev);
    public void ClearDomainEvents() => _events.Clear();
}
