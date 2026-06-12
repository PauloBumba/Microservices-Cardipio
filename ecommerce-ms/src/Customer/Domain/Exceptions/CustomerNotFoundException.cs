namespace Customer.Domain.Exceptions;
public sealed class CustomerNotFoundException(Guid id) : Exception($"Cliente {id} não encontrado.");
