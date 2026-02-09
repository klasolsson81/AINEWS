using Microsoft.Extensions.Logging;
using NewsRoom.Core.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace NewsRoom.Infrastructure.Mocks;

public class MockTtsProvider : ITtsProvider
{
    private readonly ILogger<MockTtsProvider> _logger;

    public MockTtsProvider(ILogger<MockTtsProvider> logger)
    {
        _logger = logger;
    }

    public Task<TtsResult> GenerateSpeechAsync(
        string text,
        string tone,
        CancellationToken cancellationToken = default)
    {
        var wordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var estimatedDuration = wordCount / 2.5; // ~150 words/min for Swedish news

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)))[..16];

        _logger.LogInformation("MockTtsProvider: Generated mock audio for {Words} words, duration {Duration:F1}s",
            wordCount, estimatedDuration);

        return Task.FromResult(new TtsResult
        {
            AudioFilePath = $"mock://audio/{hash}.mp3",
            DurationSeconds = estimatedDuration,
            ContentHash = hash
        });
    }
}
