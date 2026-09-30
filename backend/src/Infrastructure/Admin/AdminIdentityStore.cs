using System.Text.RegularExpressions;
using BuildingBlocks.Results;
using Core.Application.Admin;
using Core.Auth;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Admin;

public sealed partial class AdminIdentityStore : IAdminIdentityStore
{
    private readonly AppDbContext _dbContext;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<AppUser> _userManager;

    public AdminIdentityStore(
        AppDbContext dbContext,
        RoleManager<IdentityRole> roleManager,
        UserManager<AppUser> userManager)
    {
        _dbContext = dbContext;
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public async Task<Result<IReadOnlyList<RoleResponse>>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _roleManager.Roles
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        var responses = new List<RoleResponse>(roles.Count);
        foreach (var role in roles)
        {
            responses.Add(await MapRoleAsync(role, cancellationToken));
        }

        return Result<IReadOnlyList<RoleResponse>>.Success(responses);
    }

    public async Task<Result<RoleResponse>> GetRoleByIdAsync(string roleId, CancellationToken cancellationToken = default)
    {
        var idValidation = GuidIdValidator.ValidateGuidString(roleId, "Role id");
        if (idValidation.IsFailure)
        {
            return Result<RoleResponse>.Failure(idValidation.Error!);
        }

        var role = await _roleManager.FindByIdAsync(roleId);
        if (role is null)
        {
            return Result<RoleResponse>.Failure(Error.NotFound("roles.not_found", "Role not found."));
        }

        return Result<RoleResponse>.Success(await MapRoleAsync(role, cancellationToken));
    }

    public async Task<Result<RoleResponse>> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var validation = ValidateRoleName(request.Name);
        if (validation.IsFailure)
        {
            return Result<RoleResponse>.Failure(validation.Error!);
        }

        var permissionValidation = await ValidatePermissionKeysAsync(request.PermissionKeys, cancellationToken);
        if (permissionValidation.IsFailure)
        {
            return Result<RoleResponse>.Failure(permissionValidation.Error!);
        }

        var permissionsRequiredValidation = ValidateDynamicRolePermissionsRequired(request.PermissionKeys);
        if (permissionsRequiredValidation.IsFailure)
        {
            return Result<RoleResponse>.Failure(permissionsRequiredValidation.Error!);
        }

        var normalizedName = request.Name.Trim();
        if (await _roleManager.RoleExistsAsync(normalizedName))
        {
            return Result<RoleResponse>.Failure(Error.Conflict("roles.duplicate", "A role with this name already exists."));
        }

        var role = new IdentityRole(normalizedName)
        {
            Id = Guid.NewGuid().ToString()
        };

        var createResult = await _roleManager.CreateAsync(role);
        if (!createResult.Succeeded)
        {
            return Result<RoleResponse>.Failure(Error.Validation(
                "roles.create_failed",
                string.Join("; ", createResult.Errors.Select(e => e.Description))));
        }

        await ReplaceRolePermissionsAsync(role.Id, request.PermissionKeys, cancellationToken);

        return Result<RoleResponse>.Success(await MapRoleAsync(role, cancellationToken));
    }

    public async Task<Result<RoleResponse>> UpdateRoleAsync(string roleId, UpdateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var idValidation = GuidIdValidator.ValidateGuidString(roleId, "Role id");
        if (idValidation.IsFailure)
        {
            return Result<RoleResponse>.Failure(idValidation.Error!);
        }

        var role = await _roleManager.FindByIdAsync(roleId);
        if (role is null)
        {
            return Result<RoleResponse>.Failure(Error.NotFound("roles.not_found", "Role not found."));
        }

        if (string.Equals(role.Name, AppRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase))
        {
            return Result<RoleResponse>.Failure(Error.Forbidden(
                "roles.superadmin_immutable",
                "The SuperAdmin role cannot be changed."));
        }

        var validation = ValidateRoleName(request.Name);
        if (validation.IsFailure)
        {
            return Result<RoleResponse>.Failure(validation.Error!);
        }

        var permissionValidation = await ValidatePermissionKeysAsync(request.PermissionKeys, cancellationToken);
        if (permissionValidation.IsFailure)
        {
            return Result<RoleResponse>.Failure(permissionValidation.Error!);
        }

        if (!IsReferenceRole(role.Name))
        {
            var permissionsRequiredValidation = ValidateDynamicRolePermissionsRequired(request.PermissionKeys);
            if (permissionsRequiredValidation.IsFailure)
            {
                return Result<RoleResponse>.Failure(permissionsRequiredValidation.Error!);
            }
        }

        var normalizedName = request.Name.Trim();
        var existingWithName = await _roleManager.FindByNameAsync(normalizedName);
        if (existingWithName is not null && existingWithName.Id != role.Id)
        {
            return Result<RoleResponse>.Failure(Error.Conflict("roles.duplicate", "A role with this name already exists."));
        }

        role.Name = normalizedName;
        var updateResult = await _roleManager.UpdateAsync(role);
        if (!updateResult.Succeeded)
        {
            return Result<RoleResponse>.Failure(Error.Validation(
                "roles.update_failed",
                string.Join("; ", updateResult.Errors.Select(e => e.Description))));
        }

        await ReplaceRolePermissionsAsync(role.Id, request.PermissionKeys, cancellationToken);

        return Result<RoleResponse>.Success(await MapRoleAsync(role, cancellationToken));
    }

    public async Task<Result<IReadOnlyList<PermissionResponse>>> GetPermissionsAsync(CancellationToken cancellationToken = default)
    {
        var permissions = await _dbContext.Permissions
            .OrderBy(p => p.Key)
            .Select(p => new PermissionResponse(p.Id, p.Key, p.Description))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<PermissionResponse>>.Success(permissions);
    }

    public async Task<Result<PermissionResponse>> GetPermissionByIdAsync(Guid permissionId, CancellationToken cancellationToken = default)
    {
        var permission = await _dbContext.Permissions
            .FirstOrDefaultAsync(p => p.Id == permissionId, cancellationToken);

        if (permission is null)
        {
            return Result<PermissionResponse>.Failure(Error.NotFound("permissions.not_found", "Permission not found."));
        }

        return Result<PermissionResponse>.Success(new PermissionResponse(permission.Id, permission.Key, permission.Description));
    }

    public async Task<Result<PermissionResponse>> CreatePermissionAsync(CreatePermissionRequest request, CancellationToken cancellationToken = default)
    {
        var validation = ValidatePermissionKey(request.Key);
        if (validation.IsFailure)
        {
            return Result<PermissionResponse>.Failure(validation.Error!);
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            return Result<PermissionResponse>.Failure(Error.Validation(
                "permissions.description_required",
                "Description is required."));
        }

        var normalizedKey = request.Key.Trim().ToLowerInvariant();
        var exists = await _dbContext.Permissions.AnyAsync(p => p.Key == normalizedKey, cancellationToken);
        if (exists)
        {
            return Result<PermissionResponse>.Failure(Error.Conflict(
                "permissions.duplicate",
                "A permission with this key already exists."));
        }

        var permission = new Permission
        {
            Id = Guid.NewGuid(),
            Key = normalizedKey,
            Description = request.Description.Trim()
        };

        _dbContext.Permissions.Add(permission);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<PermissionResponse>.Success(new PermissionResponse(permission.Id, permission.Key, permission.Description));
    }

    public async Task<Result<PermissionResponse>> UpdatePermissionAsync(Guid permissionId, UpdatePermissionRequest request, CancellationToken cancellationToken = default)
    {
        var permission = await _dbContext.Permissions
            .FirstOrDefaultAsync(p => p.Id == permissionId, cancellationToken);

        if (permission is null)
        {
            return Result<PermissionResponse>.Failure(Error.NotFound("permissions.not_found", "Permission not found."));
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            return Result<PermissionResponse>.Failure(Error.Validation(
                "permissions.description_required",
                "Description is required."));
        }

        permission.Description = request.Description.Trim();
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<PermissionResponse>.Success(new PermissionResponse(permission.Id, permission.Key, permission.Description));
    }

    public async Task<Result<IReadOnlyList<UserResponse>>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userManager.Users
            .OrderBy(u => u.Email)
            .ToListAsync(cancellationToken);

        var responses = new List<UserResponse>(users.Count);
        foreach (var user in users)
        {
            responses.Add(await MapUserAsync(user, cancellationToken));
        }

        return Result<IReadOnlyList<UserResponse>>.Success(responses);
    }

    public async Task<Result<UserResponse>> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var idValidation = GuidIdValidator.ValidateGuidString(userId, "User id");
        if (idValidation.IsFailure)
        {
            return Result<UserResponse>.Failure(idValidation.Error!);
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Result<UserResponse>.Failure(Error.NotFound("users.not_found", "User not found."));
        }

        return Result<UserResponse>.Success(await MapUserAsync(user, cancellationToken));
    }

    public async Task<Result<UserResponse>> CreateUserAsync(
        CreateUserRequest request,
        string actingUserId,
        CancellationToken cancellationToken = default)
    {
        var actingUserRoles = await LoadActingUserRolesAsync(actingUserId);
        if (actingUserRoles is null)
        {
            return Result<UserResponse>.Failure(Error.Forbidden(
                "users.acting_user_invalid",
                "The acting user is not authorized to manage users."));
        }

        var validation = await ValidateUserInputAsync(
            request.Email,
            request.DisplayName,
            request.RoleNames,
            request.DepartmentIds,
            cancellationToken);

        if (validation.IsFailure)
        {
            return Result<UserResponse>.Failure(validation.Error!);
        }

        var superAdminAssignment = ValidateSuperAdminAssignment(request.RoleNames, actingUserRoles);
        if (superAdminAssignment.IsFailure)
        {
            return Result<UserResponse>.Failure(superAdminAssignment.Error!);
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return Result<UserResponse>.Failure(Error.Validation(
                "users.password_required",
                "Password is required."));
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var existing = await _userManager.FindByEmailAsync(normalizedEmail);
        if (existing is not null)
        {
            return Result<UserResponse>.Failure(Error.Conflict(
                "users.duplicate_email",
                "A user with this email already exists."));
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = normalizedEmail,
            UserName = normalizedEmail,
            DisplayName = request.DisplayName.Trim(),
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return Result<UserResponse>.Failure(Error.Validation(
                "users.create_failed",
                string.Join("; ", createResult.Errors.Select(e => e.Description))));
        }

        var roleResult = await ReplaceUserRolesAsync(user, request.RoleNames);
        if (roleResult.IsFailure)
        {
            await _userManager.DeleteAsync(user);
            return Result<UserResponse>.Failure(roleResult.Error!);
        }

        var departmentResult = await ReplaceUserDepartmentsAsync(user.Id, request.DepartmentIds, cancellationToken);
        if (departmentResult.IsFailure)
        {
            await _userManager.DeleteAsync(user);
            return Result<UserResponse>.Failure(departmentResult.Error!);
        }

        return Result<UserResponse>.Success(await MapUserAsync(user, cancellationToken));
    }

    public async Task<Result<UserResponse>> UpdateUserAsync(
        string userId,
        UpdateUserRequest request,
        string actingUserId,
        CancellationToken cancellationToken = default)
    {
        var idValidation = GuidIdValidator.ValidateGuidString(userId, "User id");
        if (idValidation.IsFailure)
        {
            return Result<UserResponse>.Failure(idValidation.Error!);
        }

        var actingUserRoles = await LoadActingUserRolesAsync(actingUserId);
        if (actingUserRoles is null)
        {
            return Result<UserResponse>.Failure(Error.Forbidden(
                "users.acting_user_invalid",
                "The acting user is not authorized to manage users."));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Result<UserResponse>.Failure(Error.NotFound("users.not_found", "User not found."));
        }

        var validation = await ValidateUserInputAsync(
            request.Email,
            request.DisplayName,
            request.RoleNames,
            request.DepartmentIds,
            cancellationToken);

        if (validation.IsFailure)
        {
            return Result<UserResponse>.Failure(validation.Error!);
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var targetIsSuperAdmin = currentRoles.Contains(AppRoles.SuperAdmin, StringComparer.OrdinalIgnoreCase);
        var actorIsSuperAdmin = actingUserRoles.Contains(AppRoles.SuperAdmin, StringComparer.OrdinalIgnoreCase);

        if (targetIsSuperAdmin && !actorIsSuperAdmin)
        {
            return Result<UserResponse>.Failure(Error.Forbidden(
                "users.superadmin_edit_forbidden",
                "Only SuperAdmin users can modify SuperAdmin accounts."));
        }

        if (string.Equals(userId, actingUserId, StringComparison.OrdinalIgnoreCase))
        {
            var requestedRoles = request.RoleNames
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var currentRoleSet = currentRoles
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!requestedRoles.SequenceEqual(currentRoleSet, StringComparer.OrdinalIgnoreCase))
            {
                return Result<UserResponse>.Failure(Error.Forbidden(
                    "users.self_role_change_forbidden",
                    "You cannot change your own roles."));
            }
        }

        var superAdminAssignment = ValidateSuperAdminAssignment(request.RoleNames, actingUserRoles);
        if (superAdminAssignment.IsFailure)
        {
            return Result<UserResponse>.Failure(superAdminAssignment.Error!);
        }

        var isSuperAdmin = targetIsSuperAdmin;
        var removingSuperAdmin = isSuperAdmin
            && !request.RoleNames.Contains(AppRoles.SuperAdmin, StringComparer.OrdinalIgnoreCase);

        if (removingSuperAdmin)
        {
            return Result<UserResponse>.Failure(Error.Forbidden(
                "users.superadmin_role_protected",
                "The SuperAdmin role cannot be removed from a SuperAdmin user."));
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var existingWithEmail = await _userManager.FindByEmailAsync(normalizedEmail);
        if (existingWithEmail is not null && existingWithEmail.Id != user.Id)
        {
            return Result<UserResponse>.Failure(Error.Conflict(
                "users.duplicate_email",
                "A user with this email already exists."));
        }

        user.Email = normalizedEmail;
        user.UserName = normalizedEmail;
        user.DisplayName = request.DisplayName.Trim();

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return Result<UserResponse>.Failure(Error.Validation(
                "users.update_failed",
                string.Join("; ", updateResult.Errors.Select(e => e.Description))));
        }

        var roleResult = await ReplaceUserRolesAsync(user, request.RoleNames);
        if (roleResult.IsFailure)
        {
            return Result<UserResponse>.Failure(roleResult.Error!);
        }

        var departmentResult = await ReplaceUserDepartmentsAsync(user.Id, request.DepartmentIds, cancellationToken);
        if (departmentResult.IsFailure)
        {
            return Result<UserResponse>.Failure(departmentResult.Error!);
        }

        return Result<UserResponse>.Success(await MapUserAsync(user, cancellationToken));
    }

    public async Task<Result> DeleteUserAsync(string userId, string actingUserId, CancellationToken cancellationToken = default)
    {
        var idValidation = GuidIdValidator.ValidateGuidString(userId, "User id");
        if (idValidation.IsFailure)
        {
            return idValidation;
        }

        if (string.Equals(userId, actingUserId, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure(Error.Forbidden(
                "users.cannot_delete_self",
                "You cannot delete your own user account."));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Result.Failure(Error.NotFound("users.not_found", "User not found."));
        }

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains(AppRoles.SuperAdmin, StringComparer.OrdinalIgnoreCase))
        {
            return Result.Failure(Error.Forbidden(
                "users.superadmin_protected",
                "SuperAdmin users cannot be deleted."));
        }

        var deleteResult = await _userManager.DeleteAsync(user);
        if (!deleteResult.Succeeded)
        {
            return Result.Failure(Error.Validation(
                "users.delete_failed",
                string.Join("; ", deleteResult.Errors.Select(e => e.Description))));
        }

        return Result.Success();
    }

    private async Task<RoleResponse> MapRoleAsync(IdentityRole role, CancellationToken cancellationToken)
    {
        var permissionKeys = await _dbContext.RolePermissions
            .Where(rp => rp.RoleId == role.Id)
            .Select(rp => rp.Permission.Key)
            .OrderBy(key => key)
            .ToListAsync(cancellationToken);

        return new RoleResponse(role.Id, role.Name ?? string.Empty, permissionKeys);
    }

    private async Task<UserResponse> MapUserAsync(AppUser user, CancellationToken cancellationToken)
    {
        var roleNames = (await _userManager.GetRolesAsync(user))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var departmentIds = await _dbContext.UserDepartments
            .Where(ud => ud.UserId == user.Id)
            .Select(ud => ud.DepartmentId)
            .OrderBy(id => id)
            .ToListAsync(cancellationToken);

        return new UserResponse(
            user.Id,
            user.Email ?? string.Empty,
            user.DisplayName,
            roleNames,
            departmentIds);
    }

    private static Result ValidateRoleName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(Error.Validation("roles.name_required", "Role name is required."));
        }

        return Result.Success();
    }

    private static bool IsReferenceRole(string? roleName) =>
        !string.IsNullOrWhiteSpace(roleName)
        && AppRoles.ReferenceRoles.Contains(roleName, StringComparer.OrdinalIgnoreCase);

    private static Result ValidateDynamicRolePermissionsRequired(IReadOnlyList<string> permissionKeys)
    {
        if (permissionKeys is null || permissionKeys.Count == 0)
        {
            return Result.Failure(Error.Validation(
                "roles.permissions_required",
                "At least one permission is required."));
        }

        return Result.Success();
    }

    private static Result ValidatePermissionKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Result.Failure(Error.Validation("permissions.key_required", "Permission key is required."));
        }

        if (!PermissionKeyRegex().IsMatch(key.Trim()))
        {
            return Result.Failure(Error.Validation(
                "permissions.key_invalid",
                "Permission key must use lowercase letters, numbers, dots and underscores."));
        }

        return Result.Success();
    }

    private async Task<Result> ValidatePermissionKeysAsync(
        IReadOnlyList<string> permissionKeys,
        CancellationToken cancellationToken)
    {
        if (permissionKeys is null || permissionKeys.Count == 0)
        {
            return Result.Success();
        }

        var normalizedKeys = permissionKeys
            .Select(key => key.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var key in normalizedKeys)
        {
            var keyValidation = ValidatePermissionKey(key);
            if (keyValidation.IsFailure)
            {
                return keyValidation;
            }
        }

        var existingKeys = await _dbContext.Permissions
            .Where(p => normalizedKeys.Contains(p.Key))
            .Select(p => p.Key)
            .ToListAsync(cancellationToken);

        var missing = normalizedKeys.Except(existingKeys, StringComparer.OrdinalIgnoreCase).ToList();
        if (missing.Count > 0)
        {
            return Result.Failure(Error.Validation(
                "roles.permission_not_found",
                $"Unknown permission keys: {string.Join(", ", missing)}."));
        }

        return Result.Success();
    }

    private async Task ReplaceRolePermissionsAsync(
        string roleId,
        IReadOnlyList<string> permissionKeys,
        CancellationToken cancellationToken)
    {
        var existing = await _dbContext.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync(cancellationToken);

        _dbContext.RolePermissions.RemoveRange(existing);

        if (permissionKeys is null || permissionKeys.Count == 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var normalizedKeys = permissionKeys
            .Select(key => key.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var permissions = await _dbContext.Permissions
            .Where(p => normalizedKeys.Contains(p.Key))
            .ToListAsync(cancellationToken);

        foreach (var permission in permissions)
        {
            _dbContext.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permission.Id
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<string>?> LoadActingUserRolesAsync(string actingUserId)
    {
        if (string.IsNullOrWhiteSpace(actingUserId) || !Guid.TryParse(actingUserId, out _))
        {
            return null;
        }

        var actingUser = await _userManager.FindByIdAsync(actingUserId);
        if (actingUser is null || await _userManager.IsLockedOutAsync(actingUser))
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(actingUser);
        if (!roles.Contains(AppRoles.Admin, StringComparer.OrdinalIgnoreCase)
            && !roles.Contains(AppRoles.SuperAdmin, StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        return roles.ToList();
    }

    private static Result ValidateSuperAdminAssignment(
        IReadOnlyList<string> requestedRoleNames,
        IReadOnlyList<string> actingUserRoles)
    {
        var assigningSuperAdmin = requestedRoleNames.Contains(
            AppRoles.SuperAdmin,
            StringComparer.OrdinalIgnoreCase);

        if (!assigningSuperAdmin)
        {
            return Result.Success();
        }

        var actorIsSuperAdmin = actingUserRoles.Contains(
            AppRoles.SuperAdmin,
            StringComparer.OrdinalIgnoreCase);

        if (actorIsSuperAdmin)
        {
            return Result.Success();
        }

        return Result.Failure(Error.Forbidden(
            "users.superadmin_assignment_forbidden",
            "Only SuperAdmin users can assign the SuperAdmin role."));
    }

    private async Task<Result> ValidateUserInputAsync(
        string email,
        string displayName,
        IReadOnlyList<string> roleNames,
        IReadOnlyList<Guid> departmentIds,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Result.Failure(Error.Validation("users.email_required", "Email is required."));
        }

        if (!EmailRegex().IsMatch(email.Trim()))
        {
            return Result.Failure(Error.Validation("users.email_invalid", "Email is invalid."));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            return Result.Failure(Error.Validation("users.display_name_required", "Display name is required."));
        }

        if (roleNames is null || roleNames.Count == 0)
        {
            return Result.Failure(Error.Validation("users.roles_required", "At least one role is required."));
        }

        var normalizedRoles = roleNames
            .Select(name => name.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var roleName in normalizedRoles)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                return Result.Failure(Error.Validation(
                    "users.role_not_found",
                    $"Unknown role: {roleName}."));
            }
        }

        if (departmentIds is null)
        {
            return Result.Failure(Error.Validation(
                "users.departments_required",
                "Department ids are required (use an empty array when none apply)."));
        }

        if (departmentIds.Count == 0)
        {
            if (normalizedRoles.Contains(AppRoles.Manager, StringComparer.OrdinalIgnoreCase))
            {
                return Result.Failure(Error.Validation(
                    "users.manager_departments_required",
                    "Managers must be assigned to at least one department."));
            }

            return Result.Success();
        }

        var distinctIds = departmentIds.Distinct().ToList();
        var existingCount = await _dbContext.Departments
            .CountAsync(d => distinctIds.Contains(d.Id), cancellationToken);

        if (existingCount != distinctIds.Count)
        {
            return Result.Failure(Error.Validation(
                "users.department_not_found",
                "One or more department ids were not found."));
        }

        return Result.Success();
    }

    private async Task<Result> ReplaceUserRolesAsync(AppUser user, IReadOnlyList<string> roleNames)
    {
        var normalizedRoles = roleNames
            .Select(name => name.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var currentRoles = await _userManager.GetRolesAsync(user);
        var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!removeResult.Succeeded)
        {
            return Result.Failure(Error.Validation(
                "users.roles_update_failed",
                string.Join("; ", removeResult.Errors.Select(e => e.Description))));
        }

        var addResult = await _userManager.AddToRolesAsync(user, normalizedRoles);
        if (!addResult.Succeeded)
        {
            return Result.Failure(Error.Validation(
                "users.roles_update_failed",
                string.Join("; ", addResult.Errors.Select(e => e.Description))));
        }

        return Result.Success();
    }

    private async Task<Result> ReplaceUserDepartmentsAsync(
        string userId,
        IReadOnlyList<Guid> departmentIds,
        CancellationToken cancellationToken)
    {
        var existing = await _dbContext.UserDepartments
            .Where(ud => ud.UserId == userId)
            .ToListAsync(cancellationToken);

        _dbContext.UserDepartments.RemoveRange(existing);

        foreach (var departmentId in departmentIds.Distinct())
        {
            _dbContext.UserDepartments.Add(new UserDepartment
            {
                UserId = userId,
                DepartmentId = departmentId
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    [GeneratedRegex(@"^[a-z0-9._]+$", RegexOptions.CultureInvariant)]
    private static partial Regex PermissionKeyRegex();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();
}
