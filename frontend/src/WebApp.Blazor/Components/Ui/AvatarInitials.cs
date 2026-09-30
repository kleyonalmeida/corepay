namespace WebApp.Blazor.Components.Ui;

internal static class AvatarInitials
{
    internal static string FromName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "?";
        }

        var parts = name
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 1)
        {
            return parts[0][..Math.Min(1, parts[0].Length)].ToUpperInvariant();
        }

        return string.Concat(parts[0][0], parts[1][0]).ToUpperInvariant();
    }
}
