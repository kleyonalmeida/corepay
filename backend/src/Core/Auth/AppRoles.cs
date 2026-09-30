namespace Core.Auth;

public static class AppRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string Director = "Director";
    public const string Financial = "Financial";
    public const string Manager = "Manager";
    public const string User = "User";

    public static readonly IReadOnlyList<string> ReferenceRoles =
    [
        SuperAdmin,
        Admin,
        Director,
        Financial,
        Manager,
        User
    ];
}
