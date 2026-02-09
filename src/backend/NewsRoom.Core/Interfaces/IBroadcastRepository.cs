using NewsRoom.Core.Models;

namespace NewsRoom.Core.Interfaces;

public interface IBroadcastRepository
{
    Task<BroadcastJob> CreateAsync(BroadcastJob job, CancellationToken cancellationToken = default);
    Task<BroadcastJob?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task UpdateAsync(BroadcastJob job, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BroadcastJob>> GetRecentAsync(int count = 10, CancellationToken cancellationToken = default);
}
