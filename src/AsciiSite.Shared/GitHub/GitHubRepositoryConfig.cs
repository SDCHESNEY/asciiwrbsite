namespace AsciiSite.Shared.GitHub;

/// <summary>
/// Configuration entry describing a repo to surface even when live data is unavailable.
/// </summary>
public sealed class GitHubRepositoryConfig
{
    public string Owner { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
    public string? Language { get; init; }
    public List<string> Topics { get; init; } = new();
    public string? Url { get; init; }
    public int Stars { get; init; }
    public DateTimeOffset? LastUpdated { get; init; }
}
