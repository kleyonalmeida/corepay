using BuildingBlocks.Results;
using MediatR;

namespace Core.Application.Admin;

public sealed record GetRolesQuery : IRequest<Result<IReadOnlyList<RoleResponse>>>;

public sealed record GetRoleByIdQuery(string RoleId) : IRequest<Result<RoleResponse>>;

public sealed record GetPermissionsQuery : IRequest<Result<IReadOnlyList<PermissionResponse>>>;

public sealed record GetPermissionByIdQuery(Guid PermissionId) : IRequest<Result<PermissionResponse>>;

public sealed record GetUsersQuery : IRequest<Result<IReadOnlyList<UserResponse>>>;

public sealed record GetUserByIdQuery(string UserId) : IRequest<Result<UserResponse>>;
