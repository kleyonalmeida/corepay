namespace WebAPI.Configuration;

public static class EnvFileLoader
{
    public static void Load()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (string.Equals(environment, "Testing", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var repositoryRoot = FindRepositoryRoot();
        if (repositoryRoot is null)
        {
            return;
        }

        var envPath = Path.Combine(repositoryRoot, ".env");
        if (File.Exists(envPath))
        {
            LoadFile(envPath);
            return;
        }

        if (!IsLocalDevelopment(environment))
        {
            return;
        }

        var examplePath = Path.Combine(repositoryRoot, ".env.example");
        if (File.Exists(examplePath))
        {
            LoadFile(examplePath);
        }
    }

    internal static void LoadFile(string path)
    {
        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            if (key.Length == 0 || Environment.GetEnvironmentVariable(key) is not null)
            {
                continue;
            }

            var value = Unquote(line[(separatorIndex + 1)..].Trim());
            Environment.SetEnvironmentVariable(key, value);
        }
    }

    internal static string? FindRepositoryRoot()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "docker-compose.yml"))
                || File.Exists(Path.Combine(current.FullName, ".env.example")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }

    private static bool IsLocalDevelopment(string? environment) =>
        string.IsNullOrWhiteSpace(environment)
        || string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase);

    private static string Unquote(string value)
    {
        if (value.Length >= 2)
        {
            if (value.StartsWith('"') && value.EndsWith('"'))
            {
                return value[1..^1];
            }

            if (value.StartsWith('\'') && value.EndsWith('\''))
            {
                return value[1..^1];
            }
        }

        return value;
    }
}
