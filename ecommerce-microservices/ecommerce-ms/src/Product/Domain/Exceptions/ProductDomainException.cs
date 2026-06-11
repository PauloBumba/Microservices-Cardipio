namespace Product.Domain.Exceptions;
public sealed class ProductDomainException(string message) : Exception(message);
