using BuildingBlocks.ValueObjects;

namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Perfil Supervisor Comercial — fixo proporcional + comissões por projeto + Rev analista (REGRAS §6.2).
/// </summary>
public static class CommercialSupervisorCalculator
{
    public static PayrollEntryResult Calculate(PayrollEntryInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var fullBase = BaseSalaryResolver.Resolve(input);
        var factor = ProportionalFactor.CalculateForEntry(input);
        var proportionalBase = Money.FromDecimal(fullBase * factor).RoundToCurrencyScale().Amount;

        var projectCommission = CommercialSupervisorProjectCommissionCalculator.Calculate(
            input.SupervisorProjectEntries,
            input.CareerLevel);
        var autoCommission = projectCommission + input.SupervisorAnalystRevenue;
        var bonuses = ManualAdjustments.SumBonuses(input.BonusEntries);
        var deductions = ManualAdjustments.SumDeductions(input.DeductionEntries);

        var total = Money.FromDecimal(proportionalBase + autoCommission + bonuses - deductions)
            .RoundToCurrencyScale()
            .Amount;

        return new PayrollEntryResult
        {
            TotalAmount = total,
            BaseSalary = proportionalBase,
            CommissionAmount = autoCommission
        };
    }
}
