using Order.Domain.Exceptions;
namespace Order.Domain.ValueObjects;
public sealed class Money
{
    public decimal Amount { get; }
    public string Currency { get; }
    private Money(decimal amount, string currency) { Amount=amount; Currency=currency; }
    public static Money Create(decimal amount, string currency)
    {
        if (amount < 0) throw new OrderDomainException("Valor negativo.");
        return new Money(Math.Round(amount,2), currency.ToUpperInvariant());
    }
    public static Money Zero(string currency="BRL") => new(0m, currency.ToUpperInvariant());
    public Money Add(Money other)
    {
        if (Currency != other.Currency) throw new OrderDomainException("Moedas diferentes.");
        return new Money(Amount + other.Amount, Currency);
    }
}
