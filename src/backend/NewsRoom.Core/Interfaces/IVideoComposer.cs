using NewsRoom.Core.Models;

namespace NewsRoom.Core.Interfaces;

public interface IVideoComposer
{
    Task<string> ComposeAsync(
        BroadcastScript script,
        CompositionAssets assets,
        CancellationToken cancellationToken = default);
}

public class CompositionAssets
{
    public string BroadcastId { get; set; } = string.Empty;
    public Dictionary<int, SegmentAssets> SegmentAssets { get; set; } = new();
    public string? IntroAudioPath { get; set; }
    public string? OutroAudioPath { get; set; }
    public string? IntroAvatarPath { get; set; }
    public string? OutroAvatarPath { get; set; }
}

public class SegmentAssets
{
    public int SegmentNumber { get; set; }
    public string? AnchorIntroAudioPath { get; set; }
    public string? AnchorIntroVideoPath { get; set; }
    public string? VoiceoverAudioPath { get; set; }
    public List<string> BRollPaths { get; set; } = new();
    public string? AnchorOutroAudioPath { get; set; }
    public string? AnchorOutroVideoPath { get; set; }
}
