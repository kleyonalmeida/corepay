using BuildingBlocks.Results;
using Core.Auth;
using Infrastructure;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Auth;

public sealed class LoginService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly AppDbContext _dbContext;
    private readonly JwtTokenService _jwtTokenService;

    public LoginService(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        AppDbContext dbContext,
        JwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<Result<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Result<LoginResponse>.Failure(
                Error.Unauthorized("auth.invalid_credentials", "Invalid email or password."));
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _userManager.FindByEmailAsync(normalizedEmail);
        if (user is null)
        {
            return Result<LoginResponse>.Failure(
                Error.Unauthorized("auth.invalid_credentials", "Invalid email or password."));
        }

        var signInResult = await _signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: true);

        if (!signInResult.Succeeded)
        {
            return Result<LoginResponse>.Failure(
                Error.Unauthorized("auth.invalid_credentials", "Invalid email or password."));
        }

        var roles = (await _userManager.GetRolesAsync(user)).ToList();
        var permissions = await ResolvePermissionsAsync(user, roles, cancellationToken);
        var (token, expiresAtUtc) = _jwtTokenService.CreateToken(user);

        return Result<LoginResponse>.Success(new LoginResponse(
            token,
            expiresAtUtc,
            new LoginUserResponse(
                user.Id,
                user.Email ?? string.Empty,
                user.DisplayName,
                roles,
                permissions)));
    }

    private async Task<IReadOnlyList<string>> ResolvePermissionsAsync(
        AppUser user,
        IReadOnlyList<string> roles,
        CancellationToken cancellationToken)
    {
        if (roles.Contains(AppRoles.SuperAdmin, StringComparer.OrdinalIgnoreCase))
        {
            return AppPermissions.All;
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
