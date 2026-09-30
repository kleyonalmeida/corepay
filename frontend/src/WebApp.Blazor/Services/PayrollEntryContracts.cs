using System.Text.Json.Serialization;

namespace WebApp.Blazor.Services;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GoalTier
{
    None,
    Goal,
    SuperGoal
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TrafficCpaKind
{
    Supervised,
    Manager
}

public sealed record PayrollEditorProjectOptionDto(
    Guid Id,
    string Name,
    ProjectPlatform Platform,
    bool ExcludesGoalBonus,
    bool IsDefaultAllocationTarget,
    bool ExcludesSupervisorFixedAllocation);

public sealed record PayrollEditorDepartmentOptionDto(Guid Id, string Name);

public sealed record PayrollEditorCareerLevelOptionDto(
    Guid Id,
    string Name,
    Guid? DepartmentId,
    CalculationProfile Profile);

public sealed record PayrollEditorOptionsDto(
    IReadOnlyList<PayrollEditorProjectOptionDto> Projects,
    IReadOnlyList<PayrollEditorDepartmentOptionDto> Departments,
    IReadOnlyList<PayrollEditorCareerLevelOptionDto> CareerLevels);

public sealed record ProjectEntryDto(Guid ProjectId, decimal Value, decimal GroupPercentage = 0m);

public sealed record RateioProjectEntryDto(Guid ProjectId, decimal? RateioValue = null);

public sealed record CommercialAnalystProjectEntryDto(
    Guid ProjectId,
    ProjectPlatform Platform,
    int FtdTotal,
    int FtdSuperbet,
    bool IsFtdGoalReached,
    bool IsProjectFtdGoalReached,
    int CpaCount,
    decimal SalesAmount,
    bool IsSalesGoalReached,
    bool IsProjectSalesGoalReached,
    decimal Rev);

public sealed record SupervisorProjectEntryDto(
    Guid ProjectId,
    int FtdTotal,
    int FtdSuperbet,
    decimal SalesAmount,
    decimal AnalystRev,
    bool IsProjectFtdGoalReached,
    bool IsProjectSalesGoalReached,
    decimal DeviceRecharge = 0m,
    decimal BonusCpa = 0m);

public sealed record TrafficCpaEntryDto(string HouseKey, TrafficCpaKind Kind, int Count);

public sealed record TrafficProjectEntryDto(
    Guid ProjectId,
    decimal InvestedAmount,
    IReadOnlyList<TrafficCpaEntryDto>? CpaEntries = null);

public sealed record ManagementProjectBreakdownDto(Guid ProjectId, decimal Amount);

public sealed record ManagementRevenueEntryDto(
    decimal NetRevenue,
    IReadOnlyList<ManagementProjectBreakdownDto>? ProjectBreakdown = null);

public sealed record BonusEntryDto(Guid? ProjectId, decimal Value, string? Justification = null);

public sealed record DeductionEntryDto(decimal Value, string? Description = null);

public sealed record ComplementPayingProjectDto(Guid ProjectId, decimal Percentage);

public sealed record PayrollRoleChangeSnapshotDto(
    Guid? DepartmentId,
    Guid? CareerLevelId,
    decimal? FullBaseSalary,
    GoalTier GoalTier,
    decimal? FinalSalary,
    Guid? TrafficSeniorLevelId,
    IReadOnlyList<ManagementRevenueEntryDto>? ManagementRevenueEntries,
    IReadOnlyList<TrafficProjectEntryDto>? TrafficProjectEntries,
    decimal SupervisorAnalystRevenue,
    IReadOnlyList<SupervisorProjectEntryDto>? SupervisorProjectEntries,
    IReadOnlyList<CommercialAnalystProjectEntryDto>? CommercialProjectEntries,
    int BetanoInternaCount,
    int BetanoMundoBetCount,
    IReadOnlyList<ProjectEntryDto>? ProjectEntries,
    IReadOnlyList<RateioProjectEntryDto>? RateioProjectEntries,
    Guid? CommissionPayingProjectId,
    IReadOnlyList<ComplementPayingProjectDto>? ComplementPayingProjects);

public sealed record PayrollRoleChangeEntryDto(
    DateOnly ChangeDate,
    PayrollRoleChangeSnapshotDto Role);

public sealed record PayrollEntryPayloadDto(
    IReadOnlyList<ProjectEntryDto> ProjectEntries,
    IReadOnlyList<RateioProjectEntryDto> RateioProjectEntries,
    IReadOnlyList<CommercialAnalystProjectEntryDto> CommercialProjectEntries,
    IReadOnlyList<SupervisorProjectEntryDto> SupervisorProjectEntries,
    IReadOnlyList<TrafficProjectEntryDto> TrafficProjectEntries,
    IReadOnlyList<ManagementRevenueEntryDto> ManagementRevenueEntries,
    IReadOnlyList<BonusEntryDto> BonusEntries,
    IReadOnlyList<DeductionEntryDto> DeductionEntries,
    IReadOnlyList<ComplementPayingProjectDto> ComplementPayingProjects,
    IReadOnlyList<PayrollRoleChangeEntryDto> RoleChanges);

public sealed record PayrollEntryEditorDto(
    Guid Id,
    Guid CollaboratorId,
    string CollaboratorName,
    string? CareerLevelName,
    string? PixKey,
    DateOnly? AdmissionDate,
    decimal? FullBaseSalary,
    CalculationProfile CalculationProfile,
    bool IsApproved,
    bool IsPaid = false,
    bool NfSent = false,
    GoalTier GoalTier = GoalTier.None,
    decimal? FinalSalary = null,
    int BetanoInternaCount = 0,
    int BetanoMundoBetCount = 0,
    decimal SupervisorAnalystRevenue = 0,
    Guid? CommissionPayingProjectId = null,
    PayrollEntryPayloadDto? Payload = null,
    PayrollEntryResultDto? Result = null,
    IReadOnlyList<PayrollProjectTotalDto>? ProjectTotals = null);

public sealed record PreviewPayrollEntryRequestDto(
    GoalTier GoalTier = GoalTier.None,
    decimal? FinalSalary = null,
    int BetanoInternaCount = 0,
    int BetanoMundoBetCount = 0,
    decimal SupervisorAnalystRevenue = 0,
    Guid? CommissionPayingProjectId = null,
    IReadOnlyList<ManagementRevenueEntryDto>? ManagementRevenueEntries = null,
    IReadOnlyList<TrafficProjectEntryDto>? TrafficProjectEntries = null,
    IReadOnlyList<SupervisorProjectEntryDto>? SupervisorProjectEntries = null,
    IReadOnlyList<CommercialAnalystProjectEntryDto>? CommercialProjectEntries = null,
    IReadOnlyList<ProjectEntryDto>? ProjectEntries = null,
    IReadOnlyList<RateioProjectEntryDto>? RateioProjectEntries = null,
    IReadOnlyList<BonusEntryDto>? BonusEntries = null,
    IReadOnlyList<DeductionEntryDto>? DeductionEntries = null,
    IReadOnlyList<ComplementPayingProjectDto>? ComplementPayingProjects = null,
    IReadOnlyList<PayrollRoleChangeEntryDto>? RoleChanges = null);

public sealed record PayrollEntryResultDto(
    decimal TotalAmount,
    decimal BaseSalary,
    decimal CommissionAmount,
    decimal GoalBonusAmount,
    decimal GroupCommissionAmount,
    decimal PlatformTotal);

public sealed record PayrollProjectTotalDto(Guid ProjectId, decimal Amount);

public sealed record PayrollEntryPreviewDto(
    PayrollEntryResultDto Result,
    IReadOnlyList<PayrollProjectTotalDto> ProjectTotals);

public sealed record PayrollEntryPreviewResult(
    PayrollApiStatus Status,
    PayrollEntryPreviewDto? Preview = null,
    string? ErrorCode = null,
    string? Message = null);
