namespace Core.Domain;

public sealed class Payroll
{
    public Guid Id { get; set; }

    public Guid DepartmentId { get; set; }

    public Department Department { get; set; } = null!;

    public int Month { get; set; }

    public int Year { get; set; }

    public PayrollStatus Status { get; set; } = PayrollStatus.Draft;

    public decimal TotalAmount { get; set; }

    public string? RejectionComment { get; set; }

    public string? SubmittedBy { get; set; }

    public string? SubmittedByUserId { get; set; }

    public string? ApprovedBy { get; set; }

    public DateTimeOffset? ApprovedAt { get; set; }

    public ICollection<PayrollCollaboratorEntry> Entries { get; set; } = [];
}
