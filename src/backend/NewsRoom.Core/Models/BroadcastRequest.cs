using NewsRoom.Core.Enums;

namespace NewsRoom.Core.Models;

public class BroadcastRequest
{
    public int TimePeriodHours { get; set; } = 24;
    public List<NewsCategory> Categories { get; set; } = new()
    {
        NewsCategory.Inrikes,
        NewsCategory.Utrikes,
        NewsCategory.Sport,
        NewsCategory.Politik
    };
    public int MaxArticles { get; set; } = 7;
}
