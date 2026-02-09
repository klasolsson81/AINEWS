using NewsRoom.Core.Models;

namespace NewsRoom.Core.Interfaces;

public interface IBroadcastOrchestrator
{
    Task<BroadcastJob> StartBroadcastAsync(
        BroadcastRequest request,
        CancellationToken cancellationToken = default);

    Task<BroadcastJob?> GetJobStatusAsync(
        string jobId,
        CancellationToken cancellationToken = default);
}
