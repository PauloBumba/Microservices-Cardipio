using FluentValidation;
namespace Customer.Application.Features.Customers.Commands.UpdateCustomer;
public sealed class UpdateCustomerValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).NotEmpty().Matches(@"^\+?[\d\s\-\(\)]{8,20}$").WithMessage("Telefone inválido.");
        RuleFor(x => x.Street).NotEmpty(); RuleFor(x => x.City).NotEmpty();
        RuleFor(x => x.State).NotEmpty(); RuleFor(x => x.ZipCode).NotEmpty(); RuleFor(x => x.Country).NotEmpty();
    }
}
