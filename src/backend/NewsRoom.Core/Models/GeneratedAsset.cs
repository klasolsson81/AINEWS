namespace NewsRoom.Core.Models;

public class GeneratedAsset
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string BroadcastId { get; set; } = string.Empty;
    public int SegmentNumber { get; set; }
    public AssetType AssetType { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public double DurationSeconds { get; set; }
    public string ContentHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum AssetType
{
    TtsAudio,
    AvatarVideo,
    BRollImage,
    BRollVideo,
    EditorialImage,
    MapGraphic,
    DataGraphic,
    ComposedVideo
}
