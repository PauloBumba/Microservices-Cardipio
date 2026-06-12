namespace Order.Domain.Exceptions;
public sealed class OrderNotFoundException(Guid id) : Exception($"Pedido {id} não encontrado.");
