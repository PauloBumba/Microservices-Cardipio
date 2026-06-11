namespace Product.Domain.Exceptions;
public sealed class ProductNotFoundException(Guid id) : Exception($"Produto {id} não encontrado.");
