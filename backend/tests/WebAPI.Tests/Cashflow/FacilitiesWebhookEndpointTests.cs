using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebAPI.Tests.Common;
using WebAPI.Tests.MasterData;
using Xunit;

namespace WebAPI.Tests.Cashflow;

[Collection("WebApiIntegration")]
public class FacilitiesWebhookEndpointTests : IClassFixture<CorePayWebApplicationFactory>
{
    private readonly CorePayWebApplicationFactory _factory;

    public FacilitiesWebhookEndpointTests(CorePayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_ValidEntrada_ShouldCreateEntryWithoutJwt()
    {
        var client = _factory.CreateClient();
        var lancamentoId = Guid.NewGuid();
        var request = FacilitiesWebhookTestHelper.CreateSignedPostFromPayload(lancamentoId, valor: 750m);

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<FacilitiesWebhookApiResponse>();
        body.Should().NotBeNull();
        body!.Id.Should().NotBeEmpty();
        body.LancamentoId.Should().Be(lancamentoId);
        body.Idempotent.Should().BeFalse();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entry = await dbContext.ProjectCosts.SingleAsync(c => c.FacilitiesLancamentoId == lancamentoId);
        entry.Amount.Should().Be(750m);
        entry.Type.Should().Be(Core.Domain.ProjectCostType.Entrada);
    }

    [Fact]
    public async Task Post_ValidSaida_ShouldCreateEntryWithPaymentMethodAndDepartment()
    {
        var adminClient = await CreateAdminClientAsync();
        var (paymentMethodId, departmentId) = await CreatePaymentMethodAndDepartmentAsync(adminClient);
        var client = _factory.CreateClient();
        var lancamentoId = Guid.NewGuid();

        var request = FacilitiesWebhookTestHelper.CreateSignedPostFromPayload(
            lancamentoId,
            tipo: "saida",
            categoria: "folhaPagamento",
            valor: 300m,
            departmentId: departmentId,
            paymentMethodId: paymentMethodId);

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entry = await dbContext.ProjectCosts.SingleAsync(c => c.FacilitiesLancamentoId == lancamentoId);
        entry.PaymentMethodId.Should().Be(paymentMethodId);
        entry.DepartmentId.Should().Be(departmentId);
    }

    [Fact]
    public async Task Post_ReplaySameLancamentoId_ShouldReturnIdempotentTrueWithoutDuplicate()
    {
        var client = _factory.CreateClient();
        var lancamentoId = Guid.NewGuid();
        var request = FacilitiesWebhookTestHelper.CreateSignedPostFromPayload(lancamentoId);

        var first = await client.SendAsync(request);
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstBody = await first.Content.ReadFromJsonAsync<FacilitiesWebhookApiResponse>();

        var replay = FacilitiesWebhookTestHelper.CreateSignedPostFromPayload(lancamentoId);
        var second = await client.SendAsync(replay);
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondBody = await second.Content.ReadFromJsonAsync<FacilitiesWebhookApiResponse>();
        secondBody!.Idempotent.Should().BeTrue();
        secondBody.Id.Should().Be(firstBody!.Id);
        secondBody.LancamentoId.Should().Be(lancamentoId);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await dbContext.ProjectCosts.CountAsync(c => c.FacilitiesLancamentoId == lancamentoId))
            .Should().Be(1);
    }

    [Fact]
    public async Task Post_InvalidSignature_ShouldReturnUnauthorized()
    {
        var client = _factory.CreateClient();
        var lancamentoId = Guid.NewGuid();
        var request = FacilitiesWebhookTestHelper.CreateSignedPostFromPayload(
            lancamentoId,
            signatureOverride: "sha256=deadbeef");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_InvalidSignature_ShouldReturnUnauthorizedWithoutBodyOrSecret()
    {
        var client = _factory.CreateClient();
        var lancamentoId = Guid.NewGuid();
        var request = FacilitiesWebhookTestHelper.CreateSignedPostFromPayload(
            lancamentoId,
            signatureOverride: "sha256=deadbeef");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().BeEmpty();
        body.Should().NotContain(CorePayWebApplicationFactory.TestFacilitiesWebhookSecret);
    }

    [Fact]
    public async Task Post_MissingWebhookSecret_ShouldReturnUnauthorized()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.UseSetting("Facilities:WebhookSecret", string.Empty));
        var client = factory.CreateClient();
        var lancamentoId = Guid.NewGuid();
        var request = FacilitiesWebhookTestHelper.CreateSignedPostFromPayload(lancamentoId);

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_MissingTimestamp_ShouldReturnBadRequest()
    {
        var client = _factory.CreateClient();
        var lancamentoId = Guid.NewGuid();
        var body = FacilitiesWebhookTestHelper.BuildPayload(lancamentoId);
        var request = new HttpRequestMessage(HttpMethod.Post, FacilitiesWebhookTestHelper.WebhookPath)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Facilities-Signature", "sha256=abc");
        request.Headers.Add("X-Idempotency-Key", lancamentoId.ToString());

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_ExpiredTimestamp_ShouldReturnUnauthorized()
    {
        var client = _factory.CreateClient();
        var lancamentoId = Guid.NewGuid();
        var expiredTimestamp = (DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds()).ToString();
        var request = FacilitiesWebhookTestHelper.CreateSignedPostFromPayload(
            lancamentoId,
            timestamp: expiredTimestamp);

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_FutureTimestampOutsideWindow_ShouldReturnUnauthorized()
    {
        var client = _factory.CreateClient();
        var lancamentoId = Guid.NewGuid();
        var futureTimestamp = (DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds()).ToString();
        var request = FacilitiesWebhookTestHelper.CreateSignedPostFromPayload(
            lancamentoId,
            timestamp: futureTimestamp);

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_IdempotencyKeyMismatch_ShouldReturnBadRequest()
    {
        var client = _factory.CreateClient();
        var lancamentoId = Guid.NewGuid();
        var body = FacilitiesWebhookTestHelper.BuildPayload(lancamentoId);
        var request = FacilitiesWebhookTestHelper.CreateSignedPost(body, Guid.NewGuid());

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_UnsupportedEvent_ShouldReturnUnprocessableEntity()
    {
        var client = _factory.CreateClient();
        var lancamentoId = Guid.NewGuid();
        var request = FacilitiesWebhookTestHelper.CreateSignedPostFromPayload(
            lancamentoId,
            evento: "lancamento.cancelado");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Post_InvalidDomainPayload_ShouldReturnBadRequest()
    {
        var client = _factory.CreateClient();
        var lancamentoId = Guid.NewGuid();
        var request = FacilitiesWebhookTestHelper.CreateSignedPostFromPayload(
            lancamentoId,
            tipo: "entrada",
            categoria: "folhaPagamento");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_ShouldReturnMethodNotAllowed()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync(FacilitiesWebhookTestHelper.WebhookPath);
        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task Post_CreatedEntry_ShouldAppearInCashflowReport()
    {
        var financialClient = await CreateFinancialClientAsync();
        var webhookClient = _factory.CreateClient();
        var lancamentoId = Guid.NewGuid();

        var request = FacilitiesWebhookTestHelper.CreateSignedPostFromPayload(
            lancamentoId,
            valor: 1000m,
            dataLancamento: "2026-07-15");
        (await webhookClient.SendAsync(request)).EnsureSuccessStatusCode();

        var reportResponse = await financialClient.GetAsync("/api/v1/cashflow/report?month=7&year=2026");
        reportResponse.EnsureSuccessStatusCode();

        var report = await reportResponse.Content.ReadFromJsonAsync<CashflowReportApiResponse>();
        report!.Summary.TotalEntradas.Should().BeGreaterThanOrEqualTo(1000m);
    }

    private async Task<HttpClient> CreateFinancialClientAsync()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateFinancialUserAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);
        return client;
    }

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateAdminUserAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);
        return client;
    }

    private async Task<(Guid PaymentMethodId, Guid DepartmentId)> CreatePaymentMethodAndDepartmentAsync(
        HttpClient adminClient)
    {
        var paymentMethodResponse = await adminClient.PostAsJsonAsync(
            "/api/v1/payment-methods",
            MasterDataTestHelper.CreatePaymentMethodPayload(name: $"Pix {Guid.NewGuid():N}"));
        paymentMethodResponse.EnsureSuccessStatusCode();
        var paymentMethod = await paymentMethodResponse.Content.ReadFromJsonAsync<PaymentMethodApiResponse>();

        var departmentResponse = await adminClient.PostAsJsonAsync(
            "/api/v1/departments",
            MasterDataTestHelper.CreateDepartmentPayload(name: $"Setor {Guid.NewGuid():N}"));
        departmentResponse.EnsureSuccessStatusCode();
        var department = await departmentResponse.Content.ReadFromJsonAsync<DepartmentApiResponse>();

        return (paymentMethod!.Id, department!.Id);
    }

    private sealed record FacilitiesWebhookApiResponse(Guid Id, Guid LancamentoId, bool Idempotent);

    private sealed record PaymentMethodApiResponse(Guid Id, string Name);

    private sealed record DepartmentApiResponse(Guid Id, string Name);

    private sealed record CashflowReportApiResponse(CashflowSummaryApiResponse Summary);

    private sealed record CashflowSummaryApiResponse(
        decimal TotalEntradas,
        decimal TotalSaidas,
        decimal Saldo,
        int Count);
}
