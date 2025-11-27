namespace AsciiSite.Shared.GitHub;

/// <summary>
/// Represents a GitHub repository showcase entry used across UI and curl experiences.
/// </summary>
/// <param name="Owner">Repository owner (organization or user).</param>
/// <param name="Name">Repository name.</param>
/// <param name="DisplayName">Optional friendly name for rendering.</param>
/// <param name="Description">Repository summary.</param>
/// <param name="Language">Primary language tag.</param>
/// <param name="Topics">Normalized topic list for filtering.</param>
/// <param name="Stars">Star count (may be estimated when offline).</param>
/// <param name="LastUpdated">Last push timestamp used for ordering.</param>
/// <param name="Url">Canonical repository URL.</param>
/// <param name="IsLive">True when sourced from the GitHub API, false when falling back to configuration.</param>
public sealed record GitHubRepo(
    string Owner,
    string Name,
    string DisplayName,
    string Description,
    string Language,
    IReadOnlyList<string> Topics,
    int Stars,
    DateTimeOffset LastUpdated,
    string Url,
    bool IsLive);
