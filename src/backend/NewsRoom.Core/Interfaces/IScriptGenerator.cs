using NewsRoom.Core.Models;

namespace NewsRoom.Core.Interfaces;

public interface IScriptGenerator
{
    Task<BroadcastScript> GenerateScriptAsync(
        IReadOnlyList<NewsArticle> articles,
        CancellationToken cancellationToken = default);
}
