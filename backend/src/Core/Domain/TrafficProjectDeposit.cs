namespace Core.Domain;

/// <summary>
/// Aporte datado por projeto (REGRAS §8) — distinto dos depósitos semanais do card.
/// </summary>
public sealed class TrafficProjectDeposit
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public DateOnly DepositDate { get; set; }

    public decimal Amount { get; set; }

    public string? Notes { get; set; }
}
