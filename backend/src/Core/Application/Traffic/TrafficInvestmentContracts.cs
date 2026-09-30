using Core.Domain.TrafficInvestmentCalculation;

namespace Core.Application.Traffic;

public sealed record TrafficInvestmentListFilters(
    int? Month,
    int? Year,
    Guid? ProjectId);

public sealed record TrafficProjectDepositListFilters(
    int? Month,
    int? Year,
    Guid? ProjectId);

public sealed record TrafficInvestmentMonthlyTotalsResponse(
    decimal RequestedAmount,
    decimal DepositedAmount,
    decimal SpentAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal Balance);

public sealed record TrafficWeekDepositResponse(
    Guid Id,
    decimal RequestedAmount,
    decimal DepositedAmount,
    TrafficDepositStatus Status);

public sealed record TrafficWeekChannelSpendResponse(
    TrafficMediaChannel Channel,
    decimal Amount);

public sealed record TrafficWeekResponse(
    int WeekNumber,
    decimal RequestedAmount,
    decimal DepositedAmount,
    decimal SpentAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal Balance,
    decimal SuggestedNext,
    IReadOnlyList<TrafficWeekDepositResponse> Deposits,
    IReadOnlyList<TrafficWeekChannelSpendResponse> ChannelSpends);

public sealed record TrafficInvestmentListItemResponse(
    Guid Id,
    Guid ProjectId,
    string ProjectName,
    int Month,
    int Year,
    decimal MonthlyTarget,
    TrafficInvestmentMonthlyTotalsResponse MonthlyTotals);

public sealed record TrafficInvestmentResponse(
    Guid Id,
    Guid ProjectId,
    string ProjectName,
    int Month,
    int Year,
    decimal MonthlyTarget,
    IReadOnlyList<TrafficWeekResponse> Weeks,
    TrafficInvestmentMonthlyTotalsResponse MonthlyTotals);

public sealed record TrafficWeekDepositRequest(
    decimal RequestedAmount,
    decimal DepositedAmount);

public sealed record TrafficWeekChannelSpendRequest(
    TrafficMediaChannel Channel,
    decimal Amount);

public sealed record TrafficWeekRequest(
    int WeekNumber,
    IReadOnlyList<TrafficWeekDepositRequest> Deposits,
    IReadOnlyList<TrafficWeekChannelSpendRequest> ChannelSpends);

public sealed record CreateTrafficInvestmentRequest(
    Guid ProjectId,
    int Month,
    int Year,
    decimal MonthlyTarget,
    IReadOnlyList<TrafficWeekRequest> Weeks);

public sealed record UpdateTrafficInvestmentRequest(
    Guid ProjectId,
    int Month,
    int Year,
    decimal MonthlyTarget,
    IReadOnlyList<TrafficWeekRequest> Weeks);

public sealed record TrafficProjectDepositResponse(
    Guid Id,
    Guid ProjectId,
    string ProjectName,
    DateOnly DepositDate,
    decimal Amount,
    string? Notes);

public sealed record CreateTrafficProjectDepositRequest(
    Guid ProjectId,
    DateOnly DepositDate,
    decimal Amount,
    string? Notes);
