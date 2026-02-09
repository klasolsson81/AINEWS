using Microsoft.Extensions.Logging;
using NewsRoom.Core.Interfaces;
using NewsRoom.Core.Models;

namespace NewsRoom.Infrastructure.Mocks;

public class MockVideoComposer : IVideoComposer
{
    private readonly ILogger<MockVideoComposer> _logger;

    public MockVideoComposer(ILogger<MockVideoComposer> logger)
    {
        _logger = logger;
    }

    public Task<string> ComposeAsync(BroadcastScript script, CompositionAssets assets, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MockVideoComposer: Mock composing video for broadcast {Id} with {Segments} segments",
            script.BroadcastId, script.TotalSegments);
        return Task.FromResult($"mock://broadcasts/{script.BroadcastId}.mp4");
    }
}
