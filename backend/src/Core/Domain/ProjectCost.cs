namespace Core.Domain;

public sealed class ProjectCost
{
    public Guid Id { get; set; }

    public ProjectCostType Type { get; set; }

    public ProjectCostCategory Category { get; set; }

    public decimal Amount { get; set; }

    public DateOnly TransactionDate { get; set; }

    public int Month { get; set; }

    public int Year { get; set; }

    public Guid? ProjectId { get; set; }

    public Project? Project { get; set; }

    public Guid? DepartmentId { get; set; }

    public Department? Department { get; set; }

    public Guid? PaymentMethodId { get; set; }

    public PaymentMethod? PaymentMethod { get; set; }

    public string? Requester { get; set; }

    public string? PurchaseLocation { get; set; }

    public int? InstallmentNumber { get; set; }

    public int? InstallmentTotal { get; set; }

    public Guid? CompraId { get; set; }

    public string? AttachmentUrl { get; set; }

    public string? Notes { get; set; }

    public Guid? FacilitiesLancamentoId { get; set; }
}
