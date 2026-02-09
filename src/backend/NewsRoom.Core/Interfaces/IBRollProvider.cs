using NewsRoom.Core.Enums;

namespace NewsRoom.Core.Interfaces;

public interface IBRollProvider
{
    Task<BRollResult> GenerateAsync(
        VisualContentType type,
        string description,
        string? prompt = null,
        IEnumerable<string>? searchTerms = null,
        CancellationToken cancellationToken = default);
}

public class BRollResult
{
    public string FilePath { get; set; } = string.Empty;
    public double DurationSeconds { get; set; }
    public bool IsVideo { get; set; }
    public string? Attribution { get; set; }
}
