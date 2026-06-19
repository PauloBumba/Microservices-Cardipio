namespace Shared.Application.Behaviors;

/// <summary>
/// Interface mínima que o TransactionBehavior precisa.
/// Cada microserviço faz seu DbContext implementar esta interface.
/// </summary>
public interface IUnitOfWorkAccessor
{
    Task CommitAsync(CancellationToken ct = default);
}
