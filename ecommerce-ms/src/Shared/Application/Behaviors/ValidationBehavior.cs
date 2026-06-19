using FluentValidation;
using MediatR;
using Shared.Application.Response;

namespace Shared.Application.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .Select(f => f.ErrorMessage)
            .ToList();

        if (failures.Count == 0) return await next();

        // Tenta encaixar no ApiResponse<T> sem lançar exceção
        var responseType = typeof(TResponse);
        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(ApiResponse<>))
        {
            var innerType = responseType.GetGenericArguments()[0];
            var failMethod = typeof(ApiResponse<>).MakeGenericType(innerType)
                .GetMethod(nameof(ApiResponse<object>.Fail), [typeof(IEnumerable<string>)])!;
            return (TResponse)failMethod.Invoke(null, [failures])!;
        }

        throw new ValidationException(string.Join("; ", failures));
    }
}
