using Core.Auth;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Seed;

public sealed class IdentityDataSeeder
{
    private readonly AppDbContext _dbContext;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<AppUser> _userManager;
    private readonly SeedOptions _seedOptions;
    private readonly ILogger<IdentityDataSeeder> _logger;

    public IdentityDataSeeder(
        AppDbContext dbContext,
        RoleManager<IdentityRole> roleManager,
        UserManager<AppUser> userManager,
        IOptions<SeedOptions> seedOptions,
        ILogger<IdentityDataSeeder> logger)
    {
        _dbContext = dbContext;
        _roleManager = roleManager;
        _userManager = userManager;
        _seedOptions = seedOptions.Value;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedPermissionsAsync(cancellationToken);
        var createdRoleNames = await SeedRolesAsync(cancellationToken);
        await SeedRolePermissionsAsync(createdRoleNames, cancellationToken);
        await SeedSuperAdminAsync(cancellationToken);
    }

    private async Task SeedPermissionsAsync(CancellationToken cancellationToken)
    {
        var existingKeys = await _dbContext.Permissions
            .Select(p => p.Key)
            .ToListAsync(cancellationToken);

        var missing = AppPermissions.All
            .Where(key => !existingKeys.Contains(key))
            .Select(key => new Permission
            {
                Id = Guid.NewGuid(),
                Key = key,
                Description = key
            })
            .ToList();

        if (missing.Count == 0)
        {
            return;
        }

        _dbContext.Permissions.AddRange(missing);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlySet<string>> SeedRolesAsync(CancellationToken cancellationToken)
    {
        var createdRoleNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var roleName in AppRoles.ReferenceRoles)
        {
            if (await _roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var result = await _roleManager.CreateAsync(new IdentityRole(roleName)
            {
                Id = Guid.NewGuid().ToString()
            });
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create role '{roleName}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            createdRoleNames.Add(roleName);
        }

        return createdRoleNames;
    }

    private async Task SeedRolePermissionsAsync(
        IReadOnlySet<string> createdRoleNames,
        CancellationToken cancellationToken)
    {
        if (createdRoleNames.Count == 0)
        {
            return;
        }

        var permissionsByKey = await _dbContext.Permissions
            .ToDictionaryAsync(p => p.Key, cancellationToken);

        foreach (var (roleName, permissionKeys) in RolePermissionMap.ReferenceRolePermissions)
        {
            if (!createdRoleNames.Contains(roleName))
            {
                continue;
            }

            var role = await _roleManager.FindByNameAsync(roleName)
                ?? throw new InvalidOperationException($"Reference role '{roleName}' was not found.");

            var existingPermissionIds = await _dbContext.RolePermissions
                .Where(rp => rp.RoleId == role.Id)
                .Select(rp => rp.PermissionId)
                .ToListAsync(cancellationToken);

            var toAdd = permissionKeys
                .Select(key => permissionsByKey[key].Id)
                .Where(id => !existingPermissionIds.Contains(id))
                .Select(id => new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = id
                })
                .ToList();

            if (toAdd.Count == 0)
            {
                continue;
            }

            _dbContext.RolePermissions.AddRange(toAdd);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task SeedSuperAdminAsync(CancellationToken cancellationToken)
    {
        var email = _seedOptions.SuperAdmin.Email.Trim().ToLowerInvariant();
        var password = _seedOptions.SuperAdmin.Password;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Seed:SuperAdmin:Email and Seed:SuperAdmin:Password must be configured.");
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new AppUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = email,
                UserName = email,
                DisplayName = "Super Admin",
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create SuperAdmin user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
            }

            _logger.LogInformation("SuperAdmin user created for {Email}.", email);
        }
        else if (!string.Equals(user.UserName, email, StringComparison.OrdinalIgnoreCase))
        {
            user.UserName = email;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to normalize SuperAdmin UserName: {string.Join(", ", updateResult.Errors.Select(e => e.Description))}");
            }
        }

        if (!await _userManager.IsInRoleAsync(user, AppRoles.SuperAdmin))
        {
            var addRoleResult = await _userManager.AddToRoleAsync(user, AppRoles.SuperAdmin);
            if (!addRoleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to assign SuperAdmin role: {string.Join(", ", addRoleResult.Errors.Select(e => e.Description))}");
            }
        }
    }
}

public static class SeedOptionsConfiguration
{
    public static SeedOptions BindSeedOptions(IConfiguration configuration)
    {
        var options = new SeedOptions
        {
            SuperAdmin = new SuperAdminSeedOptions
            {
                Email = configuration["Seed:SuperAdmin:Email"] ?? string.Empty,
                Password = configuration["Seed:SuperAdmin:Password"] ?? string.Empty
            },
            LoadFixtures = configuration.GetValue<bool>("Seed:LoadFixtures"),
            LoadDemoData = configuration.GetValue<bool>("Seed:LoadDemoData"),
            RoleUsersPassword = configuration["DEV_ROLE_USERS_PASSWORD"]
                ?? configuration["Seed:RoleUsersPassword"]
                ?? string.Empty
        };

        return options;
    }
}
