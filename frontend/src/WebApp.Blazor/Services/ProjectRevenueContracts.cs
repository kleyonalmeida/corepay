namespace WebApp.Blazor.Services;

public enum ProjectRevenueApiStatus
{
    Success,
    ValidationError,
    NotFound,
    Conflict,
    Forbidden,
    Error
}

public sealed record ProjectRevenueDto(
    Guid Id,
    Guid ProjectId,
    string ProjectName,
    int Month,
    int Year,
    decimal ValueIgaming,
    decimal ValueVendas,
    decimal Value,
    decimal GroupPercentage,
    string? Notes);

public sealed record ProjectRevenueRequest(
    Guid ProjectId,
    int Month,
    int Year,
    decimal ValueIgaming,
    decimal ValueVendas,
    decimal GroupPercentage,
    string? Notes);

public sealed record ProjectRevenueListQuery(
    int? Month = null,
    int? Year = null,
    Guid? ProjectId = null);

public sealed record ProjectRevenueListResult(
    ProjectRevenueApiStatus Status,
    IReadOnlyList<ProjectRevenueDto>? Revenues = null,
    string? ErrorCode = null,
    string? Message = null);

public sealed record ProjectRevenueDetailResult(
    ProjectRevenueApiStatus Status,
    ProjectRevenueDto? Revenue = null,
    string? ErrorCode = null,
    string? Message = null);

public sealed record ProjectRevenueMutationResult(
    ProjectRevenueApiStatus Status,
    ProjectRevenueDto? Revenue = null,
    string? ErrorCode = null,
    string? Message = null);
