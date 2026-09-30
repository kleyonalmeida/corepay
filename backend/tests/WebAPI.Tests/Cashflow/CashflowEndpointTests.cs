using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using WebAPI.Tests.Common;
using WebAPI.Tests.MasterData;
using Xunit;

namespace WebAPI.Tests.Cashflow;

[Collection("WebApiIntegration")]
public class CashflowEndpointTests : IClassFixture<CorePayWebApplicationFactory>
{
    private readonly CorePayWebApplicationFactory _factory;

    public CashflowEndpointTests(CorePayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateEntradaAndSaida_ShouldCalculateSaldo700()
    {
        var client = await CreateFinancialClientAsync();
        var (paymentMethodId, departmentId) = await CreatePaymentMethodAndDepartmentAsync();
        const int month = 11;
        const int year = 2026;

        var entradaResponse = await client.PostAsJsonAsync(
            "/api/v1/cashflow",
            CashflowTestHelper.CreateEntradaPayload(amount: 1000m, month: month, year: year));
        entradaResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var saidaResponse = await client.PostAsJsonAsync(
            "/api/v1/cashflow",
            CashflowTestHelper.CreateSaidaPayload(
                paymentMethodId,
                departmentId,
                amount: 300m,
                month: month,
                year: year));
        saidaResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var listResponse = await client.GetAsync($"/api/v1/cashflow?month={month}&year={year}");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await listResponse.Content.ReadFromJsonAsync<CashflowListApiResponse>();
        list.Should().NotBeNull();
        list!.Summary.TotalEntradas.Should().Be(1000m);
        list.Summary.TotalSaidas.Should().Be(300m);
        list.Summary.Saldo.Should().Be(700m);
        list.Summary.Count.Should().Be(2);
    }

    [Fact]
    public async Task GetCashflow_ShouldFilterByMonth()
    {
        var client = await CreateFinancialClientAsync();
        const int year = 2024;

        await client.PostAsJsonAsync(
            "/api/v1/cashflow",
            CashflowTestHelper.CreateEntradaPayload(amount: 500m, month: 8, year: year));

        await client.PostAsJsonAsync(
            "/api/v1/cashflow",
            CashflowTestHelper.CreateEntradaPayload(amount: 1000m, month: 9, year: year));

        var listResponse = await client.GetAsync($"/api/v1/cashflow?month=9&year={year}");
        listResponse.EnsureSuccessStatusCode();

        var list = await listResponse.Content.ReadFromJsonAsync<CashflowListApiResponse>();
        list.Should().NotBeNull();
        list!.Summary.TotalEntradas.Should().Be(1000m);
        list.Summary.Count.Should().Be(1);
    }

    [Fact]
    public async Task CreateSaida_WithoutPaymentMethod_ShouldReturnBadRequest()
    {
        var client = await CreateFinancialClientAsync();
        var departmentId = await CreateDepartmentAsync();

        var response = await client.PostAsJsonAsync(
            "/api/v1/cashflow",
            new
            {
                type = "saida",
                category = "folhaPagamento",
                amount = 100m,
                transactionDate = "2026-09-10",
                month = 9,
                year = 2026,
                departmentId,
                installmentTotal = 1
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateWithInvalidCategoryForType_ShouldReturnBadRequest()
    {
        var client = await CreateFinancialClientAsync();

        var response = await client.PostAsJsonAsync(
            "/api/v1/cashflow",
            CashflowTestHelper.CreateEntradaPayload(category: "folhaPagamento"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateInstallmentGroup_ShouldSplitAmountAndExposeGroup()
    {
        var client = await CreateFinancialClientAsync();
        var (paymentMethodId, departmentId) = await CreatePaymentMethodAndDepartmentAsync();

        var response = await client.PostAsJsonAsync(
            "/api/v1/cashflow",
            CashflowTestHelper.CreateSaidaPayload(
                paymentMethodId,
                departmentId,
                amount: 100m,
                installmentTotal: 3));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<CashflowCreateApiResponse>();
        created.Should().NotBeNull();
        created!.PrimaryEntry.CompraId.Should().NotBeNull();
        created.CreatedEntries.Should().HaveCount(3);
        created.CreatedEntries.Sum(e => e.Amount).Should().Be(100m);
        created.CreatedEntries.Select(e => e.InstallmentNumber).Should().Equal(1, 2, 3);

        var groupResponse = await client.GetAsync(
            $"/api/v1/cashflow/installments/{created.PrimaryEntry.CompraId}");
        groupResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var group = await groupResponse.Content.ReadFromJsonAsync<List<CashflowEntryApiResponse>>();
        group.Should().NotBeNull();
        group!.Should().HaveCount(3);
    }

    [Fact]
    public async Task DeleteCashflowEntry_ShouldReturnNoContent()
    {
        var client = await CreateFinancialClientAsync();

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/cashflow",
            CashflowTestHelper.CreateEntradaPayload());
        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<CashflowCreateApiResponse>();

        var deleteResponse = await client.DeleteAsync($"/api/v1/cashflow/{created!.PrimaryEntry.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync($"/api/v1/cashflow/{created.PrimaryEntry.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task FinancialUser_CanWrite_Director_CannotWrite()
    {
        var financialClient = await CreateFinancialClientAsync();
        var directorClient = _factory.CreateClient();
        var (_, _, directorToken) = await AuthTestHelper.CreateDirectorUserAsync(_factory);
        AuthTestHelper.SetBearerToken(directorClient, directorToken);

        var financialCreate = await financialClient.PostAsJsonAsync(
            "/api/v1/cashflow",
            CashflowTestHelper.CreateEntradaPayload(amount: 50m));
        financialCreate.StatusCode.Should().Be(HttpStatusCode.Created);

        var directorCreate = await directorClient.PostAsJsonAsync(
            "/api/v1/cashflow",
            CashflowTestHelper.CreateEntradaPayload(amount: 50m));
        directorCreate.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var directorList = await directorClient.GetAsync("/api/v1/cashflow?month=9&year=2026");
        directorList.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Manager_CannotAccessCashflow()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateManagerUserAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);

        var response = await client.GetAsync("/api/v1/cashflow");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetCashflowReport_ShouldAggregateByProjectAndPaymentMethod()
    {
        var client = await CreateFinancialClientAsync();
        var adminClient = await CreateAdminClientAsync();
        var (paymentMethodId, departmentId) = await CreatePaymentMethodAndDepartmentAsync(adminClient);
        var (pixMethodId, _) = await CreatePaymentMethodAndDepartmentAsync(adminClient);
        var projectAId = await CreateProjectAsync(adminClient, "Projeto Alpha");
        var projectBId = await CreateProjectAsync(adminClient, "Projeto Beta");
        const int month = 3;
        const int year = 2026;

        await client.PostAsJsonAsync(
            "/api/v1/cashflow",
            CashflowTestHelper.CreateEntradaPayload(amount: 1000m, month: month, year: year, projectId: projectAId));
        await client.PostAsJsonAsync(
            "/api/v1/cashflow",
            CashflowTestHelper.CreateEntradaPayload(amount: 200m, month: month, year: year));
        await client.PostAsJsonAsync(
            "/api/v1/cashflow",
            CashflowTestHelper.CreateSaidaPayload(
                paymentMethodId,
                departmentId,
                amount: 300m,
                month: month,
                year: year,
                projectId: projectAId));
        await client.PostAsJsonAsync(
            "/api/v1/cashflow",
            CashflowTestHelper.CreateSaidaPayload(
                pixMethodId,
                departmentId,
                amount: 150m,
                month: month,
                year: year,
                projectId: projectBId));

        var response = await client.GetAsync($"/api/v1/cashflow/report?month={month}&year={year}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var report = await response.Content.ReadFromJsonAsync<CashflowReportApiResponse>();
        report.Should().NotBeNull();
        report!.Summary.TotalEntradas.Should().Be(1200m);
        report.Summary.TotalSaidas.Should().Be(450m);
        report.Summary.Saldo.Should().Be(750m);
        report.Summary.Count.Should().Be(4);

        report.ByProject.Should().HaveCount(3);
        report.ByProject.Should().Contain(p =>
            p.ProjectId == projectAId &&
            p.ProjectName == "Projeto Alpha" &&
            p.TotalEntradas == 1000m &&
            p.TotalSaidas == 300m &&
            p.Saldo == 700m &&
            p.Count == 2);
        report.ByProject.Should().Contain(p =>
            p.ProjectId == projectBId &&
            p.ProjectName == "Projeto Beta" &&
            p.TotalEntradas == 0m &&
            p.TotalSaidas == 150m &&
            p.Saldo == -150m &&
            p.Count == 1);
        report.ByProject.Should().Contain(p =>
            p.ProjectId == null &&
            p.ProjectName == "Sem projeto" &&
            p.TotalEntradas == 200m &&
            p.TotalSaidas == 0m &&
            p.Saldo == 200m &&
            p.Count == 1);

        report.ByPaymentMethod.Should().HaveCount(2);
        report.ByPaymentMethod.Sum(m => m.TotalSaidas).Should().Be(450m);
        report.ByPaymentMethod.Sum(m => m.Count).Should().Be(2);
    }

    [Fact]
    public async Task GetCashflowReport_ShouldFilterByMonth()
    {
        var client = await CreateFinancialClientAsync();
        const int year = 2025;

        await client.PostAsJsonAsync(
            "/api/v1/cashflow",
            CashflowTestHelper.CreateEntradaPayload(amount: 500m, month: 4, year: year));
        await client.PostAsJsonAsync(
            "/api/v1/cashflow",
            CashflowTestHelper.CreateEntradaPayload(amount: 900m, month: 5, year: year));

        var response = await client.GetAsync($"/api/v1/cashflow/report?month=5&year={year}");
        response.EnsureSuccessStatusCode();

        var report = await response.Content.ReadFromJsonAsync<CashflowReportApiResponse>();
        report.Should().NotBeNull();
        report!.Summary.TotalEntradas.Should().Be(900m);
        report.Summary.Count.Should().Be(1);
        report.ByProject.Should().ContainSingle();
    }

    [Fact]
    public async Task GetCashflowReport_InvalidMonth_ShouldReturnBadRequest()
    {
        var client = await CreateFinancialClientAsync();

        var response = await client.GetAsync("/api/v1/cashflow/report?month=13&year=2026");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetCashflowReport_Manager_ShouldReturnForbidden()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateManagerUserAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);

        var response = await client.GetAsync("/api/v1/cashflow/report?month=9&year=2026");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetCashflowReport_Director_ShouldReturnOk()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateDirectorUserAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);

        var response = await client.GetAsync("/api/v1/cashflow/report?month=9&year=2026");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeletePaymentMethod_InUse_ShouldReturnConflict()
    {
        var adminClient = await CreateAdminClientAsync();
        var financialClient = await CreateFinancialClientAsync();
        var (paymentMethodId, departmentId) = await CreatePaymentMethodAndDepartmentAsync(adminClient);

        await financialClient.PostAsJsonAsync(
            "/api/v1/cashflow",
            CashflowTestHelper.CreateSaidaPayload(paymentMethodId, departmentId));

        var deleteResponse = await adminClient.DeleteAsync($"/api/v1/payment-methods/{paymentMethodId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
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
        HttpClient? adminClient = null)
    {
        adminClient ??= await CreateAdminClientAsync();

        var paymentMethodResponse = await adminClient.PostAsJsonAsync(
            "/api/v1/payment-methods",
            MasterDataTestHelper.CreatePaymentMethodPayload(name: $"Pix {Guid.NewGuid():N}"));
        paymentMethodResponse.EnsureSuccessStatusCode();
        var paymentMethod = await paymentMethodResponse.Content.ReadFromJsonAsync<PaymentMethodApiResponse>();

        var departmentId = await CreateDepartmentAsync(adminClient);
        return (paymentMethod!.Id, departmentId);
    }

    private async Task<Guid> CreateDepartmentAsync(HttpClient? adminClient = null)
    {
        adminClient ??= await CreateAdminClientAsync();

        var response = await adminClient.PostAsJsonAsync(
            "/api/v1/departments",
            MasterDataTestHelper.CreateDepartmentPayload(name: $"Setor {Guid.NewGuid():N}"));
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<DepartmentApiResponse>();
        return created!.Id;
    }

    private static async Task<Guid> CreateProjectAsync(HttpClient adminClient, string name)
    {
        var response = await adminClient.PostAsJsonAsync(
            "/api/v1/projects",
            MasterDataTestHelper.CreateProjectPayload(name: name));
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<ProjectApiResponse>();
        return created!.Id;
    }

    private sealed record PaymentMethodApiResponse(Guid Id, string Name, bool IsActive);

    private sealed record DepartmentApiResponse(Guid Id, string Name);

    private sealed record CashflowSummaryApiResponse(
        decimal TotalEntradas,
        decimal TotalSaidas,
        decimal Saldo,
        int Count);

    private sealed record CashflowEntryApiResponse(
        Guid Id,
        string Type,
        string Category,
        decimal Amount,
        int Month,
        int Year,
        Guid? CompraId,
        int? InstallmentNumber,
        int? InstallmentTotal);

    private sealed record CashflowListApiResponse(
        CashflowSummaryApiResponse Summary,
        List<CashflowEntryApiResponse> Items);

    private sealed record CashflowCreateApiResponse(
        CashflowEntryApiResponse PrimaryEntry,
        List<CashflowEntryApiResponse> CreatedEntries);

    private sealed record CashflowReportProjectRowApiResponse(
        Guid? ProjectId,
        string ProjectName,
        decimal TotalEntradas,
        decimal TotalSaidas,
        decimal Saldo,
        int Count);

    private sealed record CashflowReportPaymentMethodRowApiResponse(
        Guid PaymentMethodId,
        string PaymentMethodName,
        decimal TotalSaidas,
        int Count);

    private sealed record CashflowReportApiResponse(
        CashflowSummaryApiResponse Summary,
        List<CashflowReportProjectRowApiResponse> ByProject,
        List<CashflowReportPaymentMethodRowApiResponse> ByPaymentMethod);

    private sealed record ProjectApiResponse(Guid Id, string Name);
}
