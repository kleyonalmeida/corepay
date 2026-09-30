using BuildingBlocks.Results;
using MediatR;

namespace Core.Application.Admin;

public sealed class GetRolesHandler(IAdminIdentityStore store)
    : IRequestHandler<GetRolesQuery, Result<IReadOnlyList<RoleResponse>>>
{
    public Task<Result<IReadOnlyList<RoleResponse>>> Handle(GetRolesQuery request, CancellationToken cancellationToken) =>
        store.GetRolesAsync(cancellationToken);
}

public sealed class GetRoleByIdHandler(IAdminIdentityStore store)
    : IRequestHandler<GetRoleByIdQuery, Result<RoleResponse>>
{
    public Task<Result<RoleResponse>> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken) =>
        store.GetRoleByIdAsync(request.RoleId, cancellationToken);
}

public sealed class CreateRoleHandler(IAdminIdentityStore store)
    : IRequestHandler<CreateRoleCommand, Result<RoleResponse>>
{
    public Task<Result<RoleResponse>> Handle(CreateRoleCommand request, CancellationToken cancellationToken) =>
        store.CreateRoleAsync(request.Request, cancellationToken);
}

public sealed class UpdateRoleHandler(IAdminIdentityStore store)
    : IRequestHandler<UpdateRoleCommand, Result<RoleResponse>>
{
    public Task<Result<RoleResponse>> Handle(UpdateRoleCommand request, CancellationToken cancellationToken) =>
        store.UpdateRoleAsync(request.RoleId, request.Request, cancellationToken);
}

public sealed class GetPermissionsHandler(IAdminIdentityStore store)
    : IRequestHandler<GetPermissionsQuery, Result<IReadOnlyList<PermissionResponse>>>
{
    public Task<Result<IReadOnlyList<PermissionResponse>>> Handle(GetPermissionsQuery request, CancellationToken cancellationToken) =>
        store.GetPermissionsAsync(cancellationToken);
}

public sealed class GetPermissionByIdHandler(IAdminIdentityStore store)
    : IRequestHandler<GetPermissionByIdQuery, Result<PermissionResponse>>
{
    public Task<Result<PermissionResponse>> Handle(GetPermissionByIdQuery request, CancellationToken cancellationToken) =>
        store.GetPermissionByIdAsync(request.PermissionId, cancellationToken);
}

public sealed class CreatePermissionHandler(IAdminIdentityStore store)
    : IRequestHandler<CreatePermissionCommand, Result<PermissionResponse>>
{
    public Task<Result<PermissionResponse>> Handle(CreatePermissionCommand request, CancellationToken cancellationToken) =>
        store.CreatePermissionAsync(request.Request, cancellationToken);
}

public sealed class UpdatePermissionHandler(IAdminIdentityStore store)
    : IRequestHandler<UpdatePermissionCommand, Result<PermissionResponse>>
{
    public Task<Result<PermissionResponse>> Handle(UpdatePermissionCommand request, CancellationToken cancellationToken) =>
        store.UpdatePermissionAsync(request.PermissionId, request.Request, cancellationToken);
}

public sealed class GetUsersHandler(IAdminIdentityStore store)
    : IRequestHandler<GetUsersQuery, Result<IReadOnlyList<UserResponse>>>
{
    public Task<Result<IReadOnlyList<UserResponse>>> Handle(GetUsersQuery request, CancellationToken cancellationToken) =>
        store.GetUsersAsync(cancellationToken);
}

public sealed class GetUserByIdHandler(IAdminIdentityStore store)
    : IRequestHandler<GetUserByIdQuery, Result<UserResponse>>
{
    public Task<Result<UserResponse>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken) =>
        store.GetUserByIdAsync(request.UserId, cancellationToken);
}

public sealed class CreateUserHandler(IAdminIdentityStore store)
    : IRequestHandler<CreateUserCommand, Result<UserResponse>>
{
    public Task<Result<UserResponse>> Handle(CreateUserCommand request, CancellationToken cancellationToken) =>
        store.CreateUserAsync(request.Request, request.ActingUserId, cancellationToken);
}

public sealed class UpdateUserHandler(IAdminIdentityStore store)
    : IRequestHandler<UpdateUserCommand, Result<UserResponse>>
{
    public Task<Result<UserResponse>> Handle(UpdateUserCommand request, CancellationToken cancellationToken) =>
        store.UpdateUserAsync(request.UserId, request.Request, request.ActingUserId, cancellationToken);
}

public sealed class DeleteUserHandler(IAdminIdentityStore store)
    : IRequestHandler<DeleteUserCommand, Result>
{
    public Task<Result> Handle(DeleteUserCommand request, CancellationToken cancellationToken) =>
        store.DeleteUserAsync(request.UserId, request.ActingUserId, cancellationToken);
}
