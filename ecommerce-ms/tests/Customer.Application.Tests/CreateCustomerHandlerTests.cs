using Customer.Application.Features.Customers.Commands.CreateCustomer;
using Customer.Domain.Entities;
using Customer.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Customer.Application.Tests;

public class CreateCustomerHandlerTests
{
    private readonly Mock<ICustomerRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private CreateCustomerHandler BuildHandler() =>
        new(_repo.Object, _uow.Object,
            NullLogger<CreateCustomerHandler>.Instance);

    private static CreateCustomerCommand ValidCommand() => new(
        "João Silva", "joao@email.com", "+5549999999999",
        "Rua A", "Videira", "SC", "89560-000", "Brasil");

    [Fact]
    public async Task Handle_NewEmail_ShouldCreateCustomerAndCommit()
    {
        _repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(false);

        var handler = BuildHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.Should().NotBeNull();
        result.Name.Should().Be("João Silva");
        result.Email.Should().Be("joao@email.com");

        _repo.Verify(r => r.AddAsync(It.IsAny<Customerss>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ShouldThrowInvalidOperation()
    {
        _repo.Setup(r => r.EmailExistsAsync("joao@email.com", It.IsAny<CancellationToken>()))
             .ReturnsAsync(true);

        var handler = BuildHandler();
        var act = async () => await handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*já cadastrado*");

        _repo.Verify(r => r.AddAsync(It.IsAny<Customerss>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
