using Core.Domain;
using Core.Domain.PayrollCalculation;
using System.Text.Json.Serialization;

namespace Core.Application.Payrolls;

public sealed record PayrollEditorProjectOption(
    Guid Id,
    string Name,
    ProjectPlatform Platform,
    bool ExcludesGoalBonus,
    bool IsDefaultAllocationTarget,
    bool ExcludesSupervisorFixedAllocation);

public sealed record PayrollEditorDepartmentOption(Guid Id, string Name);

public sealed record PayrollEditorCareerLevelOption(
    Guid Id,
    string Name,
    Guid? DepartmentId,
    CalculationProfile Profile);

public sealed record PayrollEditorOptionsResponse(
    IReadOnlyList<PayrollEditorProjectOption> Projects,
    IReadOnlyList<PayrollEditorDepartmentOption> Departments,
    IReadOnlyList<PayrollEditorCareerLevelOption> CareerLevels);

public sealed record PayrollEntryPayloadResponse(
    IReadOnlyList<ProjectEntryInput> ProjectEntries,
    IReadOnlyList<RateioProjectEntryInput> RateioProjectEntries,
    IReadOnlyList<CommercialAnalystProjectEntryInput> CommercialProjectEntries,
    IReadOnlyList<SupervisorProjectEntryInput> SupervisorProjectEntries,
    IReadOnlyList<TrafficProjectEntryInput> TrafficProjectEntries,
    IReadOnlyList<ManagementRevenueEntryInput> ManagementRevenueEntries,
    IReadOnlyList<BonusEntryInput> BonusEntries,
    IReadOnlyList<DeductionEntryInput> DeductionEntries,
    IReadOnlyList<ComplementPayingProjectInput> ComplementPayingProjects,
    IReadOnlyList<PayrollRoleChangeEntry> RoleChanges);

public sealed record PayrollEntryEditorResponse(
    Guid Id,
    Guid CollaboratorId,
    string CollaboratorName,
    string? CareerLevelName,
    string? PixKey,
    DateOnly? AdmissionDate,
    decimal? FullBaseSalary,
    CalculationProfile CalculationProfile,
    bool IsApproved,
    bool IsPaid,
    bool NfSent,
    GoalTier GoalTier,
    decimal? FinalSalary,
    int BetanoInternaCount,
    int BetanoMundoBetCount,
    decimal SupervisorAnalystRevenue,
    Guid? CommissionPayingProjectId,
    PayrollEntryPayloadResponse Payload,
    PayrollEntryResultResponse? Result = null,
    IReadOnlyList<PayrollProjectTotalResponse>? ProjectTotals = null);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record UpdatePayrollEntryRequest(
    Guid EntryId,
    GoalTier GoalTier = GoalTier.None,
    decimal? FinalSalary = null,
    int BetanoInternaCount = 0,
    int BetanoMundoBetCount = 0,
    decimal SupervisorAnalystRevenue = 0,
    Guid? CommissionPayingProjectId = null,
    IReadOnlyList<ManagementRevenueEntryInput>? ManagementRevenueEntries = null,
    IReadOnlyList<TrafficProjectEntryInput>? TrafficProjectEntries = null,
    IReadOnlyList<SupervisorProjectEntryInput>? SupervisorProjectEntries = null,
    IReadOnlyList<CommercialAnalystProjectEntryInput>? CommercialProjectEntries = null,
    IReadOnlyList<ProjectEntryInput>? ProjectEntries = null,
    IReadOnlyList<RateioProjectEntryInput>? RateioProjectEntries = null,
    IReadOnlyList<BonusEntryInput>? BonusEntries = null,
    IReadOnlyList<DeductionEntryInput>? DeductionEntries = null,
    IReadOnlyList<ComplementPayingProjectInput>? ComplementPayingProjects = null,
    IReadOnlyList<PayrollRoleChangeEntry>? RoleChanges = null);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PreviewPayrollEntryRequest(
    GoalTier GoalTier = GoalTier.None,
    decimal? FinalSalary = null,
    int BetanoInternaCount = 0,
    int BetanoMundoBetCount = 0,
    decimal SupervisorAnalystRevenue = 0,
    Guid? CommissionPayingProjectId = null,
    IReadOnlyList<ManagementRevenueEntryInput>? ManagementRevenueEntries = null,
    IReadOnlyList<TrafficProjectEntryInput>? TrafficProjectEntries = null,
    IReadOnlyList<SupervisorProjectEntryInput>? SupervisorProjectEntries = null,
    IReadOnlyList<CommercialAnalystProjectEntryInput>? CommercialProjectEntries = null,
    IReadOnlyList<ProjectEntryInput>? ProjectEntries = null,
    IReadOnlyList<RateioProjectEntryInput>? RateioProjectEntries = null,
    IReadOnlyList<BonusEntryInput>? BonusEntries = null,
    IReadOnlyList<DeductionEntryInput>? DeductionEntries = null,
    IReadOnlyList<ComplementPayingProjectInput>? ComplementPayingProjects = null,
    IReadOnlyList<PayrollRoleChangeEntry>? RoleChanges = null);

public sealed record PayrollEntryResultResponse(
    decimal TotalAmount,
    decimal BaseSalary,
    decimal CommissionAmount,
    decimal GoalBonusAmount,
    decimal GroupCommissionAmount,
    decimal PlatformTotal);

public sealed record PayrollProjectTotalResponse(Guid ProjectId, decimal Amount);

public sealed record PayrollEntryPreviewResponse(
    PayrollEntryResultResponse Result,
    IReadOnlyList<PayrollProjectTotalResponse> ProjectTotals);
