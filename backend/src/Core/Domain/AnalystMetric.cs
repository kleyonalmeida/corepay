namespace Core.Domain;

public sealed class AnalystMetric
{
    public Guid Id { get; set; }

    public Guid CollaboratorId { get; set; }

    public Collaborator Collaborator { get; set; } = null!;

    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public int Month { get; set; }

    public int Year { get; set; }

    public int FtdTotal { get; set; }

    public int CpaCount { get; set; }
}
