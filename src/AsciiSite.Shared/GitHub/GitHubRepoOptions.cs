using System.ComponentModel.DataAnnotations;

namespace AsciiSite.Shared.GitHub;

public sealed class GitHubRepoOptions
{
    public const string SectionName = "GitHub";

    /// <summary>
    /// When true the service will attempt to call the GitHub REST API for live data.
    /// </summary>
    public bool EnableLiveUpdates { get; init; }

    /// <summary>
    /// Optional Personal Access Token or GitHub App token loaded via secrets managers.
    /// </summary>
    [DataType(DataType.Password)]
    public string? Token { get; init; }

    /// <summary>
    /// Cached lifetime in minutes for live responses.
    /// </summary>
    [Range(1, 1440)]
    public int CacheDurationMinutes { get; init; } = 15;

    /// <summary>
    /// Static fallback list of repositories to surface.
    /// </summary>
    public List<GitHubRepositoryConfig> Repositories { get; init; } = new();
}
