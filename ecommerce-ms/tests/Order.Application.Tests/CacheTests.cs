using Order.Application.DTOs;
using Order.Application.Features.Orders.Commands.CancelOrder;
using Order.Application.Features.Orders.Commands.CreateOrder;
using Order.Application.Features.Orders.Queries.GetOrderById;
using Order.Application.Tests.TestHelpers;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shared.Application.Behaviors;
using Shared.Application.Response;
using Xunit;

namespace Order.Application.Tests;

public class CacheTests
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly IPipelineBehavior<GetOrderByIdQuery, ApiResponse<OrderDto>> _cachingBehavior;
    private readonly IPipelineBehavior<CreateOrderCommand, ApiResponse<Guid>> _invalidationBehavior;
    private readonly IPipelineBehavior<CancelOrderCommand, ApiResponse<bool>> _cancelInvalidationBehavior;

    public CacheTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _distributedCache = new MemoryDistributedCacheAdapter(_memoryCache);
        
        _cachingBehavior = new CachingBehavior<GetOrderByIdQuery, ApiResponse<OrderDto>>(
            _distributedCache,
            NullLogger<CachingBehavior<GetOrderByIdQuery, ApiResponse<OrderDto>>>.Instance);
        
        _invalidationBehavior = new CacheInvalidationBehavior<CreateOrderCommand, ApiResponse<Guid>>(
            _distributedCache,
            NullLogger<CacheInvalidationBehavior<CreateOrderCommand, ApiResponse<Guid>>>.Instance);
        
        _cancelInvalidationBehavior = new CacheInvalidationBehavior<CancelOrderCommand, ApiResponse<bool>>(
            _distributedCache,
            NullLogger<CacheInvalidationBehavior<CancelOrderCommand, ApiResponse<bool>>>.Instance);
    }

    [Fact]
    public async Task CachingBehavior_Query_ShouldCacheResponse()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var query = new GetOrderByIdQuery(orderId);
        var expectedOrder = new OrderDto 
        (
            orderId,
            "ORD-001",
            Guid.NewGuid(),
            "Pending",
            200m,
            "BRL",
            [new OrderItemDto(Guid.NewGuid(), "Produto X", "SKU-001", 2, 100m, 200m)],
            DateTime.UtcNow
        );
        
        var handlerMock = new Mock<IRequestHandler<GetOrderByIdQuery, ApiResponse<OrderDto>>>();
        handlerMock
            .Setup(h => h.Handle(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<OrderDto>.Ok(expectedOrder));

        // Act - Primeira chamada (MISS)
        var result1 = await _cachingBehavior.Handle(query, () => handlerMock.Object.Handle(query, CancellationToken.None), CancellationToken.None);
        
        // Act - Segunda chamada (HIT)
        var result2 = await _cachingBehavior.Handle(query, () => handlerMock.Object.Handle(query, CancellationToken.None), CancellationToken.None);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result1.Data.Should().BeEquivalentTo(expectedOrder);
        result2.Data.Should().BeEquivalentTo(expectedOrder);
        
        // Handler deve ser chamado apenas uma vez (primeira chamada)
        handlerMock.Verify(h => h.Handle(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CacheInvalidationBehavior_CreateOrderCommand_ShouldInvalidateCacheKeys()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var command = new CreateOrderCommand(
            customerId,
            [new CreateOrderItemDto(Guid.NewGuid(), "Produto X", "SKU-001", 2, 100m, "BRL")]);
        
        var handlerMock = new Mock<IRequestHandler<CreateOrderCommand, ApiResponse<Guid>>>();
        handlerMock
            .Setup(h => h.Handle(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<Guid>.Ok(Guid.NewGuid()));

        // Pre-popular cache com as chaves que serão invalidadas
        await _distributedCache.SetStringAsync($"orders:customer:{customerId}", "cached-data");
        await _distributedCache.SetStringAsync("orders:all", "cached-all-data");

        // Act
        var result = await _invalidationBehavior.Handle(
            command, 
            () => handlerMock.Object.Handle(command, CancellationToken.None), 
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        // Verificar que as chaves foram removidas do cache
        var cachedCustomerOrders = await _distributedCache.GetStringAsync($"orders:customer:{customerId}");
        var cachedAllOrders = await _distributedCache.GetStringAsync("orders:all");
        
        cachedCustomerOrders.Should().BeNull();
        cachedAllOrders.Should().BeNull();
    }

    [Fact]
    public async Task CacheInvalidationBehavior_CancelOrderCommand_ShouldInvalidateSpecificOrderCache()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var command = new CancelOrderCommand(orderId, "Cliente cancelou");
        
        var handlerMock = new Mock<IRequestHandler<CancelOrderCommand, ApiResponse<bool>>>();
        handlerMock
            .Setup(h => h.Handle(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<bool>.Ok(true));

        // Pre-popular cache com a chave específica do pedido
        await _distributedCache.SetStringAsync($"orders:{orderId}", "cached-order-data");
        await _distributedCache.SetStringAsync("orders:all", "cached-all-data");

        // Act
        var result = await _cancelInvalidationBehavior.Handle(
            command, 
            () => handlerMock.Object.Handle(command, CancellationToken.None), 
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        // Verificar que as chaves foram removidas do cache
        var cachedOrder = await _distributedCache.GetStringAsync($"orders:{orderId}");
        var cachedAll = await _distributedCache.GetStringAsync("orders:all");
        
        cachedOrder.Should().BeNull();
        cachedAll.Should().BeNull();
    }

    [Fact]
    public async Task CacheInvalidationBehavior_FailedCommand_ShouldNotInvalidateCache()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var command = new CreateOrderCommand(
            customerId,
            [new CreateOrderItemDto(Guid.NewGuid(), "Produto X", "SKU-001", 2, 100m, "BRL")]);
        
        var handlerMock = new Mock<IRequestHandler<CreateOrderCommand, ApiResponse<Guid>>>();
        handlerMock
            .Setup(h => h.Handle(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<Guid>.Fail("Erro simulado"));

        // Pre-popular cache
        await _distributedCache.SetStringAsync($"orders:customer:{customerId}", "cached-data");

        // Act
        var result = await _invalidationBehavior.Handle(
            command, 
            () => handlerMock.Object.Handle(command, CancellationToken.None), 
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        
        // Verificar que a chave NÃO foi removida do cache (comando falhou)
        var cachedValue = await _distributedCache.GetStringAsync($"orders:customer:{customerId}");
        cachedValue.Should().NotBeNull();
    }

    [Fact]
    public async Task CachingBehavior_QueryWithDifferentKeys_ShouldCacheSeparately()
    {
        // Arrange
        var orderId1 = Guid.NewGuid();
        var orderId2 = Guid.NewGuid();
        var query1 = new GetOrderByIdQuery(orderId1);
        var query2 = new GetOrderByIdQuery(orderId2);
        
        var handlerMock = new Mock<IRequestHandler<GetOrderByIdQuery, ApiResponse<OrderDto>>>();
        handlerMock
            .Setup(h => h.Handle(It.IsAny<GetOrderByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetOrderByIdQuery q, CancellationToken ct) => 
                ApiResponse<OrderDto>.Ok(new OrderDto(
                    q.Id,
                    "ORD-002",
                    Guid.NewGuid(),
                    "Pending",
                    100m,
                    "BRL",
                    [new OrderItemDto(Guid.NewGuid(), "Produto", "SKU", 1, 100m, 100m)],
                    DateTime.UtcNow
                )));

        // Act - Duas chamadas com IDs diferentes
        var result1 = await _cachingBehavior.Handle(query1, () => handlerMock.Object.Handle(query1, CancellationToken.None), CancellationToken.None);
        var result2 = await _cachingBehavior.Handle(query2, () => handlerMock.Object.Handle(query2, CancellationToken.None), CancellationToken.None);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        
        // Handler deve ser chamado duas vezes (chaves diferentes)
        handlerMock.Verify(h => h.Handle(It.IsAny<GetOrderByIdQuery>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
