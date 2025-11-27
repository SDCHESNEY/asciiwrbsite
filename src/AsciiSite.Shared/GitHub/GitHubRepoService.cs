using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AsciiSite.Shared.GitHub;

public sealed class GitHubRepoService : IGitHubRepoService
{
    private const string CacheKey = "github:repos";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly IOptionsMonitor<GitHubRepoOptions> _options;
    private readonly ILogger<GitHubRepoService> _logger;

    public GitHubRepoService(
        HttpClient httpClient,
        IMemoryCache cache,
        IOptionsMonitor<GitHubRepoOptions> options,
        ILogger<GitHubRepoService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _options = options;
        _logger = logger;
    }

    public async Task<IReadOnlyList<GitHubRepo>> GetRepositoriesAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey, out IReadOnlyList<GitHubRepo>? cached) && cached is not null)
        {
            return cached;
        }

        var current = _options.CurrentValue;
        var results = new List<GitHubRepo>();

        foreach (var repoConfig in current.Repositories)
        {
            GitHubRepo? repo = null;

            if (current.EnableLiveUpdates)
            {
                repo = await TryFetchLiveAsync(repoConfig, current.Token, cancellationToken);
            }

            repo ??= MapFromConfig(repoConfig);

            if (repo is not null)
            {
                results.Add(repo);
            }
        }

        var ordered = results
            .OrderByDescending(repo => repo.Stars)
            .ThenBy(repo => repo.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var duration = TimeSpan.FromMinutes(Math.Clamp(current.CacheDurationMinutes, 1, 1440));
        _cache.Set(CacheKey, ordered, duration);
        return ordered;
    }

    private async Task<GitHubRepo?> TryFetchLiveAsync(GitHubRepositoryConfig config, string? token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.Owner) || string.IsNullOrWhiteSpace(config.Name))
        {
            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"repos/{config.Owner}/{config.Name}");
            request.Headers.Accept.ParseAdd("application/vnd.github+json");
            request.Headers.UserAgent.ParseAdd("AsciiSite/1.0");

            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("GitHub API returned {StatusCode} for {Owner}/{Repo}", response.StatusCode, config.Owner, config.Name);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var payload = await JsonSerializer.DeserializeAsync<GitHubRepoResponse>(stream, JsonOptions, cancellationToken);
            if (payload is null)
            {
                return null;
            }

            var topics = payload.Topics?.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim().ToLowerInvariant()).ToArray()
                ?? NormalizeTopics(config.Topics);

            return new GitHubRepo(
                payload.Owner.Login,
                payload.Name,
                config.DisplayName ?? payload.Name,
                payload.Description ?? config.Description ?? string.Empty,
                payload.Language ?? config.Language ?? "n/a",
                topics,
                payload.StargazersCount ?? config.Stars,
                payload.PushedAt ?? config.LastUpdated ?? DateTimeOffset.UtcNow,
                config.Url ?? payload.HtmlUrl ?? $"https://github.com/{payload.Owner.Login}/{payload.Name}",
                true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch GitHub repo {Owner}/{Repo}", config.Owner, config.Name);
            return null;
        }
    }

    private static GitHubRepo? MapFromConfig(GitHubRepositoryConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.Owner) || string.IsNullOrWhiteSpace(config.Name))
        {
            return null;
        }

        var displayName = string.IsNullOrWhiteSpace(config.DisplayName)
            ? config.Name
            : config.DisplayName;

        var language = string.IsNullOrWhiteSpace(config.Language) ? "n/a" : config.Language;
        var url = string.IsNullOrWhiteSpace(config.Url)
            ? $"https://github.com/{config.Owner}/{config.Name}"
            : config.Url;

        return new GitHubRepo(
            config.Owner,
            config.Name,
            displayName!,
            config.Description ?? string.Empty,
            language!,
            NormalizeTopics(config.Topics),
            config.Stars,
            config.LastUpdated ?? DateTimeOffset.UtcNow,
            url,
            false);
    }

    private static string[] NormalizeTopics(IEnumerable<string>? topics)
    {
        return topics is null
            ? Array.Empty<string>()
            : topics
                .Where(topic => !string.IsNullOrWhiteSpace(topic))
                .Select(topic => topic.Trim().ToLowerInvariant())
                .Distinct()
                .OrderBy(topic => topic, StringComparer.Ordinal)
                .ToArray();
    }

    private sealed class GitHubRepoResponse
    {
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public string? Language { get; init; }
        [JsonPropertyName("html_url")]
        public string? HtmlUrl { get; init; }

        [JsonPropertyName("stargazers_count")]
        public int? StargazersCount { get; init; }

        [JsonPropertyName("pushed_at")]
        public DateTimeOffset? PushedAt { get; init; }
        public GitHubOwner Owner { get; init; } = new();
        public IReadOnlyList<string>? Topics { get; init; }
    }

    private sealed class GitHubOwner
    {
        public string Login { get; init; } = string.Empty;
    }
}
