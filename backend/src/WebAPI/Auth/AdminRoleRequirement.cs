using Microsoft.AspNetCore.Authorization;

namespace WebAPI.Auth;

public sealed class AdminRoleRequirement : IAuthorizationRequirement;

public sealed class SuperAdminRoleRequirement : IAuthorizationRequirement;
