using AsciiSite.Shared.Blog;
using FluentAssertions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace AsciiSite.Tests.Content;

public sealed class FileSystemBlogPostProviderTests
{
    [Fact]
    public async Task GetSummariesAsync_WhenPostsValid_ReturnsOrderedSummaries()
    {
        using var temp = new TempContentRoot();
        await temp.WriteBlogAsync("second-post.md", """
---
title: Second Post
slug: second-post
date: 2025-11-01
published: 2025-11-01
summary: Second summary
tags:
  - dotnet
  - blog
---
Second body paragraph.
""");

        await temp.WriteBlogAsync("first-post.md", """
---
title: First Post
published: 2025-10-15
summary: First summary
tags:
  - blog
---
First body paragraph.
""");

        var provider = new FileSystemBlogPostProvider(temp, NullLogger<FileSystemBlogPostProvider>.Instance);

        var summaries = await provider.GetSummariesAsync();

        summaries.Should().HaveCount(2);
        summaries[0].Slug.Should().Be("second-post");
        summaries[0].Tags.Should().Contain(new[] { "blog", "dotnet" });
        summaries[1].Slug.Should().Be("first-post");
    }

    [Fact]
    public async Task GetBySlugAsync_WhenSlugExists_ReturnsFullPost()
    {
        using var temp = new TempContentRoot();
        await temp.WriteBlogAsync("hello-world.md", """
---
title: Hello World
published: 2025-01-01
summary: Intro post
---
# Heading

Body copy here.
""");

        var provider = new FileSystemBlogPostProvider(temp, NullLogger<FileSystemBlogPostProvider>.Instance);

        var post = await provider.GetBySlugAsync("hello-world");

        post.Should().NotBeNull();
        post!.Html.Should().Contain("<h1");
        post.Summary.Should().Be("Intro post");
    }

    [Fact]
    public async Task GetSummariesAsync_WhenMetadataInvalid_SkipsPosts()
    {
        using var temp = new TempContentRoot();
        await temp.WriteBlogAsync("invalid.md", """
---
title:
published:
---
Body
""");

        var provider = new FileSystemBlogPostProvider(temp, NullLogger<FileSystemBlogPostProvider>.Instance);

        var summaries = await provider.GetSummariesAsync();

        summaries.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSummariesAsync_DuplicateSlugs_SkipsDuplicates()
    {
        using var temp = new TempContentRoot();
        await temp.WriteBlogAsync("one.md", """
---
title: One
slug: duplicate
published: 2025-01-05
---
Body
""");

        await temp.WriteBlogAsync("two.md", """
---
title: Two
slug: duplicate
published: 2025-02-05
---
Body
""");

        var provider = new FileSystemBlogPostProvider(temp, NullLogger<FileSystemBlogPostProvider>.Instance);

        var summaries = await provider.GetSummariesAsync();

        summaries.Should().HaveCount(1);
        summaries[0].Slug.Should().Be("duplicate");
        summaries[0].Title.Should().Be("One");
    }

    private sealed class TempContentRoot : IHostEnvironment, IDisposable
    {
        private readonly string _root;

        public TempContentRoot()
        {
            _root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(_root, "content", "blog"));
        }

        public string EnvironmentName { get; set; } = "Development";

        public string ApplicationName { get; set; } = "Tests";

        public string ContentRootPath
        {
            get => _root;
            set => throw new NotSupportedException();
        }

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();

        public Task WriteBlogAsync(string fileName, string contents)
        {
            var path = Path.Combine(_root, "content", "blog", fileName);
            return File.WriteAllTextAsync(path, contents);
        }

        public void Dispose()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }
    }
}
