using BuildingBlocks.ValueObjects;

namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Perfil Líder de Projetos — fixo proporcional + comissão sobre faturamento líquido (REGRAS §6.5).
/// </summary>
public static class ProjectLeaderCalculator
{
    public static PayrollEntryResult Calculate(PayrollEntryInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var fullBase = BaseSalaryResolver.Resolve(input);
        var factor = ProportionalFactor.CalculateForEntry(input);
        var proportionalBase = Money.FromDecimal(fullBase * factor).RoundToCurrencyScale().Amount;

        var commission = ProjectLeaderCommissionCalculator.Calculate(
            input.ProjectEntries,
            input.CareerLevel,
            input.Department,
            input.GoalTier);
        var bonuses = ManualAdjustments.SumBonuses(input.BonusEntries);
        var deductions = ManualAdjustments.SumDeductions(input.DeductionEntries);

        var total = Money.FromDecimal(proportionalBase + commission + bonuses - deductions)
            .RoundToCurrencyScale()
            .Amount;

        return new PayrollEntryResult
        {
            TotalAmount = total,
            BaseSalary = proportionalBase,
            CommissionAmount = commission
        };
    }
}
