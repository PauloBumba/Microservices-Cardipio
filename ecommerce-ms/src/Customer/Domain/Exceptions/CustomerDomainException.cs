namespace Customer.Domain.Exceptions;
public sealed class CustomerDomainException(string message) : Exception(message);
