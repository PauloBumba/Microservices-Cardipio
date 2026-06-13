using Customer.Domain.Exceptions;
using Customer.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Customer.Domain.Tests;

public class EmailTests
{
    [Theory]
    [InlineData("user@example.com")]
    [InlineData("USER@EXAMPLE.COM")]
    [InlineData("a.b+tag@sub.domain.org")]
    public void Create_ValidEmail_ShouldReturnLowercasedValue(string raw)
    {
        var email = Email.Create(raw);

        email.Value.Should().Be(raw.Trim().ToLowerInvariant());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("semdominio@")]
    [InlineData("@semlocal.com")]
    [InlineData("invalido")]
    public void Create_InvalidEmail_ShouldThrow(string raw)
    {
        var act = () => Email.Create(raw);

        act.Should().Throw<CustomerDomainException>();
    }

    [Fact]
    public void Equals_SameValue_ShouldBeTrue()
    {
        var a = Email.Create("a@b.com");
        var b = Email.Create("A@B.COM");

        a.Should().Be(b);
    }
}
