using NewsRoom.Core.Enums;

namespace NewsRoom.Core.Models;

public class BroadcastJob
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public BroadcastStatus Status { get; set; } = BroadcastStatus.Pending;
    public string? StatusMessage { get; set; }
    public int ProgressPercent { get; set; }
    public BroadcastRequest Request { get; set; } = new();
    public BroadcastScript? Script { get; set; }
    public string? OutputVideoPath { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
}
