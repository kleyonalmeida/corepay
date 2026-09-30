using System.Text.Json.Serialization;

namespace WebApp.Blazor.Services;

public enum ProjectApiStatus
{
    Success,
    ValidationError,
    NotFound,
    Conflict,
    Forbidden,
    Error
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProjectPlatform
{
    Lastlink,
    Hubla
}

public sealed record ProjectDto(
    Guid Id,
    string Name,
    string? Client,
    ProjectPlatform Platform,
    bool IsActive,
    bool IsDefaultAllocationTarget,
    bool ExcludesGoalBonus,
    bool ExcludesSupervisorFixedAllocation);

public sealed record ProjectRequest(
    string Name,
    string? Client,
    ProjectPlatform Platform,
    bool IsActive,
    bool IsDefaultAllocationTarget,
    bool ExcludesGoalBonus,
    bool ExcludesSupervisorFixedAllocation);

public sealed record ProjectListResult(
    ProjectApiStatus Status,
    IReadOnlyList<ProjectDto>? Projects = null,
    string? ErrorCode = null,
    string? Message = null);

public sealed record ProjectMutationResult(
    ProjectApiStatus Status,
    ProjectDto? Project = null,
    string? ErrorCode = null,
    string? Message = null);
