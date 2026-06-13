using Customer.Application.Features.Customers.Commands.CreateCustomer;
using FluentAssertions;
using Xunit;

namespace Customer.Application.Tests;

public class CreateCustomerValidatorTests
{
    private readonly CreateCustomerValidator _validator = new();

    private static CreateCustomerCommand Valid() => new(
        "João Silva", "joao@email.com", "+5549999999999",
        "Rua A", "Videira", "SC", "89560-000", "Brasil");

    [Fact]
    public async Task Validate_ValidCommand_ShouldHaveNoErrors()
    {
        var result = await _validator.ValidateAsync(Valid());

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_EmptyName_ShouldFail(string name)
    {
        var cmd = Valid() with { Name = name };

        var result = await _validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Theory]
    [InlineData("invalido")]
    [InlineData("sem-ponto@dominio")]
    [InlineData("")]
    public async Task Validate_InvalidEmail_ShouldFail(string email)
    {
        var cmd = Valid() with { Email = email };

        var result = await _validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("12")]
    [InlineData("")]
    public async Task Validate_InvalidPhone_ShouldFail(string phone)
    {
        var cmd = Valid() with { Phone = phone };

        var result = await _validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Phone");
    }

    [Fact]
    public async Task Validate_EmptyStreet_ShouldFail()
    {
        var cmd = Valid() with { Street = "" };

        var result = await _validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
    }
}
