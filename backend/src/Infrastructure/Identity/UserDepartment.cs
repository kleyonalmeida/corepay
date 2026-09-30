using Core.Domain;

namespace Infrastructure.Identity;

public sealed class UserDepartment
{
    public string UserId { get; set; } = string.Empty;

    public Guid DepartmentId { get; set; }

    public AppUser User { get; set; } = null!;

    public Department Department { get; set; } = null!;
}
