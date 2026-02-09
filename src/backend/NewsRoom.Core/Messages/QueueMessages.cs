using NewsRoom.Core.Enums;

namespace NewsRoom.Core.Messages;

public static class QueueNames
{
    public const string TtsGeneration = "tts-generation";
    public const string AvatarGeneration = "avatar-generation";
    public const string BRollGeneration = "broll-generation";
    public const string VideoComposition = "video-composition";
    public const string BroadcastStatus = "broadcast-status";
}

public class TtsGenerationMessage
{
    public string BroadcastId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public int SegmentNumber { get; set; }
    public string SectionType { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Tone { get; set; } = string.Empty;
}

public class AvatarGenerationMessage
{
    public string BroadcastId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public int SegmentNumber { get; set; }
    public string SectionType { get; set; } = string.Empty;
    public string AudioFilePath { get; set; } = string.Empty;
    public string Tone { get; set; } = string.Empty;
}

public class BRollGenerationMessage
{
    public string BroadcastId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public int SegmentNumber { get; set; }
    public int SceneIndex { get; set; }
    public VisualContentType ContentType { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Prompt { get; set; }
    public List<string>? SearchTerms { get; set; }
    public int DurationSeconds { get; set; }
}

public class VideoCompositionMessage
{
    public string BroadcastId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}

public class BroadcastStatusMessage
{
    public string BroadcastId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public BroadcastStatus Status { get; set; }
    public string? StatusMessage { get; set; }
    public int ProgressPercent { get; set; }
    public string? ErrorMessage { get; set; }
}
