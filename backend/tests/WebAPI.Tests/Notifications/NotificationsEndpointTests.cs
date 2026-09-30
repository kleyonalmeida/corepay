using System.Net;
using System.Net.Http.Json;
using Core.Application.Notifications;
using FluentAssertions;
using PayrollEntity = Core.Domain.Payroll;
using Infrastructure;
using Infrastructure.Identity;
using Infrastructure.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebAPI.Tests.Common;
using WebAPI.Tests.Payroll;

namespace WebAPI.Tests.Notifications;

[Collection("WebApiIntegration")]
public class NotificationsEndpointTests : IClassFixture<CorePayWebApplicationFactory>
{
    private readonly CorePayWebApplicationFactory _factory;

    public NotificationsEndpointTests(CorePayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetNotifications_WithoutToken_ShouldReturnUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/notifications");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SubmitPayroll_ShouldCreateNotificationsVisibleToDirectorAndAdmin()
    {
        var payroll = await SubmitPayrollAsManagerAsync(month: 1, year: 2050);

        var directorClient = await CreateDirectorClientAsync();
        var directorResponse = await directorClient.GetAsync("/api/v1/notifications");
        directorResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var directorBody = await directorResponse.Content.ReadFromJsonAsync<NotificationsListResponse>();
        directorBody!.Items.Should().ContainSingle(n =>
            n.Type == NotificationTypes.PayrollSubmitted && n.PayrollId == payroll.Id);
        directorBody.UnreadCount.Should().Be(1);

        var adminClient = await CreateAdminClientAsync();
        var adminResponse = await adminClient.GetAsync("/api/v1/notifications");
        adminResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var adminBody = await adminResponse.Content.ReadFromJsonAsync<NotificationsListResponse>();
        adminBody!.Items.Should().ContainSingle(n =>
            n.Type == NotificationTypes.PayrollSubmitted && n.PayrollId == payroll.Id);
        adminBody.UnreadCount.Should().Be(1);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var count = await dbContext.Notifications.CountAsync(n =>
            n.PayrollId == payroll.Id && n.Type == NotificationTypes.PayrollSubmitted);
        count.Should().Be(2);
    }

    [Fact]
    public async Task SubmitPayroll_ManagerShouldNotSeeDirectorNotification()
    {
        var payroll = await SubmitPayrollAsManagerAsync(month: 2, year: 2050);

        var departmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts);
        var managerClient = await CreateManagerClientAsync(departmentId);
        var response = await managerClient.GetAsync("/api/v1/notifications");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<NotificationsListResponse>();
        body!.Items.Should().NotContain(n =>
            n.Type == NotificationTypes.PayrollSubmitted && n.PayrollId == payroll.Id);
        body.UnreadCount.Should().Be(0);
    }

    [Fact]
    public async Task ApprovePayroll_ShouldNotifySubmittingManager()
    {
        var (payroll, managerUserId) = await SubmitPayrollAsManagerWithUserIdAsync(month: 3, year: 2050);

        var directorClient = await CreateDirectorClientAsync();
        var approveResponse = await directorClient.PostAsync($"/api/v1/payrolls/{payroll.Id}/approve", null);
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var managerClient = await CreateManagerClientForUserAsync(managerUserId);
        var response = await managerClient.GetAsync("/api/v1/notifications");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<NotificationsListResponse>();
        body!.Items.Should().ContainSingle(n =>
            n.Type == NotificationTypes.PayrollApproved && n.PayrollId == payroll.Id);
        body.UnreadCount.Should().Be(1);
    }

    [Fact]
    public async Task RejectPayroll_ShouldNotifySubmittingManagerWithComment()
    {
        var (payroll, managerUserId) = await SubmitPayrollAsManagerWithUserIdAsync(month: 4, year: 2050);

        var directorClient = await CreateDirectorClientAsync();
        var rejectResponse = await directorClient.PostAsJsonAsync(
            $"/api/v1/payrolls/{payroll.Id}/reject",
            new { rejectionComment = "Ajustar FTD" });
        rejectResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var managerClient = await CreateManagerClientForUserAsync(managerUserId);
        var response = await managerClient.GetAsync("/api/v1/notifications");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<NotificationsListResponse>();
        body!.Items.Should().ContainSingle(n =>
            n.Type == NotificationTypes.PayrollRejected
            && n.PayrollId == payroll.Id
            && n.Message.Contains("Ajustar FTD", StringComparison.Ordinal));
        body.UnreadCount.Should().Be(1);
    }

    [Fact]
    public async Task ResubmitAfterReject_ShouldCreateNewSubmittedNotifications()
    {
        var departmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts);
        var payroll = await PayrollTestHelper.CreateDraftPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            month: 5,
            year: 2050,
            SeedKeys.Collaborators.CommercialAnalystActive);

        var managerClient = await CreateManagerClientAsync(departmentId);
        await SaveAndSubmitPayrollAsync(_factory.Services, managerClient, payroll);

        var directorClient = await CreateDirectorClientAsync();
        await directorClient.PostAsJsonAsync(
            $"/api/v1/payrolls/{payroll.Id}/reject",
            new { rejectionComment = "Corrigir valores" });

        await SaveAndSubmitPayrollAsync(_factory.Services, managerClient, payroll);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var count = await dbContext.Notifications.CountAsync(n =>
            n.PayrollId == payroll.Id && n.Type == NotificationTypes.PayrollSubmitted);
        count.Should().Be(4);
    }

    [Fact]
    public async Task RejectPayroll_WithoutComment_ShouldNotCreateNotification()
    {
        var payroll = await PayrollTestHelper.CreatePendingApprovalPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.Collaborators.CommercialAnalystActive,
            month: 6,
            year: 2050);

        var directorClient = await CreateDirectorClientAsync();
        var response = await directorClient.PostAsJsonAsync(
            $"/api/v1/payrolls/{payroll.Id}/reject",
            new { rejectionComment = "   " });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var count = await dbContext.Notifications.CountAsync(n => n.PayrollId == payroll.Id);
        count.Should().Be(0);
    }

    [Fact]
    public async Task GetNotifications_UnrelatedUser_ShouldReturnEmpty()
    {
        await SubmitPayrollAsManagerAsync(month: 7, year: 2050);

        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateUserWithoutPermissionsAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);

        var response = await client.GetAsync("/api/v1/notifications");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<NotificationsListResponse>();
        body!.Items.Should().BeEmpty();
        body.UnreadCount.Should().Be(0);
    }

    [Fact]
    public async Task MarkNotificationRead_WithoutToken_ShouldReturnUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.PutAsync(
            $"/api/v1/notifications/{Guid.NewGuid()}/read",
            null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MarkNotificationRead_InvisibleNotification_ShouldReturnNotFound()
    {
        var payroll = await SubmitPayrollAsManagerAsync(month: 8, year: 2050);

        var directorClient = await CreateDirectorClientAsync();
        var directorBody = await directorClient.GetAsync("/api/v1/notifications");
        var directorNotifications = await directorBody.Content.ReadFromJsonAsync<NotificationsListResponse>();
        var notificationId = directorNotifications!.Items
            .Single(n => n.PayrollId == payroll.Id && n.Type == NotificationTypes.PayrollSubmitted)
            .Id;

        var managerDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts);
        var managerClient = await CreateManagerClientAsync(managerDepartmentId);
        var response = await managerClient.PutAsync(
            $"/api/v1/notifications/{notificationId}/read",
            null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MarkNotificationRead_ShouldDecreaseUnreadCount()
    {
        var payroll = await SubmitPayrollAsManagerAsync(month: 9, year: 2050);

        var directorClient = await CreateDirectorClientAsync();
        var beforeResponse = await directorClient.GetAsync("/api/v1/notifications");
        var beforeBody = await beforeResponse.Content.ReadFromJsonAsync<NotificationsListResponse>();
        var notificationId = beforeBody!.Items
            .Single(n => n.PayrollId == payroll.Id && n.Type == NotificationTypes.PayrollSubmitted)
            .Id;
        var unreadBefore = beforeBody.UnreadCount;

        var markResponse = await directorClient.PutAsync(
            $"/api/v1/notifications/{notificationId}/read",
            null);
        markResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterResponse = await directorClient.GetAsync("/api/v1/notifications");
        var afterBody = await afterResponse.Content.ReadFromJsonAsync<NotificationsListResponse>();
        afterBody!.UnreadCount.Should().Be(unreadBefore - 1);
        afterBody.Items
            .Single(n => n.Id == notificationId)
            .IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task MarkNotificationRead_Idempotent_ShouldReturnNoContent()
    {
        var payroll = await SubmitPayrollAsManagerAsync(month: 10, year: 2050);

        var directorClient = await CreateDirectorClientAsync();
        var listResponse = await directorClient.GetAsync("/api/v1/notifications");
        var listBody = await listResponse.Content.ReadFromJsonAsync<NotificationsListResponse>();
        var notificationId = listBody!.Items
            .Single(n => n.PayrollId == payroll.Id && n.Type == NotificationTypes.PayrollSubmitted)
            .Id;

        var first = await directorClient.PutAsync(
            $"/api/v1/notifications/{notificationId}/read",
            null);
        first.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var second = await directorClient.PutAsync(
            $"/api/v1/notifications/{notificationId}/read",
            null);
        second.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task MarkNotificationRead_TwoDirectors_ShouldHaveIndependentReadState()
    {
        var payroll = await SubmitPayrollAsManagerAsync(month: 11, year: 2050);

        var directorClient1 = await CreateDirectorClientAsync();
        var directorClient2 = await CreateDirectorClientAsync();

        var list1 = await directorClient1.GetAsync("/api/v1/notifications");
        var body1 = await list1.Content.ReadFromJsonAsync<NotificationsListResponse>();
        var notificationId = body1!.Items
            .Single(n => n.PayrollId == payroll.Id && n.Type == NotificationTypes.PayrollSubmitted)
            .Id;
        body1.Items.Single(n => n.Id == notificationId).IsRead.Should().BeFalse();

        var mark1 = await directorClient1.PutAsync(
            $"/api/v1/notifications/{notificationId}/read",
            null);
        mark1.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var after1 = await directorClient1.GetAsync("/api/v1/notifications");
        var afterBody1 = await after1.Content.ReadFromJsonAsync<NotificationsListResponse>();
        afterBody1!.Items.Single(n => n.Id == notificationId).IsRead.Should().BeTrue();

        var list2 = await directorClient2.GetAsync("/api/v1/notifications");
        var body2 = await list2.Content.ReadFromJsonAsync<NotificationsListResponse>();
        body2!.Items.Single(n => n.Id == notificationId).IsRead.Should().BeFalse();

        var mark2 = await directorClient2.PutAsync(
            $"/api/v1/notifications/{notificationId}/read",
            null);
        mark2.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var after2 = await directorClient2.GetAsync("/api/v1/notifications");
        var afterBody2 = await after2.Content.ReadFromJsonAsync<NotificationsListResponse>();
        afterBody2!.Items.Single(n => n.Id == notificationId).IsRead.Should().BeTrue();
    }

    private async Task<PayrollEntity> SubmitPayrollAsManagerAsync(int month, int year)
    {
        var (payroll, _) = await SubmitPayrollAsManagerWithUserIdAsync(month, year);
        return payroll;
    }

    private async Task<(PayrollEntity Payroll, string ManagerUserId)> SubmitPayrollAsManagerWithUserIdAsync(
        int month,
        int year)
    {
        var departmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts);
        var payroll = await PayrollTestHelper.CreateDraftPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            month,
            year,
            SeedKeys.Collaborators.CommercialAnalystActive);

        var (email, password, _) = await AuthTestHelper.CreateManagerWithDepartmentsAsync(
            _factory,
            departmentId);
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var manager = await userManager.FindByEmailAsync(email);
        manager.Should().NotBeNull();

        var managerClient = _factory.CreateClient();
        var token = await AuthTestHelper.LoginAsync(managerClient, email, password);
        AuthTestHelper.SetBearerToken(managerClient, token);

        await SaveAndSubmitPayrollAsync(_factory.Services, managerClient, payroll);

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var submitted = await dbContext.Payrolls
            .Include(p => p.Entries)
            .SingleAsync(p => p.Id == payroll.Id);

        return (submitted, manager!.Id);
    }

    private static async Task SaveAndSubmitPayrollAsync(
        IServiceProvider services,
        HttpClient managerClient,
        PayrollEntity payroll)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seedEntities = await dbContext.SeedEntities.AsNoTracking().ToListAsync();
        var lastlinkId = seedEntities.Single(s => s.Key == SeedKeys.Projects.LastlinkSample).EntityId;

        var entryId = payroll.Entries.Single().Id;
        var collaboratorId = payroll.Entries.Single().CollaboratorId;

        var saveResponse = await managerClient.PutAsJsonAsync($"/api/v1/payrolls/{payroll.Id}", new
        {
            collaboratorIds = new[] { collaboratorId },
            entries = new[]
            {
                new
                {
                    entryId,
                    commercialProjectEntries = new[]
                    {
                        new
                        {
                            projectId = lastlinkId,
                            platform = "lastlink",
                            ftdTotal = 10,
                            ftdSuperbet = 0,
                            isFtdGoalReached = false,
                            isProjectFtdGoalReached = false,
                            cpaCount = 0,
                            salesAmount = 5_000m,
                            isSalesGoalReached = false,
                            isProjectSalesGoalReached = false,
                            rev = 0m
                        }
                    }
                }
            }
        });
        saveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var submitResponse = await managerClient.PostAsync($"/api/v1/payrolls/{payroll.Id}/submit", null);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<HttpClient> CreateManagerClientForUserAsync(string userId)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = await userManager.FindByIdAsync(userId);
        user.Should().NotBeNull();

        var client = _factory.CreateClient();
        var token = await AuthTestHelper.LoginAsync(client, user!.Email!, "TestPassword123!");
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

    private async Task<HttpClient> CreateDirectorClientAsync()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateDirectorUserAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);
        return client;
    }

    private async Task<HttpClient> CreateManagerClientAsync(Guid departmentId)
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateManagerWithDepartmentsAsync(_factory, departmentId);
        AuthTestHelper.SetBearerToken(client, token);
        return client;
    }
}
