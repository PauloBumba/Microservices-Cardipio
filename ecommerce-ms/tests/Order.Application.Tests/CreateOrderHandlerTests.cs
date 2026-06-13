using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Order.Application.Contracts;
using Order.Application.Features.Orders.Commands.CreateOrder;
using Order.Domain.Entities;
using Order.Domain.Exceptions;
using Order.Domain.Repositories;
using Xunit;

namespace Order.Application.Tests;

public class CreateOrderHandlerTests
{
    private readonly Mock<IOrderRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IProductServiceClient> _productClient = new();

    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();

    private CreateOrderHandler BuildHandler() =>
        new(_repo.Object, _uow.Object, _productClient.Object,
            NullLogger<CreateOrderHandler>.Instance);

    private static CreateOrderCommand BuildCommand(IReadOnlyList<OrderLineDto>? items = null) =>
        new(CustomerId, "Rua A", "Videira", "SC", "89560-000", "Brasil", null,
            items ?? [new OrderLineDto(ProductId, 2)]);

    private void SetupProduct(bool isActive = true, int available = 10) =>
        _productClient.Setup(c => c.GetProductAsync(ProductId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new ProductInfo(ProductId, "Produto X", "SKU-001",
                          100m, "BRL", available, isActive));

    [Fact]
    public async Task Handle_ValidOrder_ShouldCreateAndCommit()
    {
        SetupProduct();
        _productClient.Setup(c => c.ReserveStockAsync(ProductId, 2, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(true);

        var handler = BuildHandler();
        var result = await handler.Handle(BuildCommand(), CancellationToken.None);

        result.Should().NotBeNull();
        result.CustomerId.Should().Be(CustomerId);
        result.Items.Should().HaveCount(1);
        result.TotalAmount.Should().Be(200m);

        _repo.Verify(r => r.AddAsync(It.IsAny<Orderss>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ShouldThrow()
    {
        _productClient.Setup(c => c.GetProductAsync(ProductId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((ProductInfo?)null);

        var handler = BuildHandler();
        var act = async () => await handler.Handle(BuildCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<OrderDomainException>().WithMessage("*não encontrado*");
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InactiveProduct_ShouldThrow()
    {
        SetupProduct(isActive: false);

        var handler = BuildHandler();
        var act = async () => await handler.Handle(BuildCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<OrderDomainException>().WithMessage("*inativo*");
    }

    [Fact]
    public async Task Handle_ReservationFailed_ShouldThrow()
    {
        SetupProduct();
        _productClient.Setup(c => c.ReserveStockAsync(ProductId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(false);

        var handler = BuildHandler();
        var act = async () => await handler.Handle(BuildCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<OrderDomainException>().WithMessage("*Estoque*");
    }
}
