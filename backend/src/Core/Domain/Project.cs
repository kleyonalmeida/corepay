namespace Core.Domain;

public sealed class Project
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Client { get; set; }

    public ProjectPlatform Platform { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsDefaultAllocationTarget { get; set; }

    public bool ExcludesGoalBonus { get; set; }

    public bool ExcludesSupervisorFixedAllocation { get; set; }
}
