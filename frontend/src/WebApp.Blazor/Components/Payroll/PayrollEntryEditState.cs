using WebApp.Blazor.Services;

namespace WebApp.Blazor.Components.Payroll;

public sealed class PayrollEntryEditState
{
    public PayrollEntryEditState(PayrollEntryEditorDto entry)
    {
        EntryId = entry.Id;
        CollaboratorId = entry.CollaboratorId;
        CollaboratorName = entry.CollaboratorName;
        CareerLevelName = entry.CareerLevelName;
        PixKey = entry.PixKey;
        AdmissionDate = entry.AdmissionDate;
        FullBaseSalary = entry.FullBaseSalary;
        CalculationProfile = entry.CalculationProfile;
        IsApproved = entry.IsApproved;
        GoalTier = entry.GoalTier;
        FinalSalary = entry.FinalSalary;
        BetanoInternaCount = entry.BetanoInternaCount;
        BetanoMundoBetCount = entry.BetanoMundoBetCount;
        SupervisorAnalystRevenue = entry.SupervisorAnalystRevenue;
        CommissionPayingProjectId = entry.CommissionPayingProjectId;
        var payload = entry.Payload ?? new PayrollEntryPayloadDto([], [], [], [], [], [], [], [], [], []);
        ProjectEntries = payload.ProjectEntries.ToList();
        RateioProjectEntries = payload.RateioProjectEntries.ToList();
        CommercialProjectEntries = payload.CommercialProjectEntries.ToList();
        SupervisorProjectEntries = payload.SupervisorProjectEntries.ToList();
        TrafficProjectEntries = payload.TrafficProjectEntries.ToList();
        ManagementRevenueEntries = payload.ManagementRevenueEntries.ToList();
        BonusEntries = payload.BonusEntries.ToList();
        DeductionEntries = payload.DeductionEntries.ToList();
        ComplementPayingProjects = payload.ComplementPayingProjects.ToList();
        RoleChanges = payload.RoleChanges.ToList();
        LastResult = entry.Result;
    }

    public Guid EntryId { get; }
    public Guid CollaboratorId { get; }
    public string CollaboratorName { get; }
    public string? CareerLevelName { get; }
    public string? PixKey { get; }
    public DateOnly? AdmissionDate { get; }
    public decimal? FullBaseSalary { get; }
    public CalculationProfile CalculationProfile { get; }
    public bool IsApproved { get; }

    public GoalTier GoalTier { get; set; }
    public decimal? FinalSalary { get; set; }
    public int BetanoInternaCount { get; set; }
    public int BetanoMundoBetCount { get; set; }
    public decimal SupervisorAnalystRevenue { get; set; }
    public Guid? CommissionPayingProjectId { get; set; }

    public List<ProjectEntryDto> ProjectEntries { get; }
    public List<RateioProjectEntryDto> RateioProjectEntries { get; }
    public List<CommercialAnalystProjectEntryDto> CommercialProjectEntries { get; }
    public List<SupervisorProjectEntryDto> SupervisorProjectEntries { get; }
    public List<TrafficProjectEntryDto> TrafficProjectEntries { get; }
    public List<ManagementRevenueEntryDto> ManagementRevenueEntries { get; }
    public List<BonusEntryDto> BonusEntries { get; }
    public List<DeductionEntryDto> DeductionEntries { get; }
    public List<ComplementPayingProjectDto> ComplementPayingProjects { get; }
    public List<PayrollRoleChangeEntryDto> RoleChanges { get; }

    public PayrollEntryResultDto? LastResult { get; set; }
    public string? PreviewError { get; set; }
    public bool IsCalculating { get; set; }

    public PreviewPayrollEntryRequestDto ToPreviewRequest() =>
        new(
            GoalTier,
            FinalSalary,
            BetanoInternaCount,
            BetanoMundoBetCount,
            SupervisorAnalystRevenue,
            CommissionPayingProjectId,
            ManagementRevenueEntries,
            TrafficProjectEntries,
            SupervisorProjectEntries,
            CommercialProjectEntries,
            ProjectEntries,
            RateioProjectEntries,
            BonusEntries,
            DeductionEntries,
            ComplementPayingProjects,
            RoleChanges);

    public UpdatePayrollEntryRequestDto ToSaveRequest()
    {
        var preview = ToPreviewRequest();
        return new UpdatePayrollEntryRequestDto(
            EntryId,
            preview.GoalTier,
            preview.FinalSalary,
            preview.BetanoInternaCount,
            preview.BetanoMundoBetCount,
            preview.SupervisorAnalystRevenue,
            preview.CommissionPayingProjectId,
            preview.ManagementRevenueEntries,
            preview.TrafficProjectEntries,
            preview.SupervisorProjectEntries,
            preview.CommercialProjectEntries,
            preview.ProjectEntries,
            preview.RateioProjectEntries,
            preview.BonusEntries,
            preview.DeductionEntries,
            preview.ComplementPayingProjects,
            preview.RoleChanges);
    }

    public decimal ComplementPercentageSum =>
        ComplementPayingProjects.Sum(p => p.Percentage);

    public bool IsComplementSumInvalid =>
        ComplementPayingProjects.Count > 0 && ComplementPercentageSum != 100m;
}
