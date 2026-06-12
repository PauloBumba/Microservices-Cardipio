using Product.Domain.Exceptions;
namespace Product.Domain.ValueObjects;
public sealed class Money
{
    private static readonly HashSet<string> _valid = ["BRL", "USD", "EUR"];
    public decimal Amount { get; }
    public string Currency { get; }
    private Money(decimal amount, string currency) { Amount = amount; Currency = currency; }
    public static Money Create(decimal amount, string currency)
    {
        if (amount < 0) throw new ProductDomainException("Valor negativo.");
        var u = currency.ToUpperInvariant();
        if (!_valid.Contains(u)) throw new ProductDomainException($"Moeda inválida: {currency}.");
        return new Money(Math.Round(amount, 2), u);
    }
    public static Money Zero(string currency = "BRL") => new(0m, currency.ToUpperInvariant());
    public override string ToString() => $"{Currency} {Amount:F2}";
    public override bool Equals(object? o) => o is Money m && Amount == m.Amount && Currency == m.Currency;
    public override int GetHashCode() => HashCode.Combine(Amount, Currency);
}
