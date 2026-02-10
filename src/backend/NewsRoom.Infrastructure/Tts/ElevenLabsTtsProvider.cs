using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NewsRoom.Core.Exceptions;
using NewsRoom.Core.Interfaces;

namespace NewsRoom.Infrastructure.Tts;

public class ElevenLabsTtsProvider : ITtsProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ElevenLabsTtsProvider> _logger;
    private readonly string _storageDirectory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private const string BaseUrl = "https://api.elevenlabs.io/v1/text-to-speech";
    private const string DefaultModelId = "eleven_multilingual_v2";
    private const string DefaultStorageDirectory = "storage/audio";

    // Default multilingual voice; override via TTS_ELEVENLABS_VOICE_ID config
    private const string DefaultVoiceId = "21m00Tcm4TlvDq8ikWAM";

    // Approximate MP3 bitrate for duration estimation (128 kbps)
    private const double Mp3BitrateKbps = 128.0;

    public ElevenLabsTtsProvider(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ElevenLabsTtsProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;

        // Allow storage directory override via config (useful for testing)
        _storageDirectory = _configuration["TTS_STORAGE_DIRECTORY"]
            ?? Environment.GetEnvironmentVariable("TTS_STORAGE_DIRECTORY")
            ?? DefaultStorageDirectory;
    }

    public async Task<TtsResult> GenerateSpeechAsync(
        string text,
        string tone,
        CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["TTS_ELEVENLABS_API_KEY"]
            ?? Environment.GetEnvironmentVariable("TTS_ELEVENLABS_API_KEY");

        if (string.IsNullOrEmpty(apiKey))
            throw new TtsGenerationException("ElevenLabs API key not configured. Set TTS_ELEVENLABS_API_KEY.");

        var voiceId = _configuration["TTS_ELEVENLABS_VOICE_ID"]
            ?? Environment.GetEnvironmentVariable("TTS_ELEVENLABS_VOICE_ID")
            ?? DefaultVoiceId;

        // Compute content hash for caching
        var contentHash = ComputeContentHash(text);
        var audioFilePath = Path.Combine(_storageDirectory, $"{contentHash}.mp3");

        // Check cache first
        if (File.Exists(audioFilePath))
        {
            var cachedFileInfo = new FileInfo(audioFilePath);
            var cachedDuration = EstimateDurationFromFileSize(cachedFileInfo.Length);

            _logger.LogInformation(
                "ElevenLabs: Cache hit for hash {Hash}, duration {Duration:F1}s",
                contentHash, cachedDuration);

            return new TtsResult
            {
                AudioFilePath = audioFilePath,
                DurationSeconds = cachedDuration,
                ContentHash = contentHash
            };
        }

        _logger.LogInformation(
            "ElevenLabs: Generating speech for {TextLength} chars, tone '{Tone}', voice '{VoiceId}'",
            text.Length, tone, voiceId);

        // Build voice settings based on tone
        var voiceSettings = BuildVoiceSettings(tone);

        var requestBody = new
        {
            text,
            model_id = DefaultModelId,
            voice_settings = voiceSettings
        };

        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(60);

        var requestUrl = $"{BaseUrl}/{voiceId}";
        var json = JsonSerializer.Serialize(requestBody, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
        request.Headers.Add("xi-api-key", apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("audio/mpeg"));
        request.Content = content;

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "ElevenLabs: HTTP request failed");
            throw new TtsGenerationException("Failed to connect to ElevenLabs API", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "ElevenLabs: Request timed out");
            throw new TtsGenerationException("ElevenLabs API request timed out", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "ElevenLabs API error: {Status} - {Body}",
                response.StatusCode, errorBody);
            throw new TtsGenerationException(
                $"ElevenLabs API returned {response.StatusCode}: {errorBody}");
        }

        // Read audio bytes from response
        var audioBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        if (audioBytes.Length == 0)
            throw new TtsGenerationException("ElevenLabs returned empty audio response");

        // Ensure storage directory exists
        Directory.CreateDirectory(_storageDirectory);

        // Save to file
        await File.WriteAllBytesAsync(audioFilePath, audioBytes, cancellationToken);

        var duration = EstimateDurationFromFileSize(audioBytes.Length);

        _logger.LogInformation(
            "ElevenLabs: Generated audio saved to {Path}, size {Size} bytes, estimated duration {Duration:F1}s",
            audioFilePath, audioBytes.Length, duration);

        return new TtsResult
        {
            AudioFilePath = audioFilePath,
            DurationSeconds = duration,
            ContentHash = contentHash
        };
    }

    /// <summary>
    /// Builds ElevenLabs voice_settings object adjusted for the given tone.
    /// Different tones map to different stability/similarity/style parameters
    /// to achieve appropriate vocal delivery for news segments.
    /// </summary>
    public static VoiceSettings BuildVoiceSettings(string tone)
    {
        var settings = new VoiceSettings
        {
            Stability = 0.75,
            SimilarityBoost = 0.75,
            Style = 0.0,
            UseSpeakerBoost = true
        };

        if (string.IsNullOrEmpty(tone))
            return settings;

        var toneLower = tone.ToLowerInvariant();

        // Serious/concerned tone: higher stability for a more controlled delivery
        if (toneLower.Contains("serious") || toneLower.Contains("concerned"))
        {
            settings.Stability = 0.85;
            settings.SimilarityBoost = 0.70;
            settings.Style = 0.0;
        }
        // Warm/welcoming tone: boost similarity for natural warmth
        else if (toneLower.Contains("warm") || toneLower.Contains("welcoming"))
        {
            settings.Stability = 0.70;
            settings.SimilarityBoost = 0.85;
            settings.Style = 0.15;
        }
        // Light/casual tone: slightly less stable for more expression
        else if (toneLower.Contains("light") || toneLower.Contains("casual"))
        {
            settings.Stability = 0.65;
            settings.SimilarityBoost = 0.80;
            settings.Style = 0.20;
        }
        // Transitional tone: neutral, clean delivery for bridges between segments
        else if (toneLower.Contains("transitional") || toneLower.Contains("closing"))
        {
            settings.Stability = 0.75;
            settings.SimilarityBoost = 0.75;
            settings.Style = 0.05;
        }
        // Informative tone: clear, professional
        else if (toneLower.Contains("informative") || toneLower.Contains("professional"))
        {
            settings.Stability = 0.80;
            settings.SimilarityBoost = 0.75;
            settings.Style = 0.0;
        }

        return settings;
    }

    /// <summary>
    /// Computes a SHA256-based content hash for caching audio files.
    /// Returns a 32-character lowercase hex string.
    /// </summary>
    public static string ComputeContentHash(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes)[..32].ToLowerInvariant();
    }

    /// <summary>
    /// Estimates audio duration from MP3 file size assuming 128 kbps bitrate.
    /// Formula: duration = fileSizeBytes * 8 / (bitrateKbps * 1000)
    /// </summary>
    public static double EstimateDurationFromFileSize(long fileSizeBytes)
    {
        if (fileSizeBytes <= 0) return 0.0;
        return fileSizeBytes * 8.0 / (Mp3BitrateKbps * 1000.0);
    }

    /// <summary>
    /// Voice settings model matching ElevenLabs API schema.
    /// Properties use snake_case to match the API's expected JSON format
    /// when serialized with default JsonSerializer settings.
    /// </summary>
    public class VoiceSettings
    {
        public double Stability { get; set; }
        public double SimilarityBoost { get; set; }
        public double Style { get; set; }
        public bool UseSpeakerBoost { get; set; }
    }
}
