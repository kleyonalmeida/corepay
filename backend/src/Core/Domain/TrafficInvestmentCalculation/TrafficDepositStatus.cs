namespace Core.Domain.TrafficInvestmentCalculation;

/// <summary>
/// Status de workflow de um depósito semanal (REGRAS §8.1).
/// </summary>
public enum TrafficDepositStatus
{
    Pending,
    Requested,
    Deposited
}
