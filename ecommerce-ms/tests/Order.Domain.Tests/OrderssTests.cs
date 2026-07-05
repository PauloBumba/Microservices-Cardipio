using FluentAssertions;
using Order.Domain.Entities;
using Order.Domain.Events;
using Order.Domain.Exceptions;
using Xunit;

namespace Order.Domain.Tests;

public class OrdersTests
{
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();

    private static Orders CreateOrder() =>
        Orders.Create(CustomerId);

    private static Orders CreateOrderWithItem()
    {
        var o = CreateOrder();
        o.AddItem(ProductId, "Produto X", "SKU-001", 2, 100m, "BRL");
        return o;
    }

    // ── Create ──────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ValidData_ShouldReturnPendingOrder()
    {
        var order = CreateOrder();

        order.Status.Should().Be(OrderStatus.Pending);
        order.CustomerId.Should().Be(CustomerId);
        order.OrderNumber.Should().StartWith("ORD-");
        order.Items.Should().BeEmpty();
        order.Total.Should().Be(0m);
    }

    // ── AddItem ──────────────────────────────────────────────────────────────

    [Fact]
    public void AddItem_ValidItem_ShouldUpdateTotal()
    {
        var order = CreateOrder();

        order.AddItem(ProductId, "Produto X", "SKU-001", 3, 50m, "BRL");

        order.Items.Should().HaveCount(1);
        order.Total.Should().Be(150m);
    }

    [Fact]
    public void AddItem_SameProduct_ShouldReplaceExisting()
    {
        var order = CreateOrder();
        order.AddItem(ProductId, "Prod", "SKU-001", 1, 100m, "BRL");

        order.AddItem(ProductId, "Prod", "SKU-001", 5, 100m, "BRL");

        order.Items.Should().HaveCount(2);
        order.Total.Should().Be(600m);
    }

    [Fact]
    public void AddItem_ToConfirmedOrder_ShouldThrow()
    {
        var order = CreateOrderWithItem();
        order.Confirm();

        var act = () => order.AddItem(Guid.NewGuid(), "X", "S", 1, 10m, "BRL");

        act.Should().Throw<OrderDomainException>().WithMessage("*pendente*");
    }

    // ── Confirm ──────────────────────────────────────────────────────────────

    [Fact]
    public void Confirm_PendingWithItems_ShouldSetConfirmedStatus()
    {
        var order = CreateOrderWithItem();

        order.Confirm();

        order.Status.Should().Be(OrderStatus.Confirmed);
        order.DomainEvents.Should().Contain(e => e is OrderConfirmedDomainEvent);
    }

    [Fact]
    public void Confirm_EmptyOrder_ShouldThrow()
    {
        var order = CreateOrder();

        var act = () => order.Confirm();

        act.Should().Throw<OrderDomainException>().WithMessage("*sem itens*");
    }

    [Fact]
    public void Confirm_AlreadyConfirmed_ShouldThrow()
    {
        var order = CreateOrderWithItem();
        order.Confirm();

        var act = () => order.Confirm();

        act.Should().Throw<OrderDomainException>().WithMessage("*confirmado*");
    }

    // ── Cancel ───────────────────────────────────────────────────────────────

    [Fact]
    public void Cancel_PendingOrder_ShouldSetCancelledStatus()
    {
        var order = CreateOrder();

        order.Cancel("Desistência do cliente");

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.DomainEvents.Should().Contain(e => e is OrderCancelledDomainEvent);
    }

    [Fact]
    public void Cancel_ConfirmedOrder_ShouldSucceed()
    {
        var order = CreateOrderWithItem();
        order.Confirm();
        order.ClearDomainEvents();

        order.Cancel("Motivo qualquer");

        order.Status.Should().Be(OrderStatus.Cancelled);
    }
}

public class OrderItemTests
{
    [Fact]
    public void Create_ValidItem_ShouldCalculateTotalPrice()
    {
        var item = OrderItem.Create(Guid.NewGuid(), Guid.NewGuid(),
            "Produto", "SKU-X", 3, 99.99m, "BRL");

        item.TotalPrice.Should().Be(299.97m);
        item.Quantity.Should().Be(3);
    }

    [Fact]
    public void Create_ZeroQty_ShouldThrow()
    {
        var act = () => OrderItem.Create(Guid.NewGuid(), Guid.NewGuid(),
            "P", "S", 0, 10m, "BRL");

        act.Should().Throw<OrderDomainException>().WithMessage("*positiv*");
    }

    [Fact]
    public void Create_NegativePrice_ShouldThrow()
    {
        var act = () => OrderItem.Create(Guid.NewGuid(), Guid.NewGuid(),
            "P", "S", 1, -5m, "BRL");

        act.Should().Throw<OrderDomainException>().WithMessage("*negativo*");
    }
}
