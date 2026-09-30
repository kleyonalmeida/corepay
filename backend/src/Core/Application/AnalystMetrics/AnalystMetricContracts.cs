namespace Core.Application.AnalystMetrics;

public sealed record AnalystMetricResponse(
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

public sealed record CreateAnalystMetricRequest(
    Guid CollaboratorId,
    Guid ProjectId,
    int Month,
    int Year,
    int FtdTotal,
    int CpaCount);

public sealed record UpdateAnalystMetricRequest(
    Guid CollaboratorId,
    Guid ProjectId,
    int Month,
    int Year,
    int FtdTotal,
    int CpaCount);

public sealed record AnalystMetricListFilters(
    int? Month,
    int? Year,
    Guid? DepartmentId,
    Guid? CollaboratorId,
    Guid? ProjectId);

public sealed record AnalystMetricAccessContext(
    IReadOnlyList<string> Roles,
    IReadOnlyList<Guid>? AllowedDepartmentIds);
