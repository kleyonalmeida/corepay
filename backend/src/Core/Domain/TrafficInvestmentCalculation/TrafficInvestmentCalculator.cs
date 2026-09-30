using BuildingBlocks.Results;
using BuildingBlocks.ValueObjects;

namespace Core.Domain.TrafficInvestmentCalculation;

/// <summary>
/// Motor puro de investimento de tráfego: semanas, gastos, depósitos, imposto e sugestão (REGRAS §8).
/// </summary>
public static class TrafficInvestmentCalculator
{
    public static Result<TrafficInvestmentResult> Calculate(TrafficInvestmentInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.MonthlyTarget < 0m)
        {
            return Result<TrafficInvestmentResult>.Failure(
                Error.Validation(
                    "traffic.investment.negative_monthly_target",
                    "A meta mensal não pode ser negativa."));
        }

        var validationResult = ValidateWeekStructure(input.Weeks);
        if (validationResult.IsFailure)
        {
            return Result<TrafficInvestmentResult>.Failure(validationResult.Error!);
        }

        var weeks = new List<TrafficWeekResult>(input.Weeks.Count);

        foreach (var week in input.Weeks)
        {
            var weekResult = CalculateWeekCore(week, input.MonthlyTarget);
            if (weekResult.IsFailure)
            {
                return Result<TrafficInvestmentResult>.Failure(weekResult.Error!);
            }

            weeks.Add(weekResult.Value);
        }

        return Result<TrafficInvestmentResult>.Success(new TrafficInvestmentResult
        {
            MonthlyTarget = input.MonthlyTarget,
            Weeks = weeks
        });
    }

    public static Result<TrafficWeekResult> CalculateWeek(TrafficWeekInput week, decimal monthlyTarget)
    {
        ArgumentNullException.ThrowIfNull(week);

        if (monthlyTarget < 0m)
        {
            return Result<TrafficWeekResult>.Failure(
                Error.Validation(
                    "traffic.investment.negative_monthly_target",
                    "A meta mensal não pode ser negativa."));
        }

        var validationResult = ValidateWeekStructure([week]);
        if (validationResult.IsFailure)
        {
            return Result<TrafficWeekResult>.Failure(validationResult.Error!);
        }

        return CalculateWeekCore(week, monthlyTarget);
    }

    public static decimal GetSuggestedNext(decimal monthlyTarget, decimal balance)
    {
        if (monthlyTarget <= 0m)
        {
            return 0m;
        }

        var weeklyBase = monthlyTarget / TrafficInvestmentConstants.TotalWeeks;
        return Math.Max(0m, weeklyBase - balance);
    }

    private static Result<TrafficWeekResult> CalculateWeekCore(TrafficWeekInput week, decimal monthlyTarget)
    {
        var depositValidation = ValidateDeposits(week.Deposits);
        if (depositValidation.IsFailure)
        {
            return Result<TrafficWeekResult>.Failure(depositValidation.Error!);
        }

        var spendValidation = ValidateSpends(week.ChannelSpends);
        if (spendValidation.IsFailure)
        {
            return Result<TrafficWeekResult>.Failure(spendValidation.Error!);
        }

        var requestedAmount = week.Deposits.Sum(deposit => deposit.RequestedAmount);
        var depositedAmount = week.Deposits.Sum(deposit => deposit.DepositedAmount);
        var spentAmount = week.ChannelSpends.Sum(spend => spend.Amount);
        var taxAmount = CalculateTax(spentAmount);
        var totalAmount = spentAmount + taxAmount;
        var balance = depositedAmount - spentAmount - taxAmount;
        var suggestedNext = GetSuggestedNext(monthlyTarget, balance);

        return Result<TrafficWeekResult>.Success(new TrafficWeekResult
        {
            WeekNumber = week.WeekNumber,
            RequestedAmount = requestedAmount,
            DepositedAmount = depositedAmount,
            SpentAmount = spentAmount,
            TaxAmount = taxAmount,
            TotalAmount = totalAmount,
            Balance = balance,
            SuggestedNext = suggestedNext
        });
    }

    private static decimal CalculateTax(decimal spentAmount) =>
        spentAmount == 0m
            ? 0m
            : Money.FromDecimal(spentAmount * TrafficInvestmentConstants.TaxRate)
                .RoundToCurrencyScale()
                .Amount;

    private static Result ValidateWeekStructure(IReadOnlyList<TrafficWeekInput> weeks)
    {
        var seenWeeks = new HashSet<int>();

        foreach (var week in weeks)
        {
            if (week.WeekNumber is < 1 or > TrafficInvestmentConstants.TotalWeeks)
            {
                return Result.Failure(
                    Error.Validation(
                        "traffic.investment.invalid_week_number",
                        $"O número da semana deve estar entre 1 e {TrafficInvestmentConstants.TotalWeeks}."));
            }

            if (!seenWeeks.Add(week.WeekNumber))
            {
                return Result.Failure(
                    Error.Validation(
                        "traffic.investment.duplicate_week",
                        "Não é permitido informar a mesma semana mais de uma vez."));
            }
        }

        return Result.Success();
    }

    private static Result ValidateDeposits(IReadOnlyList<TrafficDepositInput> deposits)
    {
        foreach (var deposit in deposits)
        {
            if (deposit.RequestedAmount < 0m || deposit.DepositedAmount < 0m)
            {
                return Result.Failure(
                    Error.Validation(
                        "traffic.investment.negative_deposit",
                        "Valores de depósito não podem ser negativos."));
            }
        }

        return Result.Success();
    }

    private static Result ValidateSpends(IReadOnlyList<TrafficChannelSpendInput> spends)
    {
        foreach (var spend in spends)
        {
            if (spend.Amount < 0m)
            {
                return Result.Failure(
                    Error.Validation(
                        "traffic.investment.negative_spend",
                        "Valores de gasto não podem ser negativos."));
            }
        }

        return Result.Success();
    }
}
