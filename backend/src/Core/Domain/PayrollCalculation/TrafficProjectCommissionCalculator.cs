using BuildingBlocks.Results;
using BuildingBlocks.ValueObjects;

namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Comissão automática de tráfego por projeto: investimento + CPA (REGRAS §6.3).
/// </summary>
public static class TrafficProjectCommissionCalculator
{
    public static Result<decimal> Calculate(
        IReadOnlyList<TrafficProjectEntryInput> trafficProjectEntries,
        CareerLevel? careerLevel,
        CareerLevel? trafficSeniorLevel)
    {
        if (trafficProjectEntries.Count == 0 || careerLevel is null)
        {
            return Result<decimal>.Success(0m);
        }

        var pct = careerLevel.TrafficInvestmentCommissionPct;
        var total = 0m;

        foreach (var projectEntry in trafficProjectEntries)
        {
            var projectResult = CalculateProject(projectEntry, careerLevel, trafficSeniorLevel);
            if (projectResult.IsFailure)
            {
                return Result<decimal>.Failure(projectResult.Error!);
            }

            total += projectResult.Value;
        }

        return Result<decimal>.Success(total);
    }

    public static Result<decimal> CalculateProject(
        TrafficProjectEntryInput projectEntry,
        CareerLevel? careerLevel,
        CareerLevel? trafficSeniorLevel)
    {
        if (careerLevel is null)
        {
            return Result<decimal>.Success(0m);
        }

        var pct = careerLevel.TrafficInvestmentCommissionPct;
        var investmentCommission = pct == 0m || projectEntry.InvestedAmount == 0m
            ? 0m
            : Percentage.FromPercentPoints(pct)
                .ApplyTo(Money.FromDecimal(projectEntry.InvestedAmount))
                .RoundToCurrencyScale()
                .Amount;

        var cpaResult = CalculateCpaCommission(
            projectEntry.CpaEntries,
            careerLevel,
            trafficSeniorLevel);

        if (cpaResult.IsFailure)
        {
            return Result<decimal>.Failure(cpaResult.Error!);
        }

        return Result<decimal>.Success(investmentCommission + cpaResult.Value);
    }

    private static Result<decimal> CalculateCpaCommission(
        IReadOnlyList<TrafficCpaEntryInput> cpaEntries,
        CareerLevel careerLevel,
        CareerLevel? trafficSeniorLevel)
    {
        if (cpaEntries.Count == 0)
        {
            return Result<decimal>.Success(0m);
        }

        var total = 0m;

        foreach (var cpaEntry in cpaEntries)
        {
            if (cpaEntry.Count <= 0)
            {
                continue;
            }

            var rateResult = TrafficCpaRateHelper.GetTrafficCpaRate(
                cpaEntry.HouseKey,
                cpaEntry.Kind,
                careerLevel,
                trafficSeniorLevel);

            if (rateResult.IsFailure)
            {
                return Result<decimal>.Failure(rateResult.Error!);
            }

            var lineTotal = Money.FromDecimal(cpaEntry.Count * rateResult.Value)
                .RoundToCurrencyScale()
                .Amount;

            total += lineTotal;
        }

        return Result<decimal>.Success(total);
    }
}
