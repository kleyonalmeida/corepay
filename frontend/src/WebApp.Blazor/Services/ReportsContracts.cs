namespace WebApp.Blazor.Services;

public enum ReportsApiStatus
{
    Success,
    ValidationError,
    Forbidden,
    Error
}

public sealed record ReportsQuery(
    int? Year = null,
    Guid? DepartmentId = null,
    Guid? ProjectId = null);

public sealed record ReportsSummaryDto(
    decimal TotalYear,
    decimal MonthlyAverage,
    Guid? TopDepartmentId,
    string? TopDepartmentName,
    decimal TopDepartmentAmount,
    Guid? TopProjectId,
    string? TopProjectName,
    decimal TopProjectAmount,
    int CollaboratorCount);

public sealed record ReportsMonthlyPointDto(int Month, decimal Amount);

public sealed record ReportsDepartmentRowDto(
    Guid DepartmentId,
    string DepartmentName,
    decimal Amount,
    int EntryCount,
    int PayrollCount);

public sealed record ReportsProjectRowDto(
    Guid ProjectId,
    string ProjectName,
    decimal Amount,
    int EntryCount);

public sealed record ReportsCollaboratorRowDto(
    Guid CollaboratorId,
    string CollaboratorName,
    Guid DepartmentId,
    string DepartmentName,
    decimal Amount,
    int CompetenceCount);

public sealed record ReportsFilterOptionDto(Guid Id, string Name);

public sealed record ReportsFilterOptionsDto(
    IReadOnlyList<ReportsFilterOptionDto> Departments,
    IReadOnlyList<ReportsFilterOptionDto> Projects);

public sealed record ReportsDto(
    int Year,
    ReportsSummaryDto Summary,
    IReadOnlyList<ReportsMonthlyPointDto> MonthlySeries,
    IReadOnlyList<ReportsDepartmentRowDto> ByDepartment,
    IReadOnlyList<ReportsProjectRowDto> ByProject,
    IReadOnlyList<ReportsCollaboratorRowDto> ByCollaborator,
    ReportsFilterOptionsDto FilterOptions);

public sealed record ReportsResult(
    ReportsApiStatus Status,
    ReportsDto? Data = null,
    string? ErrorCode = null,
    string? Message = null);

public sealed record ReportsExportResult(
    ReportsApiStatus Status,
    byte[]? FileBytes = null,
    string? FileName = null,
    string? ContentType = null,
    string? ErrorCode = null,
    string? Message = null);
