using Microsoft.Extensions.Logging;
using NewsRoom.Core.Interfaces;
using NewsRoom.Core.Models;

namespace NewsRoom.Infrastructure.Mocks;

public class MockBRollOrchestrator : IBRollOrchestrator
{
    private readonly IBRollProvider _provider;
    private readonly ILogger<MockBRollOrchestrator> _logger;

    public MockBRollOrchestrator(IBRollProvider provider, ILogger<MockBRollOrchestrator> logger)
    {
        _provider = provider;
        _logger = logger;
    }

    public async Task<IReadOnlyList<BRollResult>> GenerateBRollForSegmentAsync(
        ScriptSegment segment,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MockBRollOrchestrator: Generating B-roll for segment {Number}: {Headline}",
            segment.SegmentNumber, segment.Headline);

        var results = new List<BRollResult>();
        foreach (var scene in segment.VisualContent.Scenes)
        {
            var result = await _provider.GenerateAsync(
                scene.Type, scene.Description, scene.Prompt, scene.SearchTerms, cancellationToken);
            results.Add(result);
        }

        return results;
    }
}
