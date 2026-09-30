using BuildingBlocks.Results;
using BuildingBlocks.ValueObjects;

namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Perfil Tráfego Pago — fixo proporcional + comissão investimento/CPA (REGRAS §6.3).
/// </summary>
public static class PaidTrafficCalculator
{
    public static Result<PayrollEntryResult> Calculate(PayrollEntryInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var fullBase = BaseSalaryResolver.Resolve(input);
        var factor = ProportionalFactor.CalculateForEntry(input);
        var proportionalBase = Money.FromDecimal(fullBase * factor).RoundToCurrencyScale().Amount;

        var allocationResult = TrafficFixedAllocationCalculator.Calculate(
            proportionalBase,
            input.RateioProjectEntries);

        if (allocationResult.IsFailure)
        {
            return Result<PayrollEntryResult>.Failure(allocationResult.Error!);
        }

        var commissionResult = TrafficProjectCommissionCalculator.Calculate(
            input.TrafficProjectEntries,
            input.CareerLevel,
            input.TrafficSeniorLevel);

        if (commissionResult.IsFailure)
        {
            return Result<PayrollEntryResult>.Failure(commissionResult.Error!);
        }

        var autoCommission = commissionResult.Value;
        var bonuses = ManualAdjustments.SumBonuses(input.BonusEntries);
        var deductions = ManualAdjustments.SumDeductions(input.DeductionEntries);
        var commissionAmount = autoCommission + bonuses;

        var total = Money.FromDecimal(proportionalBase + commissionAmount - deductions)
            .RoundToCurrencyScale()
            .Amount;

        return Result<PayrollEntryResult>.Success(new PayrollEntryResult
        {
            TotalAmount = total,
            BaseSalary = proportionalBase,
            CommissionAmount = commissionAmount
        });
    }
}
