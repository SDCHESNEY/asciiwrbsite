using System.Net;
using System.Net.Http;
using System.Text.Json;
using AsciiSite.Shared.GitHub;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AsciiSite.Tests.GitHub;

public sealed class GitHubRepoServiceTests
{
    [Fact]
    public async Task GetRepositoriesAsync_LiveEnabled_ReturnsApiPayload()
    {
        var repoConfig = new GitHubRepositoryConfig { Owner = "octocat", Name = "Hello-World", Description = "Fallback" };
        var options = new GitHubRepoOptions
        {
            EnableLiveUpdates = true,
            Repositories = { repoConfig }
        };

        var payload = new
        {
            name = "Hello-World",
            description = "Sample description",
            language = "C#",
            stargazers_count = 42,
            pushed_at = DateTimeOffset.UtcNow,
            html_url = "https://github.com/octocat/Hello-World",
            owner = new { login = "octocat" },
            topics = new[] { "dotnet", "sample" }
        };

        var handler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload))
        });

        var service = CreateService(handler, options);

        var repos = await service.GetRepositoriesAsync();

        repos.Should().ContainSingle();
        repos[0].IsLive.Should().BeTrue();
        repos[0].Stars.Should().Be(42);
        repos[0].Topics.Should().Contain(new[] { "dotnet", "sample" });
    }

    [Fact]
    public async Task GetRepositoriesAsync_ApiFailure_FallsBackToConfig()
    {
        var repoConfig = new GitHubRepositoryConfig
        {
            Owner = "octocat",
            Name = "Hello-World",
            DisplayName = "Hello",
            Description = "Fallback description",
            Language = "C#",
            Topics = new List<string> { "fallback" },
            Stars = 5
        };

        var options = new GitHubRepoOptions
        {
            EnableLiveUpdates = true,
            Repositories = { repoConfig }
        };

        var handler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var service = CreateService(handler, options);

        var repos = await service.GetRepositoriesAsync();

        repos.Should().ContainSingle();
        repos[0].IsLive.Should().BeFalse();
        repos[0].Description.Should().Be("Fallback description");
    }

    [Fact]
    public async Task GetRepositoriesAsync_ReusesCache()
    {
        var repoConfig = new GitHubRepositoryConfig { Owner = "octocat", Name = "Hello-World" };
        var options = new GitHubRepoOptions
        {
            EnableLiveUpdates = true,
            CacheDurationMinutes = 60,
            Repositories = { repoConfig }
        };

        var handler = new CountingHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        var service = CreateService(handler, options);

        await service.GetRepositoriesAsync();
        await service.GetRepositoriesAsync();

        handler.SendCount.Should().Be(1);
    }

    private static GitHubRepoService CreateService(HttpMessageHandler handler, GitHubRepoOptions options)
    {
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.github.test/")
        };
        var cache = new MemoryCache(new MemoryCacheOptions());
        var optionsMonitor = new TestOptionsMonitor(options);
        return new GitHubRepoService(client, cache, optionsMonitor, NullLogger<GitHubRepoService>.Instance);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public StubHttpMessageHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_response);
        }
    }

    private sealed class CountingHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public CountingHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        public int SendCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            SendCount++;
            return Task.FromResult(_response);
        }
    }

    private sealed class TestOptionsMonitor : IOptionsMonitor<GitHubRepoOptions>
    {
        public TestOptionsMonitor(GitHubRepoOptions currentValue)
        {
            CurrentValue = currentValue;
        }

        public GitHubRepoOptions CurrentValue { get; private set; }

        public GitHubRepoOptions Get(string? name) => CurrentValue;

        public IDisposable OnChange(Action<GitHubRepoOptions, string?> listener) => NullDisposable.Instance;

        private sealed class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new();
            public void Dispose()
            {
            }
        }
    }
}
