using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
namespace Product.Application.Behaviors;
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators,
    ILogger<ValidationBehavior<TRequest,TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest req, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!validators.Any()) return await next();
        var ctx = new ValidationContext<TRequest>(req);
        var f = validators.Select(v => v.Validate(ctx)).SelectMany(r => r.Errors).Where(x => x is not null).ToList();
        if (f.Count > 0) { logger.LogWarning("Validation: {E}", f); throw new ValidationException(f); }
        return await next();
    }
}
