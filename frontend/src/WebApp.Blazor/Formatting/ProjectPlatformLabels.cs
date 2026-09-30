using WebApp.Blazor.Services;

namespace WebApp.Blazor.Formatting;

public static class ProjectPlatformLabels
{
    public static string GetLabel(ProjectPlatform platform) =>
        platform switch
        {
            ProjectPlatform.Lastlink => "Lastlink",
            ProjectPlatform.Hubla => "Hubla",
            _ => platform.ToString()
        };

    public static IReadOnlyList<ProjectPlatform> AllPlatforms { get; } =
        [ProjectPlatform.Lastlink, ProjectPlatform.Hubla];
}
