namespace WebApp.Blazor.Services;

public sealed class SessionIdleOptions
{
    public const string SectionName = "SessionIdle";

    public const int DefaultTimeoutMinutes = 30;

    public int TimeoutMinutes { get; init; } = DefaultTimeoutMinutes;
}
