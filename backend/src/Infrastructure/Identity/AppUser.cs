using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

public sealed class AppUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;

    public ICollection<UserDepartment> UserDepartments { get; set; } = [];
}
