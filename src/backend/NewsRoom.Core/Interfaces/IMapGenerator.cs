namespace NewsRoom.Core.Interfaces;

public interface IMapGenerator
{
    Task<string> GenerateMapAsync(
        string description,
        string? sourceHint = null,
        CancellationToken cancellationToken = default);
}
