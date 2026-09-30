namespace WebApp.Blazor.Services;

public enum PaymentMethodApiStatus
{
    Success,
    ValidationError,
    NotFound,
    Conflict,
    Forbidden,
    Error
}

public sealed record PaymentMethodDto(Guid Id, string Name, bool IsActive);

public sealed record PaymentMethodRequest(string Name, bool IsActive);

public sealed record PaymentMethodListResult(
    PaymentMethodApiStatus Status,
    IReadOnlyList<PaymentMethodDto>? PaymentMethods = null,
    string? ErrorCode = null,
    string? Message = null);

public sealed record PaymentMethodMutationResult(
    PaymentMethodApiStatus Status,
    PaymentMethodDto? PaymentMethod = null,
    string? ErrorCode = null,
    string? Message = null);

public sealed record PaymentMethodDeleteResult(
    PaymentMethodApiStatus Status,
    string? ErrorCode = null,
    string? Message = null);
