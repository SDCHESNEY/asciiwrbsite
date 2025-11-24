using Markdig;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AsciiSite.Shared.Content;

public sealed class FileSystemAboutContentProvider : IAboutContentProvider
{
    private readonly IHostEnvironment _environment;
    private readonly ILogger<FileSystemAboutContentProvider> _logger;
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    private static readonly char[] NewLineSeparators = ['\r', '\n'];

    public FileSystemAboutContentProvider(IHostEnvironment environment, ILogger<FileSystemAboutContentProvider> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<AboutContent> GetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var aboutPath = ResolveAboutPath();
            if (aboutPath is null)
            {
                _logger.LogWarning("About content file not found relative to {Root}", _environment.ContentRootPath);
                return AboutContent.Empty;
            }

            await using var stream = File.OpenRead(aboutPath);
            using var reader = new StreamReader(stream);
            var markdown = await reader.ReadToEndAsync(cancellationToken);
            var html = Markdown.ToHtml(markdown, _pipeline);
            var summary = CreateSummary(markdown);
            return new AboutContent(markdown, html, summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load about content");
            return AboutContent.Empty;
        }
    }

    private string? ResolveAboutPath()
    {
        var directory = new DirectoryInfo(_environment.ContentRootPath);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "content", "about.md");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static string CreateSummary(string markdown)
    {
        var paragraphs = markdown.Split(NewLineSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var firstParagraph = paragraphs.FirstOrDefault() ?? string.Empty;
        if (firstParagraph.Length <= 240)
        {
            return firstParagraph.Trim();
        }

        var truncated = firstParagraph[..240].TrimEnd();
        return string.Concat(truncated, "...");
    }
}
