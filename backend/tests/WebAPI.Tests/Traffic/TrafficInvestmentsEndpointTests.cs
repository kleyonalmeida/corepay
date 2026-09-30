using System.Net;
using System.Net.Http.Json;
using Core.Domain.TrafficInvestmentCalculation;
using FluentAssertions;
using WebAPI.Tests.Common;
using WebAPI.Tests.MasterData;

namespace WebAPI.Tests.Traffic;

[Collection("WebApiIntegration")]
public class TrafficInvestmentsEndpointTests : IClassFixture<CorePayWebApplicationFactory>
{
    private readonly CorePayWebApplicationFactory _factory;

    public TrafficInvestmentsEndpointTests(CorePayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateTrafficInvestment_ShouldCalculateTaxAndTotal_ForTelegram1000()
    {
        var client = await CreateAdminClientAsync();
        var projectId = await CreateProjectAsync();

        var response = await client.PostAsJsonAsync(
            "/api/v1/traffic-investments",
            TrafficInvestmentsTestHelper.CreatePayload(projectId));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<TrafficInvestmentApiResponse>();
        created.Should().NotBeNull();
        created!.MonthlyTarget.Should().Be(4000m);
        created.Weeks.Should().ContainSingle(week => week.WeekNumber == 1);
        var week = created.Weeks.Single(week => week.WeekNumber == 1);
        week.SpentAmount.Should().Be(1000m);
        week.TaxAmount.Should().Be(121.50m);
        week.TotalAmount.Should().Be(1121.50m);
        week.SuggestedNext.Should().Be(2121.50m);
        created.MonthlyTotals.TaxAmount.Should().Be(121.50m);
        created.MonthlyTotals.TotalAmount.Should().Be(1121.50m);
    }

    [Fact]
    public async Task GetTrafficInvestments_ShouldFilterByMonthAndYear()
    {
        var client = await CreateAdminClientAsync();
        var projectId = await CreateProjectAsync();

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/traffic-investments",
            TrafficInvestmentsTestHelper.CreatePayload(projectId, month: 8, year: 2026));
        createResponse.EnsureSuccessStatusCode();

        var listResponse = await client.GetAsync(
            $"/api/v1/traffic-investments?month=8&year=2026&projectId={projectId}");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await listResponse.Content.ReadFromJsonAsync<List<TrafficInvestmentListItemApiResponse>>();
        list.Should().NotBeNull();
        list!.Should().ContainSingle();
        list[0].MonthlyTotals.TaxAmount.Should().Be(121.50m);
    }

    [Fact]
    public async Task GetTrafficInvestmentById_ShouldReturnRecord()
    {
        var client = await CreateAdminClientAsync();
        var projectId = await CreateProjectAsync();

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/traffic-investments",
            TrafficInvestmentsTestHelper.CreatePayload(projectId));
        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<TrafficInvestmentApiResponse>();
        created.Should().NotBeNull();

        var getResponse = await client.GetAsync($"/api/v1/traffic-investments/{created!.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var fetched = await getResponse.Content.ReadFromJsonAsync<TrafficInvestmentApiResponse>();
        fetched.Should().NotBeNull();
        fetched!.Id.Should().Be(created.Id);
        fetched.Weeks.Single(week => week.WeekNumber == 1).TotalAmount.Should().Be(1121.50m);
    }

    [Fact]
    public async Task UpdateTrafficInvestment_ShouldRecalculateTotals()
    {
        var client = await CreateAdminClientAsync();
        var projectId = await CreateProjectAsync();

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/traffic-investments",
            TrafficInvestmentsTestHelper.CreatePayload(projectId, monthlyTarget: 2000m));
        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<TrafficInvestmentApiResponse>();
        created.Should().NotBeNull();

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/v1/traffic-investments/{created!.Id}",
            TrafficInvestmentsTestHelper.CreateUpdatePayload(
                projectId,
                monthlyTarget: 2000m,
                weeks:
                [
                    new
                    {
                        weekNumber = 1,
                        deposits = Array.Empty<object>(),
                        channelSpends = new[]
                        {
                            new { channel = TrafficMediaChannel.Telegram.ToString(), amount = 2000m }
                        }
                    }
                ]));

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await updateResponse.Content.ReadFromJsonAsync<TrafficInvestmentApiResponse>();
        updated.Should().NotBeNull();
        updated!.Weeks.Single(week => week.WeekNumber == 1).SpentAmount.Should().Be(2000m);
        updated.Weeks.Single(week => week.WeekNumber == 1).TaxAmount.Should().Be(243.00m);
    }

    [Fact]
    public async Task CreateTrafficInvestment_WithEmptyContent_ShouldReturnBadRequest()
    {
        var client = await CreateAdminClientAsync();
        var projectId = await CreateProjectAsync();

        var response = await client.PostAsJsonAsync(
            "/api/v1/traffic-investments",
            TrafficInvestmentsTestHelper.CreatePayload(
                projectId,
                monthlyTarget: 0m,
                weeks:
                [
                    new
                    {
                        weekNumber = 1,
                        deposits = Array.Empty<object>(),
                        channelSpends = Array.Empty<object>()
                    }
                ]));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.Error.Should().Be("trafficinvestments.content_required");
    }

    [Fact]
    public async Task CreateTrafficInvestment_WithDuplicateCompetence_ShouldReturnConflict()
    {
        var client = await CreateAdminClientAsync();
        var projectId = await CreateProjectAsync();

        var payload = TrafficInvestmentsTestHelper.CreatePayload(projectId);
        var first = await client.PostAsJsonAsync("/api/v1/traffic-investments", payload);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var duplicate = await client.PostAsJsonAsync("/api/v1/traffic-investments", payload);
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateTrafficInvestment_WithUnknownProject_ShouldReturnBadRequest()
    {
        var client = await CreateAdminClientAsync();

        var response = await client.PostAsJsonAsync(
            "/api/v1/traffic-investments",
            TrafficInvestmentsTestHelper.CreatePayload(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTrafficInvestment_WithNegativeSpend_ShouldReturnBadRequest()
    {
        var client = await CreateAdminClientAsync();
        var projectId = await CreateProjectAsync();

        var response = await client.PostAsJsonAsync(
            "/api/v1/traffic-investments",
            TrafficInvestmentsTestHelper.CreatePayload(projectId, weeks:
            [
                new
                {
                    weekNumber = 1,
                    deposits = Array.Empty<object>(),
                    channelSpends = new[] { new { channel = TrafficMediaChannel.Telegram.ToString(), amount = -1m } }
                }
            ]));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTrafficInvestments_WithInvalidMonth_ShouldReturnBadRequest()
    {
        var client = await CreateAdminClientAsync();

        var response = await client.GetAsync("/api/v1/traffic-investments?month=13");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTrafficInvestments_WithoutToken_ShouldReturnUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/traffic-investments");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTrafficInvestments_WithManagerToken_ShouldReturnForbidden()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateManagerUserAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);

        var response = await client.GetAsync("/api/v1/traffic-investments");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateTrafficInvestment_WithDirectorToken_ShouldReturnForbidden()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateDirectorUserAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);

        var response = await client.PostAsJsonAsync(
            "/api/v1/traffic-investments",
            TrafficInvestmentsTestHelper.CreatePayload(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetTrafficInvestments_WithDirectorToken_ShouldReturnOk()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateDirectorUserAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);

        var response = await client.GetAsync("/api/v1/traffic-investments");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateAdminUserAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);
        return client;
    }

    private async Task<Guid> CreateProjectAsync()
    {
        var adminClient = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateAdminUserAsync(_factory);
        AuthTestHelper.SetBearerToken(adminClient, token);

        var payload = MasterDataTestHelper.CreateProjectPayload(name: $"Projeto {Guid.NewGuid():N}");
        var response = await adminClient.PostAsJsonAsync("/api/v1/projects", payload);
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<ProjectApiResponse>();
        return created!.Id;
    }

    private sealed record ProjectApiResponse(Guid Id, string Name);

    private sealed record TrafficInvestmentListItemApiResponse(
        Guid Id,
        Guid ProjectId,
        string ProjectName,
        int Month,
        int Year,
        decimal MonthlyTarget,
        TrafficInvestmentMonthlyTotalsApiResponse MonthlyTotals);

    private sealed record TrafficInvestmentApiResponse(
        Guid Id,
        Guid ProjectId,
        string ProjectName,
        int Month,
        int Year,
        decimal MonthlyTarget,
        IReadOnlyList<TrafficWeekApiResponse> Weeks,
        TrafficInvestmentMonthlyTotalsApiResponse MonthlyTotals);

    private sealed record TrafficInvestmentMonthlyTotalsApiResponse(
        decimal RequestedAmount,
        decimal DepositedAmount,
        decimal SpentAmount,
        decimal TaxAmount,
        decimal TotalAmount,
        decimal Balance);

    private sealed record TrafficWeekApiResponse(
        int WeekNumber,
        decimal RequestedAmount,
        decimal DepositedAmount,
        decimal SpentAmount,
        decimal TaxAmount,
        decimal TotalAmount,
        decimal Balance,
        decimal SuggestedNext);

    private sealed record ApiErrorResponse(string? Error, string? Message);
}
