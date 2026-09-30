using Core.Domain.PayrollCalculation;

namespace Core.Domain;

public sealed class PayrollCollaboratorEntry
{
    public Guid Id { get; set; }

    public Guid PayrollId { get; set; }

    public Payroll Payroll { get; set; } = null!;

    public Guid CollaboratorId { get; set; }

    // Snapshot leve (UI / histórico)
    public string CollaboratorName { get; set; } = string.Empty;

    public string? PixKey { get; set; }

    public DateOnly? AdmissionDate { get; set; }

    public string? CareerLevelName { get; set; }

    public CalculationProfile CalculationProfile { get; set; }

    public Guid? DepartmentId { get; set; }

    public Guid? CareerLevelId { get; set; }

    public decimal? FullBaseSalary { get; set; }

    public GoalTier GoalTier { get; set; }

    public decimal? FinalSalary { get; set; }

    public int BetanoInternaCount { get; set; }

    public int BetanoMundoBetCount { get; set; }

    public decimal SupervisorAnalystRevenue { get; set; }

    public Guid? CommissionPayingProjectId { get; set; }

    public Guid? TrafficSeniorLevelId { get; set; }

    public bool IsApproved { get; set; }

    public bool IsPaid { get; set; }

    public bool NfSent { get; set; }

    /// <summary>Blocos variáveis serializados como JSON.</summary>
    public PayrollCollaboratorEntryPayload Payload { get; set; } = new();
}
