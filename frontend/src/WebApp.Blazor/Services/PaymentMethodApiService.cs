using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace WebApp.Blazor.Services;

public sealed class PaymentMethodApiService(HttpClient httpClient) : IPaymentMethodApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<PaymentMethodListResult> GetPaymentMethodsAsync(CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync("api/v1/payment-methods", cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new PaymentMethodListResult(PaymentMethodApiStatus.Error);
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new PaymentMethodListResult(PaymentMethodApiStatus.Forbidden);
        }

        if (!response.IsSuccessStatusCode)
        {
            return new PaymentMethodListResult(PaymentMethodApiStatus.Error);
        }

        var methods = await response.Content.ReadFromJsonAsync<List<PaymentMethodDto>>(JsonOptions, cancellationToken);
        return methods is null
            ? new PaymentMethodListResult(PaymentMethodApiStatus.Error)
            : new PaymentMethodListResult(PaymentMethodApiStatus.Success, methods);
    }

    public Task<PaymentMethodMutationResult> CreatePaymentMethodAsync(
        PaymentMethodRequest request,
        CancellationToken cancellationToken = default) =>
        SendMutationAsync(HttpMethod.Post, "api/v1/payment-methods", request, cancellationToken);

    public Task<PaymentMethodMutationResult> UpdatePaymentMethodAsync(
        Guid id,
        PaymentMethodRequest request,
        CancellationToken cancellationToken = default) =>
        SendMutationAsync(HttpMethod.Put, $"api/v1/payment-methods/{id}", request, cancellationToken);

    public async Task<PaymentMethodDeleteResult> DeletePaymentMethodAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.DeleteAsync($"api/v1/payment-methods/{id}", cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new PaymentMethodDeleteResult(PaymentMethodApiStatus.Error);
        }

        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return new PaymentMethodDeleteResult(PaymentMethodApiStatus.Success);
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new PaymentMethodDeleteResult(PaymentMethodApiStatus.Forbidden);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            var notFound = await ReadErrorAsync(response, cancellationToken);
            return new PaymentMethodDeleteResult(
                PaymentMethodApiStatus.NotFound,
                ErrorCode: notFound?.Error,
                Message: notFound?.Message);
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var conflict = await ReadErrorAsync(response, cancellationToken);
            return new PaymentMethodDeleteResult(
                PaymentMethodApiStatus.Conflict,
                ErrorCode: conflict?.Error,
                Message: conflict?.Message);
        }

        return new PaymentMethodDeleteResult(PaymentMethodApiStatus.Error);
    }

    private async Task<PaymentMethodMutationResult> SendMutationAsync(
        HttpMethod method,
        string url,
        PaymentMethodRequest request,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = method == HttpMethod.Post
                ? await httpClient.PostAsJsonAsync(url, request, JsonOptions, cancellationToken)
                : await httpClient.PutAsJsonAsync(url, request, JsonOptions, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new PaymentMethodMutationResult(PaymentMethodApiStatus.Error);
        }

        if (response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created)
        {
            var methodDto = await response.Content.ReadFromJsonAsync<PaymentMethodDto>(JsonOptions, cancellationToken);
            return methodDto is null
                ? new PaymentMethodMutationResult(PaymentMethodApiStatus.Error)
                : new PaymentMethodMutationResult(PaymentMethodApiStatus.Success, methodDto);
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new PaymentMethodMutationResult(PaymentMethodApiStatus.Forbidden);
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var conflict = await ReadErrorAsync(response, cancellationToken);
            return new PaymentMethodMutationResult(
                PaymentMethodApiStatus.Conflict,
                ErrorCode: conflict?.Error,
                Message: conflict?.Message);
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var validation = await ReadErrorAsync(response, cancellationToken);
            return new PaymentMethodMutationResult(
                PaymentMethodApiStatus.ValidationError,
                ErrorCode: validation?.Error,
                Message: validation?.Message);
        }

        return new PaymentMethodMutationResult(PaymentMethodApiStatus.Error);
    }

    private static async Task<ApiErrorResponse?> ReadErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions, cancellationToken);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
