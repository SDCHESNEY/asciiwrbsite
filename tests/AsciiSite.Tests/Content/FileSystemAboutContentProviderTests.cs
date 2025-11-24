using AsciiSite.Shared.Content;
using FluentAssertions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace AsciiSite.Tests.Content;

public sealed class FileSystemAboutContentProviderTests
{
    [Fact]
    public async Task GetAsync_WhenFileExists_ReturnsMarkdownHtmlAndSummary()
    {
        using var temp = new TempContentRoot();
        var markdown = "# Heading\n\nThis is the ASCII story.";
        await temp.WriteAboutAsync(markdown);

        var provider = new FileSystemAboutContentProvider(temp, NullLogger<FileSystemAboutContentProvider>.Instance);
        var result = await provider.GetAsync();

        result.Markdown.Should().Be(markdown);
        result.Html.Should().MatchRegex("<h1[^>]*>Heading</h1>");
        result.Summary.Should().Contain("Heading");
    }

    [Fact]
    public async Task GetAsync_WhenFileMissing_ReturnsEmpty()
    {
        using var temp = new TempContentRoot(createContentFolder: false);
        var provider = new FileSystemAboutContentProvider(temp, NullLogger<FileSystemAboutContentProvider>.Instance);

        var result = await provider.GetAsync();
        result.Markdown.Should().BeEmpty();
    }

    private sealed class TempContentRoot : IHostEnvironment, IDisposable
    {
        private readonly string _root;

        public TempContentRoot(bool createContentFolder = true)
        {
            _root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);

            if (createContentFolder)
            {
                Directory.CreateDirectory(Path.Combine(_root, "content"));
            }
        }

        public string EnvironmentName { get; set; } = "Development";

        public string ApplicationName { get; set; } = "Tests";

        public string ContentRootPath
        {
            get => _root;
            set => throw new NotSupportedException();
        }

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();

        public async Task WriteAboutAsync(string markdown)
        {
            var path = Path.Combine(_root, "content", "about.md");
            await File.WriteAllTextAsync(path, markdown);
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
