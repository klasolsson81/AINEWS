using Microsoft.Extensions.Logging;
using NewsRoom.Core.Interfaces;

namespace NewsRoom.Infrastructure.Mocks;

public class MockDataGraphicGenerator : IDataGraphicGenerator
{
    private readonly ILogger<MockDataGraphicGenerator> _logger;

    public MockDataGraphicGenerator(ILogger<MockDataGraphicGenerator> logger)
    {
        _logger = logger;
    }

    public Task<string> GenerateGraphicAsync(string description, string? dataHint = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MockDataGraphicGenerator: Mock generating graphic for: {Description}", description);
        return Task.FromResult($"mock://graphics/{Guid.NewGuid():N}.png");
    }
}
