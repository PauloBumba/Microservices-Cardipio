using FluentValidation;

namespace Customer.Application.Features.Customers.Commands.CreateCustomer;

public sealed class CreateCustomerValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress(FluentValidation.Validators.EmailValidationMode.Net4xRegex)
            .WithMessage("Email inválido.");

        RuleFor(x => x.Phone)
            .NotEmpty()
            .Matches(@"^\+?[0-9]{10,15}$")
            .WithMessage("Telefone inválido.");

        RuleFor(x => x.Street).NotEmpty();
        RuleFor(x => x.City).NotEmpty();
        RuleFor(x => x.State).NotEmpty();
        RuleFor(x => x.ZipCode).NotEmpty();
        RuleFor(x => x.Country).NotEmpty();
    }
}