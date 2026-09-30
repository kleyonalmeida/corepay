using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Auth;
using WebApp.Blazor.Components.Payroll;
using WebApp.Blazor.Services;
using WebApp.Blazor.Tests.Auth;

namespace WebApp.Blazor.Tests.Payroll;

public class PayrollFormShellTests : BlazorComponentTestContext
{
    private readonly StubHttpMessageHandler _httpHandler;

    public PayrollFormShellTests()
    {
        _httpHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        Services.AddSingleton<IAuthSessionStorage>(new InMemoryAuthSessionStorage());
        Services.AddScoped<CorePayAuthenticationStateProvider>();
        Services.AddScoped<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<CorePayAuthenticationStateProvider>());
        AuthorizationTestSetup.AddCorePayAuthorization(Services);
        Services.AddScoped<AuthorizationMessageHandler>();
        Services.AddScoped(sp =>
        {
            var handler = sp.GetRequiredService<AuthorizationMessageHandler>();
            handler.InnerHandler = _httpHandler;
            return new HttpClient(handler)
            {
                BaseAddress = new Uri("http://localhost:5000")
            };
        });
        Services.AddScoped<AuthService>();
        Services.AddScoped<IPayrollApiService, PayrollApiService>();
        Services.AddScoped<ICollaboratorApiService, CollaboratorApiService>();
    }

    [Fact]
    public async Task EditMode_WithDraftPayroll_ShouldRenderSaveActions()
    {
        var payrollId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var departmentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var entryId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var collaboratorId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        _httpHandler.Configure(request =>
        {
            if (request.RequestUri!.AbsolutePath == "/api/v1/payrolls/form-options")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new
                    {
                        departments = new[] { new { id = departmentId, name = "Analistas Comerciais" } }
                    }))
                };
            }

            if (request.RequestUri.AbsolutePath == $"/api/v1/payrolls/{payrollId}")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new
                    {
                        id = payrollId,
                        departmentId,
                        departmentName = "Analistas Comerciais",
                        month = 3,
                        year = 2026,
                        status = "draft",
                        totalAmount = 0m,
                        entryCount = 1,
                        submittedBy = (string?)null,
                        rejectionComment = (string?)null,
                        entries = new[]
                        {
                            new
                            {
                                id = entryId,
                                collaboratorId,
                                collaboratorName = "Ana Comercial",
                                careerLevelName = "Analista Comercial Júnior",
                                pixKey = "11999990001",
                                admissionDate = "2024-03-01",
                                fullBaseSalary = 3500m,
                                calculationProfile = "commercialAnalyst",
                                isApproved = false,
                                goalTier = "none",
                                finalSalary = (decimal?)null,
                                betanoInternaCount = 0,
                                betanoMundoBetCount = 0,
                                supervisorAnalystRevenue = 0m,
                                commissionPayingProjectId = (Guid?)null,
                                payload = new
                                {
                                    projectEntries = Array.Empty<object>(),
                                    rateioProjectEntries = Array.Empty<object>(),
                                    commercialProjectEntries = Array.Empty<object>(),
                                    supervisorProjectEntries = Array.Empty<object>(),
                                    trafficProjectEntries = Array.Empty<object>(),
                                    managementRevenueEntries = Array.Empty<object>(),
                                    bonusEntries = Array.Empty<object>(),
                                    deductionEntries = Array.Empty<object>(),
                                    complementPayingProjects = Array.Empty<object>(),
                                    roleChanges = Array.Empty<object>()
                                }
                            }
                        },
                        editorOptions = new
                        {
                            projects = Array.Empty<object>(),
                            departments = Array.Empty<object>(),
                            careerLevels = Array.Empty<object>()
                        }
                    }))
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        await AuthTestHelper.AuthenticateAsync(
            Services,
            ["Manager"],
            ReferenceRolePermissions.Map["Manager"]);

        var cut = Render<PayrollFormShell>(parameters => parameters
            .Add(p => p.Mode, PayrollFormMode.Edit)
            .Add(p => p.PayrollId, payrollId));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Editar folha");
            cut.Markup.Should().Contain("Salvar");
            cut.Markup.Should().Contain("Salvar e submeter");
        });
    }

    [Fact]
    public async Task CreateMode_WithSingleDepartment_PreselectsDepartment()
    {
        var departmentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        _httpHandler.Configure(request =>
        {
            if (request.RequestUri!.AbsolutePath == "/api/v1/payrolls/form-options")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new
                    {
                        departments = new[] { new { id = departmentId, name = "Analistas Comerciais" } }
                    }))
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        await AuthTestHelper.AuthenticateAsync(
            Services,
            ["Manager"],
            ReferenceRolePermissions.Map["Manager"]);

        var cut = Render<PayrollFormShell>(parameters => parameters
            .Add(p => p.Mode, PayrollFormMode.Create));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Nova folha");
            cut.Markup.Should().Contain("Analistas Comerciais");
            cut.Markup.Should().Contain("Criar folha");
            cut.FindAll("button")
                .First(button => button.TextContent?.Contains("Criar folha") == true)
                .HasAttribute("disabled")
                .Should()
                .BeTrue();
        });
    }
}
