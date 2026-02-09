using System.ComponentModel.DataAnnotations;

namespace NewsRoom.Core.DTOs;

public class BroadcastRequestDto
{
    [Range(1, 48)]
    public int TimePeriodHours { get; set; } = 24;

    public List<string> Categories { get; set; } = new() { "Inrikes", "Utrikes", "Sport" };

    [Range(3, 10)]
    public int MaxArticles { get; set; } = 7;
}
