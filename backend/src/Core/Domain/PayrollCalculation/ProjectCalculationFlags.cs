namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Semântica explícita de projetos especiais para o motor (substitui regex §11).
/// </summary>
public static class ProjectCalculationFlags
{
    public static bool IsDefaultAllocationTarget(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);
        return project.IsDefaultAllocationTarget;
    }

    public static bool ExcludesGoalBonus(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);
        return project.ExcludesGoalBonus;
    }

    public static bool ExcludesSupervisorFixedAllocation(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);
        return project.ExcludesSupervisorFixedAllocation;
    }

    public static bool IsAffiliatesProject(Project project, Guid affiliatesProjectId)
    {
        ArgumentNullException.ThrowIfNull(project);
        return project.Id == affiliatesProjectId;
    }

    public static bool IsLimaKarttosProject(Project project, Guid limaKarttosProjectId) =>
        IsProjectById(project, limaKarttosProjectId);

    public static bool IsFeiraProject(Project project, Guid feiraProjectId) =>
        IsProjectById(project, feiraProjectId);

    public static bool IsThreeCSportsProject(Project project, Guid threeCSportsProjectId) =>
        IsProjectById(project, threeCSportsProjectId);

    private static bool IsProjectById(Project project, Guid expectedId) =>
        project.Id == expectedId;
}
