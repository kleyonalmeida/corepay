using BuildingBlocks.Results;

namespace Core.Application.Traffic;

public interface ITrafficInvestmentStore
{
    Task<Result<IReadOnlyList<TrafficInvestmentListItemResponse>>> GetListAsync(
        TrafficInvestmentListFilters filters,
        CancellationToken cancellationToken = default);

    Task<Result<TrafficInvestmentResponse>> GetByIdAsync(
        Guid trafficInvestmentId,
        CancellationToken cancellationToken = default);

    Task<Result<TrafficInvestmentResponse>> CreateAsync(
        CreateTrafficInvestmentRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<TrafficInvestmentResponse>> UpdateAsync(
        Guid trafficInvestmentId,
        UpdateTrafficInvestmentRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<TrafficProjectDepositResponse>>> GetProjectDepositsAsync(
        TrafficProjectDepositListFilters filters,
        CancellationToken cancellationToken = default);

    Task<Result<TrafficProjectDepositResponse>> CreateProjectDepositAsync(
        CreateTrafficProjectDepositRequest request,
        CancellationToken cancellationToken = default);
}
