using Microsoft.Extensions.Logging;
using NewsRoom.Core.Interfaces;

namespace NewsRoom.Infrastructure.Mocks;

public class MockAvatarGenerator : IAvatarGenerator
{
    private readonly ILogger<MockAvatarGenerator> _logger;

    public MockAvatarGenerator(ILogger<MockAvatarGenerator> logger)
    {
        _logger = logger;
    }

    public Task<AvatarResult> GenerateAvatarVideoAsync(
        string audioFilePath,
        string tone,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MockAvatarGenerator: Generated mock avatar video for audio {Audio}", audioFilePath);

        return Task.FromResult(new AvatarResult
        {
            VideoFilePath = $"mock://avatar/{Guid.NewGuid():N}.mp4",
            DurationSeconds = 15.0
        });
    }
}
