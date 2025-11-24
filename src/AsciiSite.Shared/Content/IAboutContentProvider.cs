namespace AsciiSite.Shared.Content;

public interface IAboutContentProvider
{
    Task<AboutContent> GetAsync(CancellationToken cancellationToken = default);
}
