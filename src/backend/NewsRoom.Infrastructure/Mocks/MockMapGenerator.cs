using Microsoft.Extensions.Logging;
using NewsRoom.Core.Interfaces;

namespace NewsRoom.Infrastructure.Mocks;

public class MockMapGenerator : IMapGenerator
{
    private readonly ILogger<MockMapGenerator> _logger;

    public MockMapGenerator(ILogger<MockMapGenerator> logger)
    {
        _logger = logger;
    }

    public Task<string> GenerateMapAsync(string description, string? sourceHint = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MockMapGenerator: Mock generating map for: {Description}", description);
        return Task.FromResult($"mock://maps/{Guid.NewGuid():N}.png");
    }
}
