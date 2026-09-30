namespace WebApp.Blazor.Services;

public enum AnalystMetricApiStatus
{
    Success,
    ValidationError,
    NotFound,
    Conflict,
    Forbidden,
    Error
}

public sealed record AnalystMetricDto(
    Guid Id,
    Guid CollaboratorId,
    string CollaboratorName,
    Guid DepartmentId,
    string DepartmentName,
    Guid ProjectId,
    string ProjectName,
    int Month,
    int Year,
    int FtdTotal,
    int CpaCount);

public sealed record AnalystMetricRequest(
    Guid CollaboratorId,
    Guid ProjectId,
    int Month,
    int Year,
    int FtdTotal,
    int CpaCount);

public sealed record AnalystMetricListQuery(
    int? Month = null,
    int? Year = null,
    Guid? DepartmentId = null,
    Guid? CollaboratorId = null,
    Guid? ProjectId = null);

public sealed record AnalystMetricListResult(
    AnalystMetricApiStatus Status,
    IReadOnlyList<AnalystMetricDto>? Metrics = null,
    string? ErrorCode = null,
    string? Message = null);

public sealed record AnalystMetricMutationResult(
    AnalystMetricApiStatus Status,
    AnalystMetricDto? Metric = null,
    string? ErrorCode = null,
    string? Message = null);
