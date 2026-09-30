namespace Core.Domain;

public sealed class Collaborator
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public Guid DepartmentId { get; set; }

    public Department Department { get; set; } = null!;

    public Guid? CareerLevelId { get; set; }

    public CareerLevel? CareerLevel { get; set; }

    public string? JobTitle { get; set; }

    public DateOnly? AdmissionDate { get; set; }

    public DateOnly? DismissalDate { get; set; }

    public string? PixKey { get; set; }

    public decimal? BaseSalary { get; set; }

    public string? Email { get; set; }

    public string? PhotoUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public CalculationProfile? CalculationProfileOverride { get; set; }
}
