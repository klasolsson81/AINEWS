using Microsoft.Extensions.Logging;
using NewsRoom.Core.Interfaces;

namespace NewsRoom.Infrastructure.Mocks;

public class MockEditorialImageExtractor : IEditorialImageExtractor
{
    private readonly ILogger<MockEditorialImageExtractor> _logger;

    public MockEditorialImageExtractor(ILogger<MockEditorialImageExtractor> logger)
    {
        _logger = logger;
    }

    public Task<string?> ExtractImageUrlAsync(string articleUrl, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MockEditorialImageExtractor: Mock extracting OG image from {Url}", articleUrl);
        return Task.FromResult<string?>($"mock://og-image/{Uri.EscapeDataString(articleUrl)}.jpg");
    }
}
