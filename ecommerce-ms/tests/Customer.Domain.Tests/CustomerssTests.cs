using Customer.Domain.Entities;
using Customer.Domain.Exceptions;
using Customer.Domain.Events;
using FluentAssertions;
using Xunit;

namespace Customer.Domain.Tests;

public class CustomerssTests
{
    private static Customerss CreateValid() =>
        Customerss.Create("João Silva", "joao@email.com", "+5549999999999",
            "Rua A", "Videira", "SC", "89560-000", "Brasil");

    // ── Create ──────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ValidData_ShouldReturnActiveCustomer()
    {
        var customer = CreateValid();

        customer.Name.Should().Be("João Silva");
        customer.Email.Value.Should().Be("joao@email.com");
        customer.IsActive.Should().BeTrue();
        customer.Id.Should().NotBeEmpty();
        customer.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_ShouldRaiseCustomerCreatedDomainEvent()
    {
        var customer = CreateValid();

        customer.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CustomerCreatedDomainEvent>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyName_ShouldThrowDomainException(string name)
    {
        var act = () => Customerss.Create(name, "a@b.com", "+5549999999999",
            "Rua", "Cidade", "SC", "00000-000", "Brasil");

        act.Should().Throw<CustomerDomainException>().WithMessage("*Nome*");
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("sem-arroba.com")]
    [InlineData("")]
    public void Create_InvalidEmail_ShouldThrowDomainException(string email)
    {
        var act = () => Customerss.Create("Nome", email, "+5549999999999",
            "Rua", "Cidade", "SC", "00000-000", "Brasil");

        act.Should().Throw<CustomerDomainException>();
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("12")]
    [InlineData("")]
    public void Create_InvalidPhone_ShouldThrowDomainException(string phone)
    {
        var act = () => Customerss.Create("Nome", "a@b.com", phone,
            "Rua", "Cidade", "SC", "00000-000", "Brasil");

        act.Should().Throw<CustomerDomainException>();
    }

    // ── Deactivate ───────────────────────────────────────────────────────────

    [Fact]
    public void Deactivate_ActiveCustomer_ShouldSetIsActiveFalse()
    {
        var customer = CreateValid();
        customer.ClearDomainEvents();

        customer.Deactivate();

        customer.IsActive.Should().BeFalse();
        customer.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CustomerDeactivatedDomainEvent>();
    }

    [Fact]
    public void Deactivate_AlreadyInactive_ShouldThrowDomainException()
    {
        var customer = CreateValid();
        customer.Deactivate();

        var act = () => customer.Deactivate();

        act.Should().Throw<CustomerDomainException>().WithMessage("*inativo*");
    }

    // ── Update ───────────────────────────────────────────────────────────────

    [Fact]
    public void Update_ActiveCustomer_ShouldChangeName()
    {
        var customer = CreateValid();

        customer.Update("Novo Nome", "+5549988888888",
            "Nova Rua", "Nova Cidade", "PR", "12345-678", "Brasil");

        customer.Name.Should().Be("Novo Nome");
        customer.Address.City.Should().Be("Nova Cidade");
    }

    [Fact]
    public void Update_InactiveCustomer_ShouldThrowDomainException()
    {
        var customer = CreateValid();
        customer.Deactivate();

        var act = () => customer.Update("Nome", "+5549999999999",
            "Rua", "Cidade", "SC", "00000-000", "Brasil");

        act.Should().Throw<CustomerDomainException>().WithMessage("*inativo*");
    }
}
