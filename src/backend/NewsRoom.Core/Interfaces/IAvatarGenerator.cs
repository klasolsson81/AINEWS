namespace NewsRoom.Core.Interfaces;

public interface IAvatarGenerator
{
    Task<AvatarResult> GenerateAvatarVideoAsync(
        string audioFilePath,
        string tone,
        CancellationToken cancellationToken = default);
}

public class AvatarResult
{
    public string VideoFilePath { get; set; } = string.Empty;
    public double DurationSeconds { get; set; }
}
