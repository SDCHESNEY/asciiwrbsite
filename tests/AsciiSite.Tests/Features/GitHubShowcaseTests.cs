using AsciiSite.Client.Features.GitHub;
using AsciiSite.Shared.GitHub;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace AsciiSite.Tests.Features;

public sealed class GitHubShowcaseTests
{
    [Fact]
    public async Task GitHubShowcase_RendersCardsWithMetadata()
    {
        using var ctx = new BunitContext();
        var repos = new List<GitHubRepo>
        {
            CreateRepo("asciiwrbsite", language: "C#", topics: new[] { "blazor" }, stars: 100, isLive: true),
            CreateRepo("markdig", language: "C#", topics: new[] { "markdown" }, stars: 80)
        };

        var service = Substitute.For<IGitHubRepoService>();
        service.GetRepositoriesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<GitHubRepo>>(repos));
        ctx.Services.AddSingleton(service);

        var cut = ctx.Render<GitHubShowcase>();

        var cards = cut.FindAll("article[data-testid='repo-card']");
        cards.Should().HaveCount(2);
        cards[0].TextContent.Should().Contain("asciiwrbsite");
        cards[0].TextContent.Should().Contain("live");
        cards[0].TextContent.Should().Contain("⭐ 100");
        await service.Received(1).GetRepositoriesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void GitHubShowcase_ChangingFiltersNarrowsResults()
    {
        using var ctx = new BunitContext();
        var repos = new List<GitHubRepo>
        {
            CreateRepo("asciiwrbsite", language: "C#", topics: new[] { "blazor", "ascii" }, stars: 50),
            CreateRepo("terminalfx", language: "Go", topics: new[] { "cli" }, stars: 60)
        };

        var service = Substitute.For<IGitHubRepoService>();
        service.GetRepositoriesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<GitHubRepo>>(repos));
        ctx.Services.AddSingleton(service);

        var cut = ctx.Render<GitHubShowcase>();

        cut.Find("select[data-filter='language']").Change("C#");
        var cards = cut.FindAll("article[data-testid='repo-card']");
        cards.Should().HaveCount(1);
        cards[0].TextContent.Should().Contain("asciiwrbsite");

        cut.Find("select[data-filter='language']").Change(string.Empty);
        cut.Find("select[data-filter='topic']").Change("cli");
        cut.FindAll("article[data-testid='repo-card']").Should().HaveCount(1);
        cut.Markup.Should().Contain("terminalfx");
    }

    private static GitHubRepo CreateRepo(
        string name,
        string language,
        IReadOnlyList<string> topics,
        int stars,
        bool isLive = false)
    {
        return new GitHubRepo(
            Owner: "SDCHESNEY",
            Name: name,
            DisplayName: name,
            Description: "Sample",
            Language: language,
            Topics: topics,
            Stars: stars,
            LastUpdated: DateTimeOffset.UtcNow,
            Url: $"https://github.com/SDCHESNEY/{name}",
            IsLive: isLive);
    }
}
