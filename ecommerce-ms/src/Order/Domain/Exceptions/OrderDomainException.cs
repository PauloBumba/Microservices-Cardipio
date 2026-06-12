namespace Order.Domain.Exceptions;
public sealed class OrderDomainException(string message) : Exception(message);
