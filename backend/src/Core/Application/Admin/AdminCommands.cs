using BuildingBlocks.Results;
using MediatR;

namespace Core.Application.Admin;

public sealed record CreateRoleCommand(CreateRoleRequest Request) : IRequest<Result<RoleResponse>>;

public sealed record UpdateRoleCommand(string RoleId, UpdateRoleRequest Request) : IRequest<Result<RoleResponse>>;

public sealed record CreatePermissionCommand(CreatePermissionRequest Request) : IRequest<Result<PermissionResponse>>;

public sealed record UpdatePermissionCommand(Guid PermissionId, UpdatePermissionRequest Request) : IRequest<Result<PermissionResponse>>;

public sealed record CreateUserCommand(
    CreateUserRequest Request,
    string ActingUserId) : IRequest<Result<UserResponse>>;

public sealed record UpdateUserCommand(
    string UserId,
    UpdateUserRequest Request,
    string ActingUserId) : IRequest<Result<UserResponse>>;

public sealed record DeleteUserCommand(string UserId, string ActingUserId) : IRequest<Result>;
