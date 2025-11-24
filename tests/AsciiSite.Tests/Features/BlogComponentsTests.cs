using AsciiSite.Client.Features.Blog;
using AsciiSite.Shared.Blog;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace AsciiSite.Tests.Features;

public sealed class BlogComponentsTests
{
    [Fact]
    public void BlogIndex_RendersSummariesAndTags()
    {
        using var ctx = new BunitContext();
        var posts = new[]
        {
            new BlogPostSummary("post-one", "Post One", new DateOnly(2025, 10, 1), "Summary one", new[] { "ascii", "dotnet" }),
            new BlogPostSummary("post-two", "Post Two", new DateOnly(2025, 9, 1), "Summary two", new[] { "blog" })
        };

        ctx.Services.AddSingleton<IBlogPostProvider>(new FakeBlogPostProvider(posts));

        var cut = ctx.Render<BlogIndex>();

        cut.FindAll("[data-testid='blog-card']").Should().HaveCount(2);
        cut.Find("button.tag-chip.active").TextContent.Should().Be("All");
    }

    [Fact]
    public void BlogIndex_TagFilter_FiltersPosts()
    {
        using var ctx = new BunitContext();
        var posts = new[]
        {
            new BlogPostSummary("post-one", "Post One", new DateOnly(2025, 10, 1), "Summary one", new[] { "ascii" }),
            new BlogPostSummary("post-two", "Post Two", new DateOnly(2025, 9, 1), "Summary two", new[] { "blog" })
        };

        ctx.Services.AddSingleton<IBlogPostProvider>(new FakeBlogPostProvider(posts));

        var cut = ctx.Render<BlogIndex>();

        cut.FindAll("button.tag-chip").Single(button => button.TextContent == "blog").Click();

        cut.FindAll("[data-testid='blog-card']").Should().HaveCount(1);
        cut.Markup.Should().Contain("Post Two");
    }

    [Fact]
    public void BlogPostPage_RendersMarkdownHtml()
    {
        using var ctx = new BunitContext();
        var post = new BlogPost(
            "post-one",
            "Post One",
            new DateOnly(2025, 10, 1),
            "Summary",
            new[] { "ascii" },
            "# Heading",
            "<h1>Heading</h1>");

        ctx.Services.AddSingleton<IBlogPostProvider>(new FakeBlogPostProvider([post]));

        var cut = ctx.Render<BlogPostPage>(parameters =>
        {
            parameters.Add(p => p.Slug, "post-one");
        });

        cut.Find("[data-testid='blog-post-body']").InnerHtml.Should().Contain("<h1>");
    }

    private sealed class FakeBlogPostProvider : IBlogPostProvider
    {
        private readonly IReadOnlyList<BlogPostSummary> _summaries;
        private readonly IReadOnlyDictionary<string, BlogPost> _postsBySlug;

        public FakeBlogPostProvider(IEnumerable<BlogPostSummary> summaries)
        {
            _summaries = summaries.ToList();
            _postsBySlug = _summaries.ToDictionary(summary => summary.Slug, summary => new BlogPost(
                summary.Slug,
                summary.Title,
                summary.PublishedOn,
                summary.Summary,
                summary.Tags,
                summary.Summary,
                $"<p>{summary.Summary}</p>")
            );
        }

        public FakeBlogPostProvider(IEnumerable<BlogPost> posts)
        {
            var postList = posts.ToList();
            _summaries = postList
                .Select(post => new BlogPostSummary(post.Slug, post.Title, post.PublishedOn, post.Summary, post.Tags))
                .ToList();
            _postsBySlug = postList.ToDictionary(post => post.Slug);
        }

        public Task<IReadOnlyList<BlogPostSummary>> GetSummariesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_summaries);

        public Task<BlogPost?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            _postsBySlug.TryGetValue(slug, out var post);
            return Task.FromResult(post);
        }
    }
}
