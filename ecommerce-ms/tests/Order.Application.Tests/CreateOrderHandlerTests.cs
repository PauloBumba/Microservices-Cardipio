using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Order.Application.Features.Orders.Commands.CreateOrder;
using Order.Application.Repositories;
using Order.Domain.Entities;
using Xunit;

namespace Order.Application.Tests;

public class CreateOrderHandlerTests
{
    private readonly Mock<IOrderRepository> _repo = new();

    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();

    private CreateOrderHandler BuildHandler() =>
        new(_repo.Object, NullLogger<CreateOrderHandler>.Instance);

    private static CreateOrderCommand BuildCommand(List<CreateOrderItemDto>? items = null) =>
        new(CustomerId, items ?? [new CreateOrderItemDto(ProductId, "Produto X", "SKU-001", 2, 100m, "BRL")]);

    [Fact]
    public async Task Handle_ValidOrder_ShouldCreateAndCommit()
    {
        var handler = BuildHandler();
        var result = await handler.Handle(BuildCommand(), CancellationToken.None);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeEmpty();

        _repo.Verify(r => r.AddAsync(
            It.Is<Orders>(o =>
                o.CustomerId == CustomerId &&
                o.Items.Count == 1 &&
                o.Total == 200m),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyItems_ShouldReturnFailure()
    {
        var handler = BuildHandler();
        var result = await handler.Handle(BuildCommand([]), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("O pedido deve ter ao menos um item.");
        _repo.Verify(r => r.AddAsync(It.IsAny<Orders>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
