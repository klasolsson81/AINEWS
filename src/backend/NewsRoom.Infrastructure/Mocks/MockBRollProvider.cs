using Microsoft.Extensions.Logging;
using NewsRoom.Core.Enums;
using NewsRoom.Core.Interfaces;

namespace NewsRoom.Infrastructure.Mocks;

public class MockBRollProvider : IBRollProvider
{
    private readonly ILogger<MockBRollProvider> _logger;

    public MockBRollProvider(ILogger<MockBRollProvider> logger)
    {
        _logger = logger;
    }

    public Task<BRollResult> GenerateAsync(
        VisualContentType type,
        string description,
        string? prompt = null,
        IEnumerable<string>? searchTerms = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MockBRollProvider: Generated mock B-roll ({Type}) for: {Description}", type, description);

        return Task.FromResult(new BRollResult
        {
            FilePath = $"mock://broll/{Guid.NewGuid():N}.{(type == VisualContentType.StockFootage ? "mp4" : "jpg")}",
            DurationSeconds = 8.0,
            IsVideo = type == VisualContentType.StockFootage,
            Attribution = type == VisualContentType.StockFootage ? "Pexels (mock)" : null
        });
    }
}
