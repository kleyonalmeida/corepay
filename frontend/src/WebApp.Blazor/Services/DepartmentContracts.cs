using System.Text.Json.Serialization;

namespace WebApp.Blazor.Services;

public enum DepartmentApiStatus
{
    Success,
    ValidationError,
    NotFound,
    Conflict,
    Forbidden,
    Error
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CalculationProfile
{
    CommercialAnalyst,
    CommercialSupervisor,
    PaidTraffic,
    Management,
    ProjectLeader,
    CommissionOnly,
    FixedCommission,
    FixedCommissionBonus,
    FixedBonus,
    Tipster,
    AllocatedFixed
}

public sealed record DepartmentDto(
    Guid Id,
    string Name,
    CalculationProfile CalculationType,
    decimal GoalBonusPercentage,
    decimal LowRevenueThreshold,
    decimal LowRevenueBonusPct,
    string? Description,
    bool IsActive,
    bool IsAllocatedFixed,
    bool RoutesFixedToLimaKarttos);

public sealed record DepartmentRequest(
    string Name,
    CalculationProfile CalculationType,
    decimal GoalBonusPercentage,
    decimal LowRevenueThreshold,
    decimal LowRevenueBonusPct,
    string? Description,
    bool IsActive,
    bool IsAllocatedFixed,
    bool RoutesFixedToLimaKarttos);

public sealed record DepartmentListResult(
    DepartmentApiStatus Status,
    IReadOnlyList<DepartmentDto>? Departments = null,
    string? ErrorCode = null,
    string? Message = null);

public sealed record DepartmentMutationResult(
    DepartmentApiStatus Status,
    DepartmentDto? Department = null,
    string? ErrorCode = null,
    string? Message = null);

internal sealed record ApiErrorResponse(
    string Error,
    string Message);
