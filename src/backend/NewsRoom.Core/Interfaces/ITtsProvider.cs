namespace NewsRoom.Core.Interfaces;

public interface ITtsProvider
{
    Task<TtsResult> GenerateSpeechAsync(
        string text,
        string tone,
        CancellationToken cancellationToken = default);
}

public class TtsResult
{
    public string AudioFilePath { get; set; } = string.Empty;
    public double DurationSeconds { get; set; }
    public string ContentHash { get; set; } = string.Empty;
}
