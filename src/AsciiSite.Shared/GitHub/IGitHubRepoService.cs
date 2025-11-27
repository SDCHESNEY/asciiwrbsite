namespace AsciiSite.Shared.GitHub;

public interface IGitHubRepoService
{
    Task<IReadOnlyList<GitHubRepo>> GetRepositoriesAsync(CancellationToken cancellationToken = default);
}
