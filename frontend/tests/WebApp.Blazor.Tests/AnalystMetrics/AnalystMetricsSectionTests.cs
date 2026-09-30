using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Auth;
using WebApp.Blazor.Components.AnalystMetrics;
using WebApp.Blazor.Services;
using WebApp.Blazor.Tests.Auth;
using WebApp.Blazor.Tests.Common;

namespace WebApp.Blazor.Tests.AnalystMetrics;

public class AnalystMetricsSectionTests : BlazorComponentTestContext
{
    private readonly StubHttpMessageHandler _httpHandler;
    private static readonly Guid DepartmentId = Guid.Parse("7fa85f64-5717-4562-b3fc-2c963f66afa6");
    private static readonly Guid CollaboratorId = Guid.Parse("8fa85f64-5717-4562-b3fc-2c963f66afa7");
    private static readonly Guid ProjectId = Guid.Parse("9fa85f64-5717-4562-b3fc-2c963f66afa8");

    public AnalystMetricsSectionTests()
    {
        _httpHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        Services.AddSingleton<IAuthSessionStorage>(new InMemoryAuthSessionStorage());
        Services.AddScoped<CorePayAuthenticationStateProvider>();
        Services.AddScoped<AuthenticationStateProvider>(provider =>
            provider.GetRequiredService<CorePayAuthenticationStateProvider>());
        AuthorizationTestSetup.AddCorePayAuthorization(Services);
        Services.AddScoped<AuthorizationMessageHandler>();
        Services.AddScoped(provider =>
        {
            var handler = provider.GetRequiredService<AuthorizationMessageHandler>();
            handler.InnerHandler = _httpHandler;
            return new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000") };
        });
        Services.AddScoped<AuthService>();
        Services.AddScoped<IAnalystMetricApiService, AnalystMetricApiService>();
        Services.AddScoped<ICollaboratorApiService, CollaboratorApiService>();
        Services.AddScoped<IProjectApiService, ProjectApiService>();
    }

    [Fact]
    public async Task Manager_ShowsMetricsAndCreateAction()
    {
        ConfigureGetResponses(CreateMetricListJson());
        await AuthTestHelper.AuthenticateAsync(
            Services,
            ["Manager"],
            ReferenceRolePermissions.Map["Manager"]);

        var cut = Render<AnalystMetricsSection>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Nova métrica");
            cut.Markup.Should().Contain("Ana");
            cut.Markup.Should().Contain("120");
            cut.Markup.Should().Contain("35");
        });
    }

    [Fact]
    public async Task Director_CanReadWithoutMutationActions()
    {
        ConfigureGetResponses(CreateMetricListJson());
        await AuthTestHelper.AuthenticateAsync(
            Services,
            ["Director"],
            ReferenceRolePermissions.Map["Director"]);

        var cut = Render<AnalystMetricsSection>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Ana");
            cut.Markup.Should().NotContain("Nova métrica");
            cut.Markup.Should().NotContain(">Editar<");
        });
    }

    [Fact]
    public async Task EmptyResult_ShowsEmptyState()
    {
        ConfigureGetResponses("[]");
        await AuthTestHelper.AuthenticateAsync(
            Services,
            ["Manager"],
            ReferenceRolePermissions.Map["Manager"]);

        var cut = Render<AnalystMetricsSection>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Nenhuma métrica encontrada"));
    }

    private void ConfigureGetResponses(string metricJson)
    {
        _httpHandler.Configure(request =>
        {
            return request.RequestUri!.AbsolutePath switch
            {
                "/api/v1/collaborators" => JsonResponse(CreateCollaboratorListJson()),
                "/api/v1/projects" => JsonResponse(CreateProjectListJson()),
                "/api/v1/analyst-metrics" => JsonResponse(metricJson),
                _ => new HttpResponseMessage(HttpStatusCode.NotFound)
            };
        });
    }

    private static HttpResponseMessage JsonResponse(string content) =>
        new(HttpStatusCode.OK) { Content = new StringContent(content) };

    private static string CreateMetricListJson() =>
        JsonSerializer.Serialize(new[]
        {
            new
            {
                id = Guid.NewGuid(),
                collaboratorId = CollaboratorId,
                collaboratorName = "Ana",
                departmentId = DepartmentId,
                departmentName = "Comercial",
                projectId = ProjectId,
                projectName = "Projeto Demo",
                month = PayrollCompetenceDefaults.CurrentCompetence().Month,
                year = PayrollCompetenceDefaults.CurrentCompetence().Year,
                ftdTotal = 120,
                cpaCount = 35
            }
        });

    private static string CreateCollaboratorListJson() =>
        CollaboratorTestJson.WrapList(
        [
            new
            {
                id = CollaboratorId,
                name = "Ana",
                departmentId = DepartmentId,
                departmentName = "Comercial",
                careerLevelId = (Guid?)null,
                careerLevelName = (string?)null,
                jobTitle = "Analista",
                admissionDate = (DateOnly?)null,
                dismissalDate = (DateOnly?)null,
                pixKey = (string?)null,
                baseSalary = (decimal?)null,
                email = (string?)null,
                photoUrl = (string?)null,
                isActive = true,
                calculationProfileOverride = (string?)null
            }
        ]);

    private static string CreateProjectListJson() =>
        JsonSerializer.Serialize(new[]
        {
            new
            {
                id = ProjectId,
                name = "Projeto Demo",
                client = (string?)null,
                platform = "lastlink",
                isActive = true,
                isDefaultAllocationTarget = false,
                excludesGoalBonus = false,
                excludesSupervisorFixedAllocation = false
            }
        });
}
