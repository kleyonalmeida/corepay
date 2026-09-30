namespace Core.Application.Revenues;

public sealed record ProjectRevenueResponse(
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

public sealed record CreateProjectRevenueRequest(
    Guid ProjectId,
    int Month,
    int Year,
    decimal ValueIgaming,
    decimal ValueVendas,
    decimal GroupPercentage,
    string? Notes);

public sealed record UpdateProjectRevenueRequest(
    Guid ProjectId,
    int Month,
    int Year,
    decimal ValueIgaming,
    decimal ValueVendas,
    decimal GroupPercentage,
    string? Notes);

public sealed record ProjectRevenueListFilters(
    int? Month,
    int? Year,
    Guid? ProjectId);
