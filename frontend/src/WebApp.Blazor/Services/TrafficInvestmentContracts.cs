namespace WebApp.Blazor.Services;

public enum TrafficInvestmentApiStatus
{
    Success,
    ValidationError,
    NotFound,
    Conflict,
    Forbidden,
    Error
}

public enum TrafficDepositStatusDto
{
    Pending,
    Requested,
    Deposited
}

public enum TrafficMediaChannelDto
{
    Telegram,
    Instagram,
    Story,
    Direct,
    Remarketing,
    Other
}

public sealed record TrafficInvestmentMonthlyTotalsDto(
    decimal RequestedAmount,
    decimal DepositedAmount,
    decimal SpentAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal Balance);

public sealed record TrafficWeekDepositDto(
    Guid Id,
    decimal RequestedAmount,
    decimal DepositedAmount,
    TrafficDepositStatusDto Status);

public sealed record TrafficWeekChannelSpendDto(
    TrafficMediaChannelDto Channel,
    decimal Amount);

public sealed record TrafficWeekDto(
    int WeekNumber,
    decimal RequestedAmount,
    decimal DepositedAmount,
    decimal SpentAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal Balance,
    decimal SuggestedNext,
    IReadOnlyList<TrafficWeekDepositDto> Deposits,
    IReadOnlyList<TrafficWeekChannelSpendDto> ChannelSpends);

public sealed record TrafficInvestmentListItemDto(
    Guid Id,
    Guid ProjectId,
    string ProjectName,
    int Month,
    int Year,
    decimal MonthlyTarget,
    TrafficInvestmentMonthlyTotalsDto MonthlyTotals);

public sealed record TrafficInvestmentDto(
    Guid Id,
    Guid ProjectId,
    string ProjectName,
    int Month,
    int Year,
    decimal MonthlyTarget,
    IReadOnlyList<TrafficWeekDto> Weeks,
    TrafficInvestmentMonthlyTotalsDto MonthlyTotals);

public sealed record TrafficWeekDepositRequest(
    decimal RequestedAmount,
    decimal DepositedAmount);

public sealed record TrafficWeekChannelSpendRequest(
    TrafficMediaChannelDto Channel,
    decimal Amount);

public sealed record TrafficWeekRequest(
    int WeekNumber,
    IReadOnlyList<TrafficWeekDepositRequest> Deposits,
    IReadOnlyList<TrafficWeekChannelSpendRequest> ChannelSpends);

public sealed record TrafficInvestmentRequest(
    Guid ProjectId,
    int Month,
    int Year,
    decimal MonthlyTarget,
    IReadOnlyList<TrafficWeekRequest> Weeks);

public sealed record TrafficInvestmentListQuery(
    int? Month = null,
    int? Year = null,
    Guid? ProjectId = null);

public sealed record TrafficProjectDepositDto(
    Guid Id,
    Guid ProjectId,
    string ProjectName,
    DateOnly DepositDate,
    decimal Amount,
    string? Notes);

public sealed record TrafficProjectDepositRequest(
    Guid ProjectId,
    DateOnly DepositDate,
    decimal Amount,
    string? Notes);

public sealed record TrafficProjectDepositListQuery(
    int? Month = null,
    int? Year = null,
    Guid? ProjectId = null);

public sealed record TrafficInvestmentListResult(
    TrafficInvestmentApiStatus Status,
    IReadOnlyList<TrafficInvestmentListItemDto>? Investments = null,
    string? ErrorCode = null,
    string? Message = null);

public sealed record TrafficInvestmentDetailResult(
    TrafficInvestmentApiStatus Status,
    TrafficInvestmentDto? Investment = null,
    string? ErrorCode = null,
    string? Message = null);

public sealed record TrafficInvestmentMutationResult(
    TrafficInvestmentApiStatus Status,
    TrafficInvestmentDto? Investment = null,
    string? ErrorCode = null,
    string? Message = null);

public sealed record TrafficProjectDepositListResult(
    TrafficInvestmentApiStatus Status,
    IReadOnlyList<TrafficProjectDepositDto>? Deposits = null,
    string? ErrorCode = null,
    string? Message = null);

public sealed record TrafficProjectDepositMutationResult(
    TrafficInvestmentApiStatus Status,
    TrafficProjectDepositDto? Deposit = null,
    string? ErrorCode = null,
    string? Message = null);
