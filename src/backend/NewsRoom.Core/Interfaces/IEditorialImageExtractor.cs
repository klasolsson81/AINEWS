namespace NewsRoom.Core.Interfaces;

public interface IEditorialImageExtractor
{
    Task<string?> ExtractImageUrlAsync(
        string articleUrl,
        CancellationToken cancellationToken = default);
}
