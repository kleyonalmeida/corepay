using BuildingBlocks.Results;
using Core.Application.Traffic;
using Core.Domain;
using Core.Domain.TrafficInvestmentCalculation;

namespace Infrastructure.Traffic;

internal static class TrafficInvestmentMapper
{
    public static TrafficInvestmentInput ToCalculatorInput(TrafficInvestment investment) =>
        new()
        {
            MonthlyTarget = investment.MonthlyTarget,
            Weeks = investment.Weeks
                .OrderBy(week => week.WeekNumber)
                .Select(ToWeekInput)
                .ToList()
        };

    public static TrafficWeekInput ToWeekInput(TrafficInvestmentWeek week) =>
        new()
        {
            WeekNumber = week.WeekNumber,
            Deposits = week.Deposits
                .Select(deposit => new TrafficDepositInput(
                    deposit.RequestedAmount,
                    deposit.DepositedAmount,
                    deposit.Status))
                .ToList(),
            ChannelSpends = week.ChannelSpends
                .Select(spend => new TrafficChannelSpendInput(spend.Channel, spend.Amount))
                .ToList()
        };

    public static Result<TrafficInvestmentResponse> MapDetail(
        TrafficInvestment investment,
        TrafficInvestmentResult calculation)
    {
        var weeks = investment.Weeks
            .OrderBy(week => week.WeekNumber)
            .Zip(
                calculation.Weeks.OrderBy(week => week.WeekNumber),
                (persisted, calculated) => new TrafficWeekResponse(
                    persisted.WeekNumber,
                    calculated.RequestedAmount,
                    calculated.DepositedAmount,
                    calculated.SpentAmount,
                    calculated.TaxAmount,
                    calculated.TotalAmount,
                    calculated.Balance,
                    calculated.SuggestedNext,
                    persisted.Deposits
                        .OrderBy(deposit => deposit.Id)
                        .Select(deposit => new TrafficWeekDepositResponse(
                            deposit.Id,
                            deposit.RequestedAmount,
                            deposit.DepositedAmount,
                            deposit.Status))
                        .ToList(),
                    persisted.ChannelSpends
                        .OrderBy(spend => spend.Channel)
                        .Select(spend => new TrafficWeekChannelSpendResponse(
                            spend.Channel,
                            spend.Amount))
                        .ToList()))
            .ToList();

        return Result<TrafficInvestmentResponse>.Success(new TrafficInvestmentResponse(
            investment.Id,
            investment.ProjectId,
            investment.Project.Name,
            investment.Month,
            investment.Year,
            investment.MonthlyTarget,
            weeks,
            MapMonthlyTotals(calculation.Weeks)));
    }

    public static TrafficInvestmentListItemResponse MapListItem(
        TrafficInvestment investment,
        TrafficInvestmentResult calculation) =>
        new(
            investment.Id,
            investment.ProjectId,
            investment.Project.Name,
            investment.Month,
            investment.Year,
            investment.MonthlyTarget,
            MapMonthlyTotals(calculation.Weeks));

    public static TrafficInvestmentMonthlyTotalsResponse MapMonthlyTotals(
        IReadOnlyList<TrafficWeekResult> weeks) =>
        new(
            weeks.Sum(week => week.RequestedAmount),
            weeks.Sum(week => week.DepositedAmount),
            weeks.Sum(week => week.SpentAmount),
            weeks.Sum(week => week.TaxAmount),
            weeks.Sum(week => week.TotalAmount),
            weeks.Sum(week => week.Balance));

    public static IReadOnlyList<TrafficWeekRequest> NormalizeWeekRequests(
        IReadOnlyList<TrafficWeekRequest>? weeks)
    {
        var byNumber = (weeks ?? [])
            .GroupBy(week => week.WeekNumber)
            .ToDictionary(group => group.Key, group => group.Last());

        return Enumerable.Range(1, TrafficInvestmentConstants.TotalWeeks)
            .Select(weekNumber =>
            {
                if (byNumber.TryGetValue(weekNumber, out var week))
                {
                    return week with
                    {
                        WeekNumber = weekNumber,
                        Deposits = week.Deposits ?? [],
                        ChannelSpends = week.ChannelSpends ?? []
                    };
                }

                return new TrafficWeekRequest(weekNumber, [], []);
            })
            .ToList();
    }

    public static void ApplyWeeks(TrafficInvestment investment, IReadOnlyList<TrafficWeekRequest> weeks)
    {
        investment.Weeks.Clear();

        foreach (var weekRequest in weeks.OrderBy(week => week.WeekNumber))
        {
            investment.Weeks.Add(CreateWeekEntity(investment.Id, weekRequest));
        }
    }

    public static TrafficInvestmentWeek CreateWeekEntity(Guid investmentId, TrafficWeekRequest weekRequest)
    {
        var week = new TrafficInvestmentWeek
        {
            Id = Guid.NewGuid(),
            TrafficInvestmentId = investmentId,
            WeekNumber = weekRequest.WeekNumber
        };

        PopulateWeekChildren(week, weekRequest);
        return week;
    }

    public static void PopulateWeekChildren(TrafficInvestmentWeek week, TrafficWeekRequest weekRequest)
    {
        foreach (var depositRequest in weekRequest.Deposits)
        {
            week.Deposits.Add(new TrafficWeekDeposit
            {
                Id = Guid.NewGuid(),
                TrafficInvestmentWeekId = week.Id,
                RequestedAmount = depositRequest.RequestedAmount,
                DepositedAmount = depositRequest.DepositedAmount,
                Status = ResolveDepositStatus(
                    depositRequest.RequestedAmount,
                    depositRequest.DepositedAmount)
            });
        }

        foreach (var spendRequest in weekRequest.ChannelSpends.Where(spend => spend.Amount > 0m))
        {
            week.ChannelSpends.Add(new TrafficWeekChannelSpend
            {
                Id = Guid.NewGuid(),
                TrafficInvestmentWeekId = week.Id,
                Channel = spendRequest.Channel,
                Amount = spendRequest.Amount
            });
        }
    }

    public static TrafficDepositStatus ResolveDepositStatus(
        decimal requestedAmount,
        decimal depositedAmount) =>
        depositedAmount > 0m
            ? TrafficDepositStatus.Deposited
            : requestedAmount > 0m
                ? TrafficDepositStatus.Requested
                : TrafficDepositStatus.Pending;
}
