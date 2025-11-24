using AsciiSite.Client.Features.About;
using AsciiSite.Shared.Content;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace AsciiSite.Tests.Features;

public sealed class AboutPageTests
{
    [Fact]
    public void AboutPage_RendersMarkdownFromService()
    {
        using var ctx = new BunitContext();
        var content = new AboutContent("# Heading", "<h1>Heading</h1>", "Summary");
        ctx.Services.AddSingleton<IAboutContentProvider>(new FakeAboutProvider(content));

        var cut = ctx.Render<AboutPage>();
        cut.Markup.Should().Contain("Heading");
    }

    [Fact]
    public void AboutPage_ShowsCallout_WhenMarkdownMissing()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton<IAboutContentProvider>(new FakeAboutProvider(AboutContent.Empty));

        var cut = ctx.Render<AboutPage>();
        cut.Markup.Should().Contain("content/about.md");
    }

    private sealed class FakeAboutProvider : IAboutContentProvider
    {
        private readonly AboutContent _content;

        public FakeAboutProvider(AboutContent content)
        {
            _content = content;
        }

        public Task<AboutContent> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_content);
    }
}
