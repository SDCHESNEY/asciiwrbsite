namespace AsciiSite.Shared.Content;

public sealed record AboutContent(string Markdown, string Html, string Summary)
{
    public static AboutContent Empty { get; } = new(string.Empty, string.Empty, string.Empty);
}
