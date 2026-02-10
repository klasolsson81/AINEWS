using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using NewsRoom.Core.Exceptions;
using NewsRoom.Core.Interfaces;
using NewsRoom.Infrastructure.Tts;

namespace NewsRoom.Tests.Unit.Services;

public class ElevenLabsTtsProviderTests : IDisposable
{
    private readonly Mock<ILogger<ElevenLabsTtsProvider>> _loggerMock;
    private readonly string _testStorageDir;

    public ElevenLabsTtsProviderTests()
    {
        _loggerMock = new Mock<ILogger<ElevenLabsTtsProvider>>();

        // Use a unique temp directory per test instance to avoid collisions
        _testStorageDir = Path.Combine(Path.GetTempPath(), $"elevenlabs_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testStorageDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testStorageDir))
            Directory.Delete(_testStorageDir, recursive: true);
    }

    [Fact]
    public async Task GenerateSpeechAsync_WithValidText_ReturnsResult()
    {
        // Arrange
        var fakeAudioBytes = CreateFakeMp3Bytes(16000); // ~1 second at 128kbps
        var handler = CreateMockHandler(HttpStatusCode.OK, fakeAudioBytes);
        var provider = CreateProvider(handler, apiKey: "test-api-key-123");

        // Act
        var result = await provider.GenerateSpeechAsync(
            "Riksbanken sänker styrräntan med en kvarts procentenhet.",
            "serious, informative");

        // Assert
        result.Should().NotBeNull();
        result.AudioFilePath.Should().NotBeNullOrEmpty();
        result.AudioFilePath.Should().EndWith(".mp3");
        result.AudioFilePath.Should().StartWith(_testStorageDir);
        result.ContentHash.Should().NotBeNullOrEmpty();
        result.ContentHash.Should().HaveLength(32);
        result.DurationSeconds.Should().BeGreaterThanOrEqualTo(0);

        // Verify the file was actually written
        File.Exists(result.AudioFilePath).Should().BeTrue();
        var savedBytes = await File.ReadAllBytesAsync(result.AudioFilePath);
        savedBytes.Should().BeEquivalentTo(fakeAudioBytes);

        // Verify HTTP request was made once
        handler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri!.ToString().Contains("text-to-speech")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GenerateSpeechAsync_WithMissingApiKey_ThrowsTtsGenerationException()
    {
        // Arrange — no API key configured
        var handler = CreateMockHandler(HttpStatusCode.OK, Array.Empty<byte>());
        var provider = CreateProvider(handler, apiKey: null);

        // Act
        var act = () => provider.GenerateSpeechAsync(
            "Test text for TTS generation.",
            "neutral");

        // Assert
        await act.Should().ThrowAsync<TtsGenerationException>()
            .WithMessage("*API key*");
    }

    [Fact]
    public async Task GenerateSpeechAsync_AdjustsSettings_BasedOnTone()
    {
        // Arrange
        string? capturedRequestBody = null;
        var fakeAudioBytes = CreateFakeMp3Bytes(8000);

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>(async (req, _) =>
            {
                if (req.Content != null)
                    capturedRequestBody = await req.Content.ReadAsStringAsync();
            })
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(fakeAudioBytes)
            });

        var provider = CreateProvider(handler, apiKey: "test-key");

        // Act — use "serious" tone which should result in higher stability
        await provider.GenerateSpeechAsync(
            "Allvarliga översvämningar drabbar Västsverige.",
            "serious, concerned");

        // Assert — verify the request body contains adjusted voice settings
        capturedRequestBody.Should().NotBeNullOrEmpty();

        using var doc = JsonDocument.Parse(capturedRequestBody!);
        var root = doc.RootElement;

        root.GetProperty("model_id").GetString().Should().Be("eleven_multilingual_v2");
        root.GetProperty("text").GetString().Should().Contain("översvämningar");

        var voiceSettings = root.GetProperty("voice_settings");
        var stability = voiceSettings.GetProperty("stability").GetDouble();
        var similarityBoost = voiceSettings.GetProperty("similarity_boost").GetDouble();

        // Serious tone: stability = 0.85 (higher than default 0.75)
        stability.Should().Be(0.85);
        // Serious tone: similarity_boost = 0.70 (lower than default 0.75)
        similarityBoost.Should().Be(0.70);
    }

    [Fact]
    public async Task GenerateSpeechAsync_CachesResult_OnSecondCallWithSameText()
    {
        // Arrange
        var fakeAudioBytes = CreateFakeMp3Bytes(8000);
        var callCount = 0;

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(fakeAudioBytes)
                };
            });

        var provider = CreateProvider(handler, apiKey: "test-key");
        var text = "Samma text som ska cachas korrekt.";

        // Act — first call hits the API
        var result1 = await provider.GenerateSpeechAsync(text, "neutral");

        // Act — second call with identical text should use cache
        var result2 = await provider.GenerateSpeechAsync(text, "neutral");

        // Assert
        callCount.Should().Be(1, "second call should use cache and not hit the API");
        result1.ContentHash.Should().Be(result2.ContentHash);
        result1.AudioFilePath.Should().Be(result2.AudioFilePath);
    }

    [Fact]
    public async Task GenerateSpeechAsync_ApiError_ThrowsTtsGenerationException()
    {
        // Arrange
        var errorResponse = Encoding.UTF8.GetBytes(
            "{\"detail\":{\"message\":\"Rate limit exceeded\"}}");
        var handler = CreateMockHandler(HttpStatusCode.TooManyRequests, errorResponse);
        var provider = CreateProvider(handler, apiKey: "test-key");

        // Act
        var act = () => provider.GenerateSpeechAsync("Test text", "neutral");

        // Assert
        await act.Should().ThrowAsync<TtsGenerationException>()
            .WithMessage("*TooManyRequests*");
    }

    [Fact]
    public async Task GenerateSpeechAsync_EmptyAudioResponse_ThrowsTtsGenerationException()
    {
        // Arrange
        var handler = CreateMockHandler(HttpStatusCode.OK, Array.Empty<byte>());
        var provider = CreateProvider(handler, apiKey: "test-key");

        // Act
        var act = () => provider.GenerateSpeechAsync("Test text", "neutral");

        // Assert
        await act.Should().ThrowAsync<TtsGenerationException>()
            .WithMessage("*empty*");
    }

    [Fact]
    public async Task GenerateSpeechAsync_SetsCorrectHeaders()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var fakeAudioBytes = CreateFakeMp3Bytes(8000);

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) =>
            {
                capturedRequest = req;
            })
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(fakeAudioBytes)
            });

        var provider = CreateProvider(handler, apiKey: "my-secret-key");

        // Act
        await provider.GenerateSpeechAsync("Header test", "neutral");

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.GetValues("xi-api-key").Should().Contain("my-secret-key");
        capturedRequest.Headers.Accept.Should().Contain(
            h => h.MediaType == "audio/mpeg");
        capturedRequest.RequestUri!.ToString().Should().Contain("test-voice-id");
    }

    // --- Tests for static helper methods ---

    [Fact]
    public void BuildVoiceSettings_SeriousTone_ReturnsHighStability()
    {
        var settings = ElevenLabsTtsProvider.BuildVoiceSettings("serious, concerned");

        settings.Stability.Should().Be(0.85);
        settings.SimilarityBoost.Should().Be(0.70);
        settings.Style.Should().Be(0.0);
        settings.UseSpeakerBoost.Should().BeTrue();
    }

    [Fact]
    public void BuildVoiceSettings_WarmTone_ReturnsHighSimilarityBoost()
    {
        var settings = ElevenLabsTtsProvider.BuildVoiceSettings("warm, welcoming");

        settings.Stability.Should().Be(0.70);
        settings.SimilarityBoost.Should().Be(0.85);
        settings.Style.Should().Be(0.15);
    }

    [Fact]
    public void BuildVoiceSettings_LightTone_ReturnsLowerStability()
    {
        var settings = ElevenLabsTtsProvider.BuildVoiceSettings("light, casual");

        settings.Stability.Should().Be(0.65);
        settings.SimilarityBoost.Should().Be(0.80);
        settings.Style.Should().Be(0.20);
    }

    [Fact]
    public void BuildVoiceSettings_NullOrEmpty_ReturnsDefaults()
    {
        var settings = ElevenLabsTtsProvider.BuildVoiceSettings("");

        settings.Stability.Should().Be(0.75);
        settings.SimilarityBoost.Should().Be(0.75);
        settings.Style.Should().Be(0.0);
    }

    [Fact]
    public void ComputeContentHash_SameText_ProducesSameHash()
    {
        var hash1 = ElevenLabsTtsProvider.ComputeContentHash("Identical text");
        var hash2 = ElevenLabsTtsProvider.ComputeContentHash("Identical text");

        hash1.Should().Be(hash2);
        hash1.Should().HaveLength(32);
    }

    [Fact]
    public void ComputeContentHash_DifferentText_ProducesDifferentHash()
    {
        var hash1 = ElevenLabsTtsProvider.ComputeContentHash("First text");
        var hash2 = ElevenLabsTtsProvider.ComputeContentHash("Second text");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void EstimateDurationFromFileSize_ReturnsReasonableValue()
    {
        // 128 kbps = 16000 bytes/sec, so 16000 bytes should be ~1 second
        var duration = ElevenLabsTtsProvider.EstimateDurationFromFileSize(16000);

        duration.Should().BeApproximately(1.0, 0.01);
    }

    [Fact]
    public void EstimateDurationFromFileSize_ZeroBytes_ReturnsZero()
    {
        var duration = ElevenLabsTtsProvider.EstimateDurationFromFileSize(0);
        duration.Should().Be(0.0);
    }

    [Fact]
    public void EstimateDurationFromFileSize_NegativeBytes_ReturnsZero()
    {
        var duration = ElevenLabsTtsProvider.EstimateDurationFromFileSize(-100);
        duration.Should().Be(0.0);
    }

    // --- Helper methods ---

    private ElevenLabsTtsProvider CreateProvider(
        Mock<HttpMessageHandler> handler,
        string? apiKey)
    {
        var configData = new Dictionary<string, string?>
        {
            ["TTS_ELEVENLABS_API_KEY"] = apiKey,
            ["TTS_ELEVENLABS_VOICE_ID"] = "test-voice-id",
            ["TTS_STORAGE_DIRECTORY"] = _testStorageDir,
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var httpClient = new HttpClient(handler.Object);
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        return new ElevenLabsTtsProvider(
            factoryMock.Object,
            configuration,
            _loggerMock.Object);
    }

    private static Mock<HttpMessageHandler> CreateMockHandler(
        HttpStatusCode statusCode,
        byte[] responseBytes)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = new ByteArrayContent(responseBytes)
            });
        return handler;
    }

    private static byte[] CreateFakeMp3Bytes(int size)
    {
        var bytes = new byte[size];
        new Random(42).NextBytes(bytes);
        return bytes;
    }
}
