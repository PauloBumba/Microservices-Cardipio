namespace Shared.Application.Response;

/// <summary>
/// Envelope de resposta padronizado — nunca lance exceção para erros de negócio,
/// use ApiResponse.Fail() em vez disso.
/// </summary>
public sealed class ApiResponse<T>
{
    public bool IsSuccess { get; private init; }
    public T? Data { get; private init; }
    public IReadOnlyList<string> Errors { get; private init; } = [];

    private ApiResponse() { }

    public static ApiResponse<T> Ok(T data) =>
        new() { IsSuccess = true, Data = data };

    public static ApiResponse<T> Fail(params string[] errors) =>
        new() { IsSuccess = false, Errors = errors };

    public static ApiResponse<T> Fail(IEnumerable<string> errors) =>
        new() { IsSuccess = false, Errors = errors.ToList() };
}

public static class ApiResponse
{
    public static ApiResponse<bool> Ok() => ApiResponse<bool>.Ok(true);
    public static ApiResponse<bool> Fail(params string[] errors) => ApiResponse<bool>.Fail(errors);
}
