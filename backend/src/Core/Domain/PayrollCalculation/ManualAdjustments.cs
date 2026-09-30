namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Agrega bônus e descontos manuais da entrada (REGRAS §5.3).
/// </summary>
public static class ManualAdjustments
{
    public static decimal SumBonuses(IReadOnlyList<BonusEntryInput> bonusEntries) =>
        bonusEntries.Sum(entry => entry.Value);

    public static decimal SumDeductions(IReadOnlyList<DeductionEntryInput> deductionEntries) =>
        deductionEntries.Sum(entry => entry.Value);
}
