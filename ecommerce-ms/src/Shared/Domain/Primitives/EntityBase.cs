namespace Shared.Domain.Primitives;
public abstract class EntityBase
{
    public Guid Id { get; protected init; } = Guid.NewGuid();
    public override bool Equals(object? obj) =>
        obj is EntityBase e && e.GetType() == GetType() && e.Id == Id;
    public override int GetHashCode() => Id.GetHashCode();
}
