using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using WebAPI.Tests.Common;

namespace WebAPI.Tests.MasterData;

[Collection("WebApiIntegration")]
public class PaymentMethodsEndpointTests : IClassFixture<CorePayWebApplicationFactory>
{
    private readonly CorePayWebApplicationFactory _factory;

    public PaymentMethodsEndpointTests(CorePayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateAdminUserAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);
        return client;
    }

    [Fact]
    public async Task CreatePaymentMethod_ShouldPersistAndRoundTrip()
    {
        var client = await CreateAdminClientAsync();
        var payload = MasterDataTestHelper.CreatePaymentMethodPayload(name: $"Cartão {Guid.NewGuid():N}");

        var createResponse = await client.PostAsJsonAsync("/api/v1/payment-methods", payload);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<PaymentMethodApiResponse>(MasterDataJsonOptions.Instance);
        created.Should().NotBeNull();
        created!.IsActive.Should().BeTrue();

        var getResponse = await client.GetAsync($"/api/v1/payment-methods/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeletePaymentMethod_ShouldReturnNoContent()
    {
        var client = await CreateAdminClientAsync();
        var payload = MasterDataTestHelper.CreatePaymentMethodPayload(name: $"Delete {Guid.NewGuid():N}");

        var createResponse = await client.PostAsJsonAsync("/api/v1/payment-methods", payload);
        var created = await createResponse.Content.ReadFromJsonAsync<PaymentMethodApiResponse>(MasterDataJsonOptions.Instance);

        var deleteResponse = await client.DeleteAsync($"/api/v1/payment-methods/{created!.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync($"/api/v1/payment-methods/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreatePaymentMethod_WithEmptyName_ShouldReturnBadRequest()
    {
        var client = await CreateAdminClientAsync();
        var payload = MasterDataTestHelper.CreatePaymentMethodPayload(name: "  ");

        var response = await client.PostAsJsonAsync("/api/v1/payment-methods", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record PaymentMethodApiResponse(Guid Id, string Name, bool IsActive);
}
