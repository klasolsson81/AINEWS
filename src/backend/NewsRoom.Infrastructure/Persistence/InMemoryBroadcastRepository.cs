using System.Collections.Concurrent;
using NewsRoom.Core.Interfaces;
using NewsRoom.Core.Models;

namespace NewsRoom.Infrastructure.Persistence;

public class InMemoryBroadcastRepository : IBroadcastRepository
{
    private readonly ConcurrentDictionary<string, BroadcastJob> _jobs = new();

    public Task<BroadcastJob> CreateAsync(BroadcastJob job, CancellationToken cancellationToken = default)
    {
        _jobs[job.Id] = job;
        return Task.FromResult(job);
    }

    public Task<BroadcastJob?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _jobs.TryGetValue(id, out var job);
        return Task.FromResult(job);
    }

    public Task UpdateAsync(BroadcastJob job, CancellationToken cancellationToken = default)
    {
        _jobs[job.Id] = job;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<BroadcastJob>> GetRecentAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        var recent = _jobs.Values
            .OrderByDescending(j => j.CreatedAt)
            .Take(count)
            .ToList();
        return Task.FromResult<IReadOnlyList<BroadcastJob>>(recent);
    }
}
