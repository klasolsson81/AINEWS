namespace NewsRoom.Core.Interfaces;

public interface IDataGraphicGenerator
{
    Task<string> GenerateGraphicAsync(
        string description,
        string? dataHint = null,
        CancellationToken cancellationToken = default);
}
