using NewsRoom.Core.Enums;

namespace NewsRoom.Core.Models;

public class BroadcastScript
{
    public string BroadcastId { get; set; } = Guid.NewGuid().ToString();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string Language { get; set; } = "sv-SE";
    public int TotalSegments { get; set; }
    public int EstimatedDurationSeconds { get; set; }
    public ScriptIntro Intro { get; set; } = new();
    public List<ScriptSegment> Segments { get; set; } = new();
    public ScriptOutro Outro { get; set; } = new();
}

public class ScriptIntro
{
    public string AnchorText { get; set; } = string.Empty;
    public string Tone { get; set; } = "warm, professional, welcoming";
}

public class ScriptSegment
{
    public int SegmentNumber { get; set; }
    public NewsCategory Category { get; set; }
    public string Headline { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public SegmentPriority Priority { get; set; }
    public AnchorSection AnchorIntro { get; set; } = new();
    public VoiceoverSection BRollVoiceover { get; set; } = new();
    public VisualContent VisualContent { get; set; } = new();
    public AnchorSection AnchorOutro { get; set; } = new();
    public LowerThird LowerThird { get; set; } = new();
}

public class AnchorSection
{
    public string Text { get; set; } = string.Empty;
    public string Tone { get; set; } = string.Empty;
    public int EstimatedSeconds { get; set; }
}

public class VoiceoverSection
{
    public string Text { get; set; } = string.Empty;
    public string Tone { get; set; } = string.Empty;
    public int EstimatedSeconds { get; set; }
}

public class VisualContent
{
    public string Type { get; set; } = string.Empty;
    public VisualStrategy VisualStrategy { get; set; }
    public List<VisualScene> Scenes { get; set; } = new();
}

public class VisualScene
{
    public string Description { get; set; } = string.Empty;
    public VisualContentType Type { get; set; }
    public int DurationSeconds { get; set; } = 8;
    public string? Prompt { get; set; }
    public List<string>? SearchTerms { get; set; }
    public string? SourceHint { get; set; }
    public string? DataHint { get; set; }
}

public class LowerThird
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
}

public class ScriptOutro
{
    public string AnchorText { get; set; } = string.Empty;
    public string Tone { get; set; } = "warm, closing";
}
