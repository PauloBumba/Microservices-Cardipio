using Product.Application.DTOs;
using Product.Application.Features.Products.Commands.AddStock;
using Product.Application.Features.Products.Commands.CreateProduct;
using Product.Application.Features.Products.Commands.DeactivateProduct;
using Product.Application.Features.Products.Queries.GetProductById;
using Product.Application.Tests.TestHelpers;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shared.Application.Behaviors;
using Shared.Application.Response;
using Xunit;

namespace Product.Application.Tests;

public class CacheTests
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly IPipelineBehavior<GetProductByIdQuery, ApiResponse<ProductDto>> _cachingBehavior;
    private readonly IPipelineBehavior<CreateProductCommand, ApiResponse<Guid>> _invalidationBehavior;
    private readonly IPipelineBehavior<AddStockCommand, ApiResponse<bool>> _addStockInvalidationBehavior;
    private readonly IPipelineBehavior<DeactivateProductCommand, ApiResponse<bool>> _deactivateInvalidationBehavior;

    public CacheTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _distributedCache = new MemoryDistributedCacheAdapter(_memoryCache);
        
        _cachingBehavior = new CachingBehavior<GetProductByIdQuery, ApiResponse<ProductDto>>(
            _distributedCache,
            NullLogger<CachingBehavior<GetProductByIdQuery, ApiResponse<ProductDto>>>.Instance);
        
        _invalidationBehavior = new CacheInvalidationBehavior<CreateProductCommand, ApiResponse<Guid>>(
            _distributedCache,
            NullLogger<CacheInvalidationBehavior<CreateProductCommand, ApiResponse<Guid>>>.Instance);
        
        _addStockInvalidationBehavior = new CacheInvalidationBehavior<AddStockCommand, ApiResponse<bool>>(
            _distributedCache,
            NullLogger<CacheInvalidationBehavior<AddStockCommand, ApiResponse<bool>>>.Instance);
        
        _deactivateInvalidationBehavior = new CacheInvalidationBehavior<DeactivateProductCommand, ApiResponse<bool>>(
            _distributedCache,
            NullLogger<CacheInvalidationBehavior<DeactivateProductCommand, ApiResponse<bool>>>.Instance);
    }

    [Fact]
    public async Task CachingBehavior_Query_ShouldCacheResponse()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var query = new GetProductByIdQuery(productId);
        var expectedProduct = new ProductDto 
        (
            productId,
            "Produto Teste",
            "Descrição do produto",
            "SKU-001",
            100m,
            "BRL",
            50,
            0,
            50,
            "Eletrônicos",
            true,
            DateTime.UtcNow,
            DateTime.UtcNow
        );
        
        var handlerMock = new Mock<IRequestHandler<GetProductByIdQuery, ApiResponse<ProductDto>>>();
        handlerMock
            .Setup(h => h.Handle(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<ProductDto>.Ok(expectedProduct));

        // Act - Primeira chamada (MISS)
        var result1 = await _cachingBehavior.Handle(query, () => handlerMock.Object.Handle(query, CancellationToken.None), CancellationToken.None);
        
        // Act - Segunda chamada (HIT)
        var result2 = await _cachingBehavior.Handle(query, () => handlerMock.Object.Handle(query, CancellationToken.None), CancellationToken.None);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result1.Data.Should().BeEquivalentTo(expectedProduct);
        result2.Data.Should().BeEquivalentTo(expectedProduct);
        
        // Handler deve ser chamado apenas uma vez (primeira chamada)
        handlerMock.Verify(h => h.Handle(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CacheInvalidationBehavior_CreateProductCommand_ShouldInvalidateCacheKeys()
    {
        // Arrange
        var command = new CreateProductCommand(
            "Novo Produto", "Descrição", "SKU-002",
            150m, "BRL", 100, "Eletrônicos");
        
        var handlerMock = new Mock<IRequestHandler<CreateProductCommand, ApiResponse<Guid>>>();
        handlerMock
            .Setup(h => h.Handle(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<Guid>.Ok(Guid.NewGuid()));

        // Pre-popular cache com a chave que será invalidada
        await _distributedCache.SetStringAsync("products:all", "cached-data");

        // Act
        var result = await _invalidationBehavior.Handle(
            command, 
            () => handlerMock.Object.Handle(command, CancellationToken.None), 
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        // Verificar que a chave foi removida do cache
        var cachedValue = await _distributedCache.GetStringAsync("products:all");
        cachedValue.Should().BeNull();
    }

    [Fact]
    public async Task CacheInvalidationBehavior_AddStockCommand_ShouldInvalidateSpecificProductCache()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var command = new AddStockCommand(productId, 10);
        
        var handlerMock = new Mock<IRequestHandler<AddStockCommand, ApiResponse<bool>>>();
        handlerMock
            .Setup(h => h.Handle(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<bool>.Ok(true));

        // Pre-popular cache com as chaves que serão invalidadas
        await _distributedCache.SetStringAsync($"products:{productId}", "cached-product-data");
        await _distributedCache.SetStringAsync("products:all", "cached-all-data");

        // Act
        var result = await _addStockInvalidationBehavior.Handle(
            command, 
            () => handlerMock.Object.Handle(command, CancellationToken.None), 
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        // Verificar que as chaves foram removidas do cache
        var cachedProduct = await _distributedCache.GetStringAsync($"products:{productId}");
        var cachedAll = await _distributedCache.GetStringAsync("products:all");
        
        cachedProduct.Should().BeNull();
        cachedAll.Should().BeNull();
    }

    [Fact]
    public async Task CacheInvalidationBehavior_DeactivateProductCommand_ShouldInvalidateSpecificProductCache()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var command = new DeactivateProductCommand(productId);
        
        var handlerMock = new Mock<IRequestHandler<DeactivateProductCommand, ApiResponse<bool>>>();
        handlerMock
            .Setup(h => h.Handle(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<bool>.Ok(true));

        // Pre-popular cache com as chaves que serão invalidadas
        await _distributedCache.SetStringAsync($"products:{productId}", "cached-product-data");
        await _distributedCache.SetStringAsync("products:all", "cached-all-data");

        // Act
        var result = await _deactivateInvalidationBehavior.Handle(
            command, 
            () => handlerMock.Object.Handle(command, CancellationToken.None), 
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        // Verificar que as chaves foram removidas do cache
        var cachedProduct = await _distributedCache.GetStringAsync($"products:{productId}");
        var cachedAll = await _distributedCache.GetStringAsync("products:all");
        
        cachedProduct.Should().BeNull();
        cachedAll.Should().BeNull();
    }

    [Fact]
    public async Task CacheInvalidationBehavior_FailedCommand_ShouldNotInvalidateCache()
    {
        // Arrange
        var command = new CreateProductCommand(
            "Produto", "Descrição", "SKU-003",
            100m, "BRL", 50, "Eletrônicos");
        
        var handlerMock = new Mock<IRequestHandler<CreateProductCommand, ApiResponse<Guid>>>();
        handlerMock
            .Setup(h => h.Handle(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<Guid>.Fail("Erro simulado"));

        // Pre-popular cache
        await _distributedCache.SetStringAsync("products:all", "cached-data");

        // Act
        var result = await _invalidationBehavior.Handle(
            command, 
            () => handlerMock.Object.Handle(command, CancellationToken.None), 
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        
        // Verificar que a chave NÃO foi removida do cache (comando falhou)
        var cachedValue = await _distributedCache.GetStringAsync("products:all");
        cachedValue.Should().NotBeNull();
    }

    [Fact]
    public async Task CachingBehavior_QueryWithDifferentKeys_ShouldCacheSeparately()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();
        var query1 = new GetProductByIdQuery(productId1);
        var query2 = new GetProductByIdQuery(productId2);
        
        var handlerMock = new Mock<IRequestHandler<GetProductByIdQuery, ApiResponse<ProductDto>>>();
        handlerMock
            .Setup(h => h.Handle(It.IsAny<GetProductByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetProductByIdQuery q, CancellationToken ct) => 
                ApiResponse<ProductDto>.Ok(new ProductDto(
                    q.Id,
                    $"Produto {q.Id}",
                    "Descrição",
                    "SKU",
                    100m,
                    "BRL",
                    50,
                    0,
                    50,
                    "Categoria",
                    true,
                    DateTime.UtcNow,
                    DateTime.UtcNow
                )));

        // Act - Duas chamadas com IDs diferentes
        var result1 = await _cachingBehavior.Handle(query1, () => handlerMock.Object.Handle(query1, CancellationToken.None), CancellationToken.None);
        var result2 = await _cachingBehavior.Handle(query2, () => handlerMock.Object.Handle(query2, CancellationToken.None), CancellationToken.None);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        
        // Handler deve ser chamado duas vezes (chaves diferentes)
        handlerMock.Verify(h => h.Handle(It.IsAny<GetProductByIdQuery>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
