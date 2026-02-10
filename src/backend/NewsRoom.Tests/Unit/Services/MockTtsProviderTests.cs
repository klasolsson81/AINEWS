using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NewsRoom.Infrastructure.Mocks;

namespace NewsRoom.Tests.Unit.Services;

public class MockTtsProviderTests
{
    private readonly MockTtsProvider _sut;

    public MockTtsProviderTests()
    {
        _sut = new MockTtsProvider(Mock.Of<ILogger<MockTtsProvider>>());
    }

    [Fact]
    public async Task GenerateSpeechAsync_ReturnsDurationBasedOnWordCount()
    {
        // ~10 words => ~4 seconds at 2.5 words/sec
        var result = await _sut.GenerateSpeechAsync("Ett två tre fyra fem sex sju åtta nio tio", "neutral");

        result.DurationSeconds.Should().BeGreaterThan(0);
        result.AudioFilePath.Should().NotBeNullOrEmpty();
        result.ContentHash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateSpeechAsync_SameText_ProducesSameHash()
    {
        var text = "Riksbanken sänker räntan till två och en halv procent.";

        var result1 = await _sut.GenerateSpeechAsync(text, "neutral");
        var result2 = await _sut.GenerateSpeechAsync(text, "neutral");

        result1.ContentHash.Should().Be(result2.ContentHash);
    }

    [Fact]
    public async Task GenerateSpeechAsync_DifferentText_ProducesDifferentHash()
    {
        var result1 = await _sut.GenerateSpeechAsync("Första texten", "neutral");
        var result2 = await _sut.GenerateSpeechAsync("Andra texten", "neutral");

        result1.ContentHash.Should().NotBe(result2.ContentHash);
    }
}
