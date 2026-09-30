namespace Infrastructure.Identity;

public sealed class Permission
{
    public Guid Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
