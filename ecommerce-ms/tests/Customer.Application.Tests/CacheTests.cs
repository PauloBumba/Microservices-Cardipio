using Customer.Application.DTOs;
using Customer.Application.Features.Customers.Commands.CreateCustomer;
using Customer.Application.Features.Customers.Commands.UpdateCustomer;
using Customer.Application.Features.Customers.Queries.GetCustomerById;
using Customer.Application.Repositories;
using Customer.Application.Tests.TestHelpers;
using Customer.Domain.Entities;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shared.Application.Behaviors;
using Shared.Application.Response;
using Xunit;

namespace Customer.Application.Tests;

public class CacheTests
{
    private readonly Mock<ICustomerRepository> _repo = new();
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly IPipelineBehavior<GetCustomerByIdQuery, ApiResponse<CustomerDto>> _cachingBehavior;
    private readonly IPipelineBehavior<CreateCustomerCommand, ApiResponse<Guid>> _invalidationBehavior;
    private readonly IPipelineBehavior<UpdateCustomerCommand, ApiResponse<bool>> _updateInvalidationBehavior;

    public CacheTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _distributedCache = new MemoryDistributedCacheAdapter(_memoryCache);
        
        _cachingBehavior = new CachingBehavior<GetCustomerByIdQuery, ApiResponse<CustomerDto>>(
            _distributedCache,
            NullLogger<CachingBehavior<GetCustomerByIdQuery, ApiResponse<CustomerDto>>>.Instance);
        
        _invalidationBehavior = new CacheInvalidationBehavior<CreateCustomerCommand, ApiResponse<Guid>>(
            _distributedCache,
            NullLogger<CacheInvalidationBehavior<CreateCustomerCommand, ApiResponse<Guid>>>.Instance);
        
        _updateInvalidationBehavior = new CacheInvalidationBehavior<UpdateCustomerCommand, ApiResponse<bool>>(
            _distributedCache,
            NullLogger<CacheInvalidationBehavior<UpdateCustomerCommand, ApiResponse<bool>>>.Instance);
    }

    [Fact]
    public async Task CachingBehavior_Query_ShouldCacheResponse()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var query = new GetCustomerByIdQuery(customerId);
        var expectedCustomer = new CustomerDto 
        (
            customerId,
            "Test Customer",
            "test@email.com",
            "+5549999999999",
            new AddressDto("Rua A", "Videira", "SC", "89560-000", "Brasil"),
            true,
            DateTime.UtcNow,
            DateTime.UtcNow
        );
        
        var handlerMock = new Mock<IRequestHandler<GetCustomerByIdQuery, ApiResponse<CustomerDto>>>();
        handlerMock
            .Setup(h => h.Handle(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<CustomerDto>.Ok(expectedCustomer));

        // Act - Primeira chamada (MISS)
        var result1 = await _cachingBehavior.Handle(query, () => handlerMock.Object.Handle(query, CancellationToken.None), CancellationToken.None);
        
        // Act - Segunda chamada (HIT)
        var result2 = await _cachingBehavior.Handle(query, () => handlerMock.Object.Handle(query, CancellationToken.None), CancellationToken.None);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result1.Data.Should().BeEquivalentTo(expectedCustomer);
        result2.Data.Should().BeEquivalentTo(expectedCustomer);
        
        // Handler deve ser chamado apenas uma vez (primeira chamada)
        handlerMock.Verify(h => h.Handle(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CacheInvalidationBehavior_CreateCommand_ShouldInvalidateCacheKeys()
    {
        // Arrange
        var command = new CreateCustomerCommand(
            "Test Customer", "test@email.com", "+5549999999999",
            "Rua A", "Videira", "SC", "89560-000", "Brasil");
        
        var handlerMock = new Mock<IRequestHandler<CreateCustomerCommand, ApiResponse<Guid>>>();
        handlerMock
            .Setup(h => h.Handle(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<Guid>.Ok(Guid.NewGuid()));

        // Pre-popular cache com a chave que será invalidada
        await _distributedCache.SetStringAsync("customers:all", "cached-data");

        // Act
        var result = await _invalidationBehavior.Handle(
            command, 
            () => handlerMock.Object.Handle(command, CancellationToken.None), 
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        // Verificar que a chave foi removida do cache
        var cachedValue = await _distributedCache.GetStringAsync("customers:all");
        cachedValue.Should().BeNull();
    }

    [Fact]
    public async Task CacheInvalidationBehavior_UpdateCommand_ShouldInvalidateSpecificCustomerCache()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var command = new UpdateCustomerCommand(
            customerId, "Updated Name", "+5549999999999",
            "Rua B", "Videira", "SC", "89560-000", "Brasil");
        
        var handlerMock = new Mock<IRequestHandler<UpdateCustomerCommand, ApiResponse<bool>>>();
        handlerMock
            .Setup(h => h.Handle(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<bool>.Ok(true));

        // Pre-popular cache com a chave específica do cliente
        await _distributedCache.SetStringAsync($"customers:{customerId}", "cached-customer-data");

        // Act
        var result = await _updateInvalidationBehavior.Handle(
            command, 
            () => handlerMock.Object.Handle(command, CancellationToken.None), 
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        // Verificar que a chave específica foi removida do cache
        var cachedValue = await _distributedCache.GetStringAsync($"customers:{customerId}");
        cachedValue.Should().BeNull();
    }

    [Fact]
    public async Task CacheInvalidationBehavior_FailedCommand_ShouldNotInvalidateCache()
    {
        // Arrange
        var command = new CreateCustomerCommand(
            "Test Customer", "test@email.com", "+5549999999999",
            "Rua A", "Videira", "SC", "89560-000", "Brasil");
        
        var handlerMock = new Mock<IRequestHandler<CreateCustomerCommand, ApiResponse<Guid>>>();
        handlerMock
            .Setup(h => h.Handle(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<Guid>.Fail("Erro simulado"));

        // Pre-popular cache
        await _distributedCache.SetStringAsync("customers:all", "cached-data");

        // Act
        var result = await _invalidationBehavior.Handle(
            command, 
            () => handlerMock.Object.Handle(command, CancellationToken.None), 
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        
        // Verificar que a chave NÃO foi removida do cache (comando falhou)
        var cachedValue = await _distributedCache.GetStringAsync("customers:all");
        cachedValue.Should().NotBeNull();
    }

    [Fact]
    public async Task CachingBehavior_QueryWithDifferentKeys_ShouldCacheSeparately()
    {
        // Arrange
        var customerId1 = Guid.NewGuid();
        var customerId2 = Guid.NewGuid();
        var query1 = new GetCustomerByIdQuery(customerId1);
        var query2 = new GetCustomerByIdQuery(customerId2);
        
        var handlerMock = new Mock<IRequestHandler<GetCustomerByIdQuery, ApiResponse<CustomerDto>>>();
        handlerMock
            .Setup(h => h.Handle(It.IsAny<GetCustomerByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetCustomerByIdQuery q, CancellationToken ct) => 
                ApiResponse<CustomerDto>.Ok(new CustomerDto(
                    q.Id,
                    $"Customer {q.Id}",
                    "test@email.com",
                    "+5549999999999",
                    new AddressDto("Rua A", "Videira", "SC", "89560-000", "Brasil"),
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
        handlerMock.Verify(h => h.Handle(It.IsAny<GetCustomerByIdQuery>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
