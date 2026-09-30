using System.Security.Claims;

namespace WebAPI.Auth;

public interface ICurrentUserAuthorizationStateProvider
{
    Task<CurrentUserAuthorizationState?> GetAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);
}
