using FluentAssertions;
using Product.Domain.Entities;
using Product.Domain.Events;
using Product.Domain.Exceptions;
using Product.Domain.ValueObjects;
using Xunit;

namespace Product.Domain.Tests;

public class ProductssTests
{
    private static Productss CreateValid(int stock = 10) =>
        Productss.Create("Tênis X", "Descrição", "SKU-001", 199.90m, "BRL", stock, "Calçados");

    // ── Create ──────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ValidData_ShouldReturnActiveProduct()
    {
        var p = CreateValid();

        p.Name.Should().Be("Tênis X");
        p.Sku.Should().Be("SKU-001");
        p.Price.Amount.Should().Be(199.90m);
        p.IsActive.Should().BeTrue();
        p.StockQuantity.Should().Be(10);
        p.AvailableQuantity.Should().Be(10);
    }

    [Fact]
    public void Create_ShouldNormalizeSku()
    {
        var p = Productss.Create("Prod", "Desc", "sku-abc", 10m, "BRL", 5, "Cat");

        p.Sku.Should().Be("SKU-ABC");
    }

    [Fact]
    public void Create_NegativeStock_ShouldThrow()
    {
        var act = () => CreateValid(-1);

        act.Should().Throw<ProductDomainException>().WithMessage("*negativo*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_EmptyName_ShouldThrow(string name)
    {
        var act = () => Productss.Create(name, "Desc", "SKU-X", 10m, "BRL", 5, "Cat");

        act.Should().Throw<ProductDomainException>();
    }

    [Fact]
    public void Create_ShouldRaiseProductCreatedEvent()
    {
        var p = CreateValid();

        p.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductCreatedDomainEvent>();
    }

    // ── ReserveStock ────────────────────────────────────────────────────────

    [Fact]
    public void ReserveStock_SufficientStock_ShouldDecreaseAvailable()
    {
        var p = CreateValid(10);
        p.ClearDomainEvents();

        p.ReserveStock(3);

        p.ReservedQuantity.Should().Be(3);
        p.AvailableQuantity.Should().Be(7);
        p.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<StockUpdatedDomainEvent>();
    }

    [Fact]
    public void ReserveStock_InsufficientStock_ShouldThrow()
    {
        var p = CreateValid(5);

        var act = () => p.ReserveStock(10);

        act.Should().Throw<InsufficientStockException>();
    }

    [Fact]
    public void ReserveStock_InactiveProduct_ShouldThrow()
    {
        var p = CreateValid();
        p.Deactivate();

        var act = () => p.ReserveStock(1);

        act.Should().Throw<ProductDomainException>().WithMessage("*inativo*");
    }

    [Fact]
    public void ReserveStock_ZeroQty_ShouldThrow()
    {
        var p = CreateValid();

        var act = () => p.ReserveStock(0);

        act.Should().Throw<ProductDomainException>().WithMessage("*positiv*");
    }

    // ── AddStock ────────────────────────────────────────────────────────────

    [Fact]
    public void AddStock_PositiveQty_ShouldIncrease()
    {
        var p = CreateValid(10);

        p.AddStock(5);

        p.StockQuantity.Should().Be(15);
    }

    [Fact]
    public void AddStock_NegativeQty_ShouldThrow()
    {
        var p = CreateValid();

        var act = () => p.AddStock(-1);

        act.Should().Throw<ProductDomainException>().WithMessage("*positiv*");
    }

    // ── ConfirmReservation ──────────────────────────────────────────────────

    [Fact]
    public void ConfirmReservation_ShouldDeductStock()
    {
        var p = CreateValid(10);
        p.ReserveStock(4);
        p.ClearDomainEvents();

        p.ConfirmReservation(4);

        p.StockQuantity.Should().Be(6);
        p.ReservedQuantity.Should().Be(0);
    }

    [Fact]
    public void ConfirmReservation_MoreThanReserved_ShouldThrow()
    {
        var p = CreateValid(10);
        p.ReserveStock(2);

        var act = () => p.ConfirmReservation(5);

        act.Should().Throw<ProductDomainException>().WithMessage("*Reserva*");
    }
}

public class MoneyTests
{
    [Theory]
    [InlineData(100, "BRL")]
    [InlineData(0, "USD")]
    [InlineData(9.999, "EUR")]
    public void Create_ValidArgs_ShouldRound2Decimals(decimal amount, string currency)
    {
        var m = Money.Create(amount, currency);

        m.Amount.Should().Be(Math.Round(amount, 2));
        m.Currency.Should().Be(currency.ToUpper());
    }

    [Fact]
    public void Create_NegativeAmount_ShouldThrow()
    {
        var act = () => Money.Create(-1m, "BRL");

        act.Should().Throw<ProductDomainException>().WithMessage("*negativ*");
    }

    [Fact]
    public void Create_InvalidCurrency_ShouldThrow()
    {
        var act = () => Money.Create(10m, "XYZ");

        act.Should().Throw<ProductDomainException>().WithMessage("*Moeda*");
    }

    [Fact]
    public void Equals_SameAmountCurrency_ShouldBeTrue()
    {
        var a = Money.Create(50m, "BRL");
        var b = Money.Create(50m, "BRL");

        a.Should().Be(b);
    }
}
