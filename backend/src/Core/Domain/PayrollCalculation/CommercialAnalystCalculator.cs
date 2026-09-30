using BuildingBlocks.ValueObjects;

namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Perfil Analista Comercial — comissão por projeto, mínimo proporcional, plataforma e Betano (REGRAS §6.1).
/// </summary>
public static class CommercialAnalystCalculator
{
    public static PayrollEntryResult Calculate(PayrollEntryInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var careerLevel = input.CareerLevel;
        var breakdowns = CommercialAnalystProjectCommissionCalculator.CalculateBreakdowns(
            input.CommercialProjectEntries,
            careerLevel);
        var projectCommission = breakdowns.Sum(b => b.Commission);
        var extraFtdBonus = CommercialAnalystCombinedFtdBonusCalculator.CalculateExtra(
            breakdowns,
            careerLevel);

        var betanoInternaValue = careerLevel?.BetanoInternaValue ?? 0m;
        var betanoMundoBetValue = careerLevel?.BetanoMundoBetValue ?? 0m;
        var betanoInternaCommission = Money.FromDecimal(input.BetanoInternaCount * betanoInternaValue)
            .RoundToCurrencyScale()
            .Amount;
        var betanoMundoBetTotal = Money.FromDecimal(input.BetanoMundoBetCount * betanoMundoBetValue)
            .RoundToCurrencyScale()
            .Amount;

        var commissionForMinimum = projectCommission + betanoInternaCommission + extraFtdBonus;

        var fullMinimum = BaseSalaryResolver.Resolve(input);
        var factor = ProportionalFactor.CalculateForEntry(input);
        var proportionalMinimum = Money.FromDecimal(fullMinimum * factor)
            .RoundToCurrencyScale()
            .Amount;
        var complement = Math.Max(0m, proportionalMinimum - commissionForMinimum);

        var platformTotal = CommercialAnalystPlatformCalculator.Calculate(
            input.CommercialProjectEntries,
            careerLevel);
        var manualBonuses = ManualAdjustments.SumBonuses(input.BonusEntries);
        var deductions = ManualAdjustments.SumDeductions(input.DeductionEntries);

        var platformInTotal = complement > 0m ? platformTotal : 0m;
        var total = Money.FromDecimal(
                commissionForMinimum
                + manualBonuses
                + complement
                + platformInTotal
                + betanoMundoBetTotal
                - deductions)
            .RoundToCurrencyScale()
            .Amount;

        return new PayrollEntryResult
        {
            TotalAmount = total,
            BaseSalary = complement,
            CommissionAmount = commissionForMinimum,
            PlatformTotal = platformTotal
        };
    }
}
