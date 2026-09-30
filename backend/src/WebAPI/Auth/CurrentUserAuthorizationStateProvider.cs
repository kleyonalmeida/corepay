using System.Security.Claims;
using Core.Auth;
using Infrastructure;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Auth;

public sealed class CurrentUserAuthorizationStateProvider : ICurrentUserAuthorizationStateProvider
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<AppUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CurrentUserAuthorizationStateProvider> _logger;

    public CurrentUserAuthorizationStateProvider(
        AppDbContext dbContext,
        UserManager<AppUser> userManager,
        IHttpContextAccessor httpContextAccessor,
        ILogger<CurrentUserAuthorizationStateProvider> logger)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<CurrentUserAuthorizationState?> GetAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        if (_httpContextAccessor.HttpContext?.Items[CurrentUserAuthorizationState.HttpContextItemKey]
            is CurrentUserAuthorizationState cachedState)
        {
            return cachedState;
        }

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        if (!Guid.TryParse(userId, out _))
        {
            _logger.LogWarning("Authorization rejected for principal with invalid user identifier.");
            return null;
        }

        var state = await LoadStateAsync(userId, cancellationToken);
        if (state is not null && _httpContextAccessor.HttpContext is not null)
        {
            _httpContextAccessor.HttpContext.Items[CurrentUserAuthorizationState.HttpContextItemKey] = state;
        }

        return state;
    }

    private async Task<CurrentUserAuthorizationState?> LoadStateAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            _logger.LogWarning("Authorization rejected because user {UserId} was not found.", userId);
            return null;
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            _logger.LogWarning("Authorization rejected because user {UserId} is locked out.", userId);
            return null;
        }

        var roles = (await _userManager.GetRolesAsync(user))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var permissions = await ResolvePermissionsAsync(roles, cancellationToken);

        var departmentIds = await _dbContext.UserDepartments
            .Where(ud => ud.UserId == userId)
            .Select(ud => ud.DepartmentId)
            .OrderBy(id => id)
            .ToListAsync(cancellationToken);

        var state = new CurrentUserAuthorizationState(
            user.Id,
            user.Email ?? string.Empty,
            user.DisplayName,
            roles,
            permissions,
            departmentIds);

        return state;
    }

    private async Task<IReadOnlyList<string>> ResolvePermissionsAsync(
        IReadOnlyList<string> roles,
        CancellationToken cancellationToken)
    {
        if (roles.Contains(AppRoles.SuperAdmin, StringComparer.OrdinalIgnoreCase))
        {
            return AppPermissions.All;
        }

        if (roles.Count == 0)
        {
            return [];
        }

        var roleIds = await _dbContext.Roles
            .Where(r => roles.Contains(r.Name!))
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        return await _dbContext.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => rp.Permission.Key)
            .Distinct()
            .OrderBy(key => key)
            .ToListAsync(cancellationToken);
    }
}
