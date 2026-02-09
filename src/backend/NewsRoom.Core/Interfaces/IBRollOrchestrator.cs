using NewsRoom.Core.Models;

namespace NewsRoom.Core.Interfaces;

public interface IBRollOrchestrator
{
    Task<IReadOnlyList<BRollResult>> GenerateBRollForSegmentAsync(
        ScriptSegment segment,
        CancellationToken cancellationToken = default);
}
