namespace NewsRoom.Core.DTOs;

public class BroadcastStatusDto
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? StatusMessage { get; set; }
    public int ProgressPercent { get; set; }
    public string? VideoUrl { get; set; }
    public string? ErrorMessage { get; set; }
}
