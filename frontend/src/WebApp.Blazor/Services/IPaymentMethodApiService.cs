namespace WebApp.Blazor.Services;

public interface IPaymentMethodApiService
{
    Task<PaymentMethodListResult> GetPaymentMethodsAsync(CancellationToken cancellationToken = default);

    Task<PaymentMethodMutationResult> CreatePaymentMethodAsync(
        PaymentMethodRequest request,
        CancellationToken cancellationToken = default);

    Task<PaymentMethodMutationResult> UpdatePaymentMethodAsync(
        Guid id,
        PaymentMethodRequest request,
        CancellationToken cancellationToken = default);

    Task<PaymentMethodDeleteResult> DeletePaymentMethodAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
