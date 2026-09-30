namespace Core.Application.Reports;

public sealed record PayrollReportFilters(
    int? Year,
    Guid? DepartmentId,
    Guid? ProjectId);

public sealed record PayrollReportSummaryResponse(
    decimal TotalYear,
    decimal MonthlyAverage,
    Guid? TopDepartmentId,
    string? TopDepartmentName,
    decimal TopDepartmentAmount,
    Guid? TopProjectId,
    string? TopProjectName,
    decimal TopProjectAmount,
    int CollaboratorCount);

public sealed record PayrollReportMonthlyPointResponse(int Month, decimal Amount);

public sealed record PayrollReportDepartmentRowResponse(
    Guid DepartmentId,
    string DepartmentName,
    decimal Amount,
    int EntryCount,
    int PayrollCount);

public sealed record PayrollReportProjectRowResponse(
    Guid ProjectId,
    string ProjectName,
    decimal Amount,
    int EntryCount);

public sealed record PayrollReportCollaboratorRowResponse(
    Guid CollaboratorId,
    string CollaboratorName,
    Guid DepartmentId,
    string DepartmentName,
    decimal Amount,
    int CompetenceCount);

public sealed record PayrollReportFilterOption(Guid Id, string Name);

public sealed record PayrollReportFilterOptionsResponse(
    IReadOnlyList<PayrollReportFilterOption> Departments,
    IReadOnlyList<PayrollReportFilterOption> Projects);

public sealed record PayrollReportResponse(
    int Year,
    PayrollReportSummaryResponse Summary,
    IReadOnlyList<PayrollReportMonthlyPointResponse> MonthlySeries,
    IReadOnlyList<PayrollReportDepartmentRowResponse> ByDepartment,
    IReadOnlyList<PayrollReportProjectRowResponse> ByProject,
    IReadOnlyList<PayrollReportCollaboratorRowResponse> ByCollaborator,
    PayrollReportFilterOptionsResponse FilterOptions);
