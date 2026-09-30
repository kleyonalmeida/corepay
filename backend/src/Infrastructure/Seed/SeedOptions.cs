namespace Infrastructure.Seed;

public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    public SuperAdminSeedOptions SuperAdmin { get; set; } = new();

    public bool LoadFixtures { get; set; }

    public bool LoadDemoData { get; set; }

    public bool LoadLegacyData { get; set; }

    public string LegacyDataDirectory { get; set; } = string.Empty;

    public string RoleUsersPassword { get; set; } = string.Empty;
}

public sealed class SuperAdminSeedOptions
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
