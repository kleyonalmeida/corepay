using WebApp.Blazor.Formatting;
using WebApp.Blazor.Services;

namespace WebApp.Blazor.Components.Traffic;

public sealed class TrafficWeekDepositEditState
{
    public string RequestedAmountText { get; set; } = "0";

    public string DepositedAmountText { get; set; } = "0";

    public string StatusValue =>
        (ParseMoney(DepositedAmountText) > 0m
            ? TrafficDepositStatusDto.Deposited
            : ParseMoney(RequestedAmountText) > 0m
                ? TrafficDepositStatusDto.Requested
                : TrafficDepositStatusDto.Pending).ToString();

    public TrafficWeekDepositRequest ToRequest() =>
        new(
            ParseMoney(RequestedAmountText),
            ParseMoney(DepositedAmountText));

    public static TrafficWeekDepositEditState FromDto(TrafficWeekDepositDto deposit) =>
        new()
        {
            RequestedAmountText = DecimalInputParser.FormatMoneyInput(deposit.RequestedAmount),
            DepositedAmountText = DecimalInputParser.FormatMoneyInput(deposit.DepositedAmount)
        };

    private static decimal ParseMoney(string? value) =>
        DecimalInputParser.TryParse(value, out var parsed) ? parsed : 0m;
}

public sealed class TrafficWeekEditState
{
    public int WeekNumber { get; init; }

    public Dictionary<TrafficMediaChannelDto, string> ChannelAmounts { get; } = new();

    public List<TrafficWeekDepositEditState> Deposits { get; } = [];

    public TrafficWeekRequest ToRequest()
    {
        var channelSpends = TrafficMediaChannelLabels.AllChannels
            .Select(channel => new TrafficWeekChannelSpendRequest(channel, ParseMoney(GetChannelAmount(channel))))
            .Where(spend => spend.Amount > 0m)
            .ToList();

        return new TrafficWeekRequest(
            WeekNumber,
            Deposits.Select(deposit => deposit.ToRequest()).ToList(),
            channelSpends);
    }

    public static TrafficWeekEditState FromDto(TrafficWeekDto week)
    {
        var state = new TrafficWeekEditState { WeekNumber = week.WeekNumber };
        foreach (var channel in TrafficMediaChannelLabels.AllChannels)
        {
            var amount = week.ChannelSpends.FirstOrDefault(spend => spend.Channel == channel)?.Amount ?? 0m;
            state.ChannelAmounts[channel] = DecimalInputParser.FormatMoneyInput(amount);
        }

        state.Deposits.AddRange(week.Deposits.Select(TrafficWeekDepositEditState.FromDto));
        return state;
    }

    public static TrafficWeekEditState Empty(int weekNumber)
    {
        var state = new TrafficWeekEditState { WeekNumber = weekNumber };
        foreach (var channel in TrafficMediaChannelLabels.AllChannels)
        {
            state.ChannelAmounts[channel] = DecimalInputParser.FormatMoneyInput(0m);
        }

        return state;
    }

    private string GetChannelAmount(TrafficMediaChannelDto channel) =>
        ChannelAmounts.TryGetValue(channel, out var amount) ? amount : DecimalInputParser.FormatMoneyInput(0m);

    private static decimal ParseMoney(string? value) =>
        DecimalInputParser.TryParse(value, out var parsed) ? parsed : 0m;
}

public sealed class TrafficInvestmentEditState
{
    public string ProjectId { get; set; } = string.Empty;

    public string MonthValue { get; set; } = "1";

    public string YearValue { get; set; } = string.Empty;

    public string MonthlyTarget { get; set; } = "0";

    public List<TrafficWeekEditState> Weeks { get; } = [];

    public void ResetForCreate(int defaultMonth, int defaultYear)
    {
        ProjectId = string.Empty;
        MonthValue = defaultMonth.ToString();
        YearValue = defaultYear.ToString();
        MonthlyTarget = DecimalInputParser.FormatMoneyInput(0m);
        Weeks.Clear();
        for (var week = 1; week <= 4; week++)
        {
            Weeks.Add(TrafficWeekEditState.Empty(week));
        }
    }

    public void LoadFromDto(TrafficInvestmentDto investment)
    {
        ProjectId = investment.ProjectId.ToString();
        MonthValue = investment.Month.ToString();
        YearValue = investment.Year.ToString();
        MonthlyTarget = DecimalInputParser.FormatMoneyInput(investment.MonthlyTarget);
        Weeks.Clear();
        Weeks.AddRange(investment.Weeks.OrderBy(week => week.WeekNumber).Select(TrafficWeekEditState.FromDto));
        for (var weekNumber = Weeks.Count + 1; weekNumber <= 4; weekNumber++)
        {
            Weeks.Add(TrafficWeekEditState.Empty(weekNumber));
        }
    }

    public bool TryBuildRequest(out TrafficInvestmentRequest? request, out string? error)
    {
        if (!Guid.TryParse(ProjectId, out var projectId))
        {
            request = null;
            error = "Selecione um projeto.";
            return false;
        }

        if (!int.TryParse(MonthValue, out var month) || month is < 1 or > 12)
        {
            request = null;
            error = "Selecione um mês válido.";
            return false;
        }

        if (!int.TryParse(YearValue, out var year))
        {
            request = null;
            error = "Selecione um ano válido.";
            return false;
        }

        var monthlyTarget = ParseMoney(MonthlyTarget);
        var weeks = Weeks.Select(week => week.ToRequest()).ToList();
        if (!HasOperationalContent(monthlyTarget, weeks))
        {
            request = null;
            error = "Informe uma meta positiva ou ao menos um depósito/gasto maior que zero.";
            return false;
        }

        request = new TrafficInvestmentRequest(
            projectId,
            month,
            year,
            monthlyTarget,
            weeks);
        error = null;
        return true;
    }

    private static bool HasOperationalContent(decimal monthlyTarget, IReadOnlyList<TrafficWeekRequest> weeks)
    {
        if (monthlyTarget > 0m)
        {
            return true;
        }

        foreach (var week in weeks)
        {
            foreach (var deposit in week.Deposits)
            {
                if (deposit.RequestedAmount > 0m || deposit.DepositedAmount > 0m)
                {
                    return true;
                }
            }

            foreach (var spend in week.ChannelSpends)
            {
                if (spend.Amount > 0m)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static decimal ParseMoney(string? value) =>
        DecimalInputParser.TryParse(value, out var parsed) ? parsed : 0m;
}
