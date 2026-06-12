using FluentValidation;
namespace Product.Application.Features.Products.Commands.CreateProduct;
public sealed class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3)
            .Must(c => new[]{"BRL","USD","EUR"}.Contains(c.ToUpper())).WithMessage("Moeda inválida (BRL/USD/EUR).");
        RuleFor(x => x.InitialStock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
    }
}
