namespace BuildingBlocks.Results;

public sealed record Error(ErrorCategory Category, string Code, string Message)
{
    public static Error Validation(string code, string message) =>
        new(ErrorCategory.Validation, code, message);

    public static Error NotFound(string code, string message) =>
        new(ErrorCategory.NotFound, code, message);

    public static Error Conflict(string code, string message) =>
        new(ErrorCategory.Conflict, code, message);

    public static Error Unauthorized(string code, string message) =>
        new(ErrorCategory.Unauthorized, code, message);

    public static Error Forbidden(string code, string message) =>
        new(ErrorCategory.Forbidden, code, message);

    public static Error UnprocessableEntity(string code, string message) =>
        new(ErrorCategory.UnprocessableEntity, code, message);
}
