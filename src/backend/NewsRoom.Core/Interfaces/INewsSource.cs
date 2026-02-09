using NewsRoom.Core.Enums;
using NewsRoom.Core.Models;

namespace NewsRoom.Core.Interfaces;

public interface INewsSource
{
    Task<IReadOnlyList<NewsArticle>> FetchArticlesAsync(
        int timePeriodHours,
        IEnumerable<NewsCategory> categories,
        int maxArticles,
        CancellationToken cancellationToken = default);
}
