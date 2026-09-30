namespace WebApp.Blazor.Services;

public interface ITrafficInvestmentApiService
{
    Task<TrafficInvestmentListResult> GetInvestmentsAsync(
        TrafficInvestmentListQuery? query = null,
        CancellationToken cancellationToken = default);

    Task<TrafficInvestmentDetailResult> GetInvestmentByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<TrafficInvestmentMutationResult> CreateInvestmentAsync(
        TrafficInvestmentRequest request,
        CancellationToken cancellationToken = default);

    Task<TrafficInvestmentMutationResult> UpdateInvestmentAsync(
        Guid id,
        TrafficInvestmentRequest request,
        CancellationToken cancellationToken = default);

    Task<TrafficProjectDepositListResult> GetProjectDepositsAsync(
        TrafficProjectDepositListQuery? query = null,
        CancellationToken cancellationToken = default);

    Task<TrafficProjectDepositMutationResult> CreateProjectDepositAsync(
        TrafficProjectDepositRequest request,
        CancellationToken cancellationToken = default);
}
