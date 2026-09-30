using System.Net;
using System.Text.Json;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Auth;
using WebApp.Blazor.Components.Admin;
using WebApp.Blazor.Services;
using WebApp.Blazor.Tests.Auth;

namespace WebApp.Blazor.Tests.AdminUi;

public class UsersSectionTests : BlazorComponentTestContext
{
    private readonly StubHttpMessageHandler _httpHandler;
    private static readonly Guid DepartmentId1 = Guid.Parse("7fa85f64-5717-4562-b3fc-2c963f66afa6");
    private static readonly Guid DepartmentId2 = Guid.Parse("8fa85f64-5717-4562-b3fc-2c963f66afa7");

    public UsersSectionTests()
    {
        _httpHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        Services.AddSingleton<IAuthSessionStorage>(new InMemoryAuthSessionStorage());
        Services.AddScoped<CorePayAuthenticationStateProvider>();
        Services.AddScoped<AuthenticationStateProvider>(sp =>
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
        Services.AddScoped<IUserApiService, UserApiService>();
        Services.AddScoped<IRoleApiService, RoleApiService>();
        Services.AddScoped<IDepartmentApiService, DepartmentApiService>();
    }

    [Fact]
    public async Task UsersSection_Admin_ShowsCreateButtonAndList()
    {
        ConfigureDefaultResponses();
        await AuthenticateAsAdminAsync();

        var cut = RenderUsersSection();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Novo usuário");
            cut.Markup.Should().Contain("Gerente Regional");
            cut.Markup.Should().Contain("Manager");
        });
    }

    [Fact]
    public async Task UsersSection_EmptyList_ShowsEmptyState()
    {
        ConfigureDefaultResponses(usersJson: "[]");
        await AuthenticateAsAdminAsync();

        var cut = RenderUsersSection();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Nenhum usuário cadastrado");
        });
    }

    [Fact]
    public async Task UsersSection_ReadOnly_DoesNotShowWriteActionsOrCatalogRequests()
    {
        ConfigureDefaultResponses();
        await AuthTestHelper.AuthenticateAsync(
            Services,
            ["User"],
            [AppPermissions.UsersRead]);

        var cut = RenderUsersSection();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Gerente Regional");
            cut.Markup.Should().NotContain("Novo usuário");
            cut.Markup.Should().NotContain("Editar");
            cut.Markup.Should().NotContain("Excluir");
        });

        _httpHandler.RequestCount.Should().Be(1);
    }

    [Fact]
    public async Task UsersSection_CreateManagerWithTwoDepartments_SubmitsPayload()
    {
        string? capturedBody = null;
        _httpHandler.Configure(request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (request.Method == HttpMethod.Get && path == "/api/v1/users")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[]")
                };
            }

            if (request.Method == HttpMethod.Get && path == "/api/v1/roles")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(CreateRolesJson())
                };
            }

            if (request.Method == HttpMethod.Get && path == "/api/v1/departments")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(CreateDepartmentsJson())
                };
            }

            request.Method.Should().Be(HttpMethod.Post);
            path.Should().Be("/api/v1/users");
            capturedBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(CreateUserJson("Novo Gerente", "Manager", DepartmentId1, DepartmentId2))
            };
        });

        await AuthenticateAsAdminAsync();
        var cut = RenderUsersSection();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Novo usuário"));
        cut.FindAll("button").First(button => button.TextContent?.Contains("Novo usuário") == true).Click();

        cut.WaitForAssertion(() => cut.Find("input[placeholder='Nome de exibição']").Should().NotBeNull());

        cut.Find("input[placeholder='Nome de exibição']").Input("Novo Gerente");
        cut.Find("input[placeholder='usuario@corepay.test']").Input("manager@corepay.test");
        cut.Find("input[placeholder='Mínimo 8 caracteres']").Input("TestPassword123!");

        await cut.InvokeAsync(() =>
        {
            cut.FindAll("input[type='checkbox']")
                .First(input => input.ParentElement?.TextContent?.Contains("Manager") == true)
                .Change(true);
        });

        foreach (var departmentName in new[] { "Tipster", "Tráfego Pago" })
        {
            await cut.InvokeAsync(() =>
            {
                cut.FindAll("input[type='checkbox']")
                    .First(input => input.ParentElement?.TextContent?.Contains(departmentName) == true)
                    .Change(true);
            });
        }

        cut.FindAll("button").First(button => button.TextContent?.Contains("Salvar") == true).Click();

        cut.WaitForAssertion(() =>
        {
            capturedBody.Should().NotBeNull();
            using var document = JsonDocument.Parse(capturedBody!);
            document.RootElement.GetProperty("displayName").GetString().Should().Be("Novo Gerente");
            document.RootElement.GetProperty("roleNames").EnumerateArray().Select(x => x.GetString()).Should().Contain("Manager");
            document.RootElement.GetProperty("departmentIds").GetArrayLength().Should().Be(2);
        });
    }

    [Fact]
    public async Task UsersSection_Conflict_ShowsDuplicateMessage()
    {
        _httpHandler.Configure(request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (request.Method == HttpMethod.Get)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(path switch
                    {
                        "/api/v1/users" => "[]",
                        "/api/v1/roles" => CreateRolesJson(),
                        "/api/v1/departments" => CreateDepartmentsJson(),
                        _ => "[]"
                    })
                };
            }

            return new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent(
                    """{"error":"users.duplicate_email","message":"Duplicate email."}""")
            };
        });

        await AuthenticateAsAdminAsync();
        var cut = RenderUsersSection();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Novo usuário"));
        cut.FindAll("button").First(button => button.TextContent?.Contains("Novo usuário") == true).Click();
        cut.Find("input[placeholder='Nome de exibição']").Input("Dup User");
        cut.Find("input[placeholder='usuario@corepay.test']").Input("dup@corepay.test");
        cut.Find("input[placeholder='Mínimo 8 caracteres']").Input("TestPassword123!");
        cut.FindAll("input[type='checkbox']")
            .First(input => input.ParentElement?.TextContent?.Contains("User") == true)
            .Change(true);
        cut.FindAll("button").First(button => button.TextContent?.Contains("Salvar") == true).Click();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Já existe um usuário com este e-mail.");
        });
    }

    [Fact]
    public async Task UsersSection_Admin_DoesNotOfferSuperAdminRole()
    {
        ConfigureDefaultResponses(usersJson: "[]");
        await AuthenticateAsAdminAsync();

        var cut = RenderUsersSection();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Novo usuário"));
        cut.FindAll("button").First(button => button.TextContent?.Contains("Novo usuário") == true).Click();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Manager");
            cut.Markup.Should().NotContain("SuperAdmin");
        });
    }

    private void ConfigureDefaultResponses(string? usersJson = null)
    {
        _httpHandler.Configure(request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (request.Method != HttpMethod.Get)
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            var body = path switch
            {
                "/api/v1/users" => usersJson ?? CreateUserListJson(),
                "/api/v1/roles" => CreateRolesJson(),
                "/api/v1/departments" => CreateDepartmentsJson(),
                _ => "[]"
            };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body)
            };
        });
    }

    private IRenderedComponent<CascadingAuthenticationState> RenderUsersSection() =>
        Render<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent<UsersSection>());

    private Task AuthenticateAsAdminAsync() =>
        AuthTestHelper.AuthenticateAsync(
            Services,
            ["Admin"],
            ReferenceRolePermissions.Map["Admin"]);

    private static string CreateUserListJson() =>
        JsonSerializer.Serialize(new[]
        {
            new
            {
                id = "existing-user-id",
                email = "manager@corepay.test",
                displayName = "Gerente Regional",
                roleNames = new[] { "Manager" },
                departmentIds = new[] { DepartmentId1, DepartmentId2 }
            }
        });

    private static string CreateRolesJson() =>
        JsonSerializer.Serialize(new[]
        {
            new { id = Guid.NewGuid().ToString(), name = "Admin", permissionKeys = Array.Empty<string>() },
            new { id = Guid.NewGuid().ToString(), name = "Manager", permissionKeys = Array.Empty<string>() },
            new { id = Guid.NewGuid().ToString(), name = "SuperAdmin", permissionKeys = Array.Empty<string>() },
            new { id = Guid.NewGuid().ToString(), name = "User", permissionKeys = Array.Empty<string>() }
        });

    private static string CreateDepartmentsJson() =>
        JsonSerializer.Serialize(new[]
        {
            new
            {
                id = DepartmentId1,
                name = "Tipster",
                calculationType = "tipster",
                goalBonusPercentage = 0m,
                lowRevenueThreshold = 200000m,
                lowRevenueBonusPct = 0.4m,
                description = (string?)null,
                isActive = true,
                isAllocatedFixed = false,
                routesFixedToLimaKarttos = false
            },
            new
            {
                id = DepartmentId2,
                name = "Tráfego Pago",
                calculationType = "paidTraffic",
                goalBonusPercentage = 0m,
                lowRevenueThreshold = 200000m,
                lowRevenueBonusPct = 0.4m,
                description = (string?)null,
                isActive = true,
                isAllocatedFixed = false,
                routesFixedToLimaKarttos = false
            }
        });

    private static string CreateUserJson(
        string displayName,
        string role,
        params Guid[] departmentIds) =>
        JsonSerializer.Serialize(new
        {
            id = "new-user-id",
            email = "manager@corepay.test",
            displayName,
            roleNames = new[] { role },
            departmentIds
        });
}
