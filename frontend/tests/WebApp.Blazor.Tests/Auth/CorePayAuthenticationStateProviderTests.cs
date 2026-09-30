using System.Security.Claims;
using FluentAssertions;
using WebApp.Blazor.Auth;
using WebApp.Blazor.Services;

namespace WebApp.Blazor.Tests.Auth;

public class CorePayAuthenticationStateProviderTests
{
    [Fact]
    public async Task GetAuthenticationStateAsync_AfterLogin_HasRoleAndPermissionClaims()
    {
        var provider = new CorePayAuthenticationStateProvider();
        var session = new AuthSession(
            "token",
            DateTime.UtcNow.AddHours(1),
            new LoginUserResponse(
                "user-id",
                "user@test.com",
                "User Test",
                [AppRoles.SuperAdmin],
                [AppPermissions.PayrollsRead]));

        provider.SetSession(session);

        var state = await provider.GetAuthenticationStateAsync();
        var user = state.User;

        user.Identity?.IsAuthenticated.Should().BeTrue();
        user.FindFirst(ClaimTypes.NameIdentifier)?.Value.Should().Be("user-id");
        user.IsInRole(AppRoles.SuperAdmin).Should().BeTrue();
        user.Claims.Should().Contain(c => c.Type == "permission" && c.Value == AppPermissions.PayrollsRead);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_Anonymous_IsNotAuthenticated()
    {
        var provider = new CorePayAuthenticationStateProvider();

        var state = await provider.GetAuthenticationStateAsync();

        state.User.Identity?.IsAuthenticated.Should().BeFalse();
    }
}
