using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NewsRoom.Core.Enums;
using NewsRoom.Core.Exceptions;
using NewsRoom.Core.Interfaces;

namespace NewsRoom.Infrastructure.BRoll;

/// <summary>
/// IBRollProvider implementation that fetches royalty-free stock photos and videos
/// from the Pexels API (https://www.pexels.com/api/).
/// Supports both photo and video search with local file caching.
/// </summary>
public class PexelsBRollProvider : IBRollProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PexelsBRollProvider> _logger;

    private const string PhotoSearchUrl = "https://api.pexels.com/v1/search";
    private const string VideoSearchUrl = "https://api.pexels.com/videos/search";
    private const string StorageDirectory = "storage/broll";
    private const int TargetVideoWidth = 1920;
    private const double DefaultPhotoDurationSeconds = 8.0;

    public PexelsBRollProvider(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<PexelsBRollProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<BRollResult> GenerateAsync(
        VisualContentType type,
        string description,
        string? prompt = null,
        IEnumerable<string>? searchTerms = null,
        CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["BROLL_PEXELS_API_KEY"]
            ?? Environment.GetEnvironmentVariable("BROLL_PEXELS_API_KEY");

        if (string.IsNullOrEmpty(apiKey))
            throw new BRollGenerationException("Pexels API key not configured. Set BROLL_PEXELS_API_KEY.");

        var queries = BuildSearchQueries(description, searchTerms);

        _logger.LogInformation(
            "Pexels: Searching {Type} for '{Description}' with {QueryCount} queries",
            type, description, queries.Count);

        // StockFootage type: try video search first, fallback to photos
        if (type == VisualContentType.StockFootage)
        {
            var videoResult = await TrySearchVideosAsync(apiKey, queries, cancellationToken);
            if (videoResult != null)
                return videoResult;

            _logger.LogInformation("Pexels: No video results found, falling back to photo search");
        }

        // Search photos (primary for non-StockFootage, fallback for StockFootage)
        var photoResult = await TrySearchPhotosAsync(apiKey, queries, cancellationToken);
        if (photoResult != null)
            return photoResult;

        // No results found at all
        _logger.LogWarning(
            "Pexels: No results found for any query. Type={Type}, Description='{Description}'",
            type, description);

        return new BRollResult
        {
            FilePath = string.Empty,
            DurationSeconds = 0,
            IsVideo = false,
            Attribution = null
        };
    }

    /// <summary>
    /// Builds an ordered list of search queries from searchTerms and description.
    /// SearchTerms are tried first (in order), with description as final fallback.
    /// </summary>
    internal static List<string> BuildSearchQueries(string description, IEnumerable<string>? searchTerms)
    {
        var queries = new List<string>();

        if (searchTerms != null)
        {
            foreach (var term in searchTerms)
            {
                if (!string.IsNullOrWhiteSpace(term))
                    queries.Add(term.Trim());
            }
        }

        // Always add description as final fallback if not already included
        if (!string.IsNullOrWhiteSpace(description))
        {
            var trimmed = description.Trim();
            if (!queries.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
                queries.Add(trimmed);
        }

        return queries;
    }

    /// <summary>
    /// Attempts to find and download a video from Pexels for any of the provided queries.
    /// Returns null if no video is found.
    /// </summary>
    private async Task<BRollResult?> TrySearchVideosAsync(
        string apiKey,
        List<string> queries,
        CancellationToken cancellationToken)
    {
        foreach (var query in queries)
        {
            var url = $"{VideoSearchUrl}?query={Uri.EscapeDataString(query)}&per_page=3&orientation=landscape";

            var responseJson = await SendPexelsRequestAsync(apiKey, url, TimeSpan.FromSeconds(30), cancellationToken);
            if (responseJson == null)
                continue;

            var videoInfo = ParseVideoResponse(responseJson);
            if (videoInfo == null)
                continue;

            _logger.LogInformation(
                "Pexels: Found video for query '{Query}', duration={Duration}s, downloading from {Url}",
                query, videoInfo.Value.Duration, videoInfo.Value.DownloadUrl);

            var filePath = await DownloadFileAsync(
                apiKey, videoInfo.Value.DownloadUrl, "mp4", cancellationToken);

            return new BRollResult
            {
                FilePath = filePath,
                DurationSeconds = videoInfo.Value.Duration,
                IsVideo = true,
                Attribution = $"Video: Pexels"
            };
        }

        return null;
    }

    /// <summary>
    /// Attempts to find and download a photo from Pexels for any of the provided queries.
    /// Returns null if no photo is found.
    /// </summary>
    private async Task<BRollResult?> TrySearchPhotosAsync(
        string apiKey,
        List<string> queries,
        CancellationToken cancellationToken)
    {
        foreach (var query in queries)
        {
            var url = $"{PhotoSearchUrl}?query={Uri.EscapeDataString(query)}&per_page=5&orientation=landscape";

            var responseJson = await SendPexelsRequestAsync(apiKey, url, TimeSpan.FromSeconds(30), cancellationToken);
            if (responseJson == null)
                continue;

            var photoInfo = ParsePhotoResponse(responseJson);
            if (photoInfo == null)
                continue;

            _logger.LogInformation(
                "Pexels: Found photo for query '{Query}' by {Photographer}, downloading from {Url}",
                query, photoInfo.Value.Photographer, photoInfo.Value.DownloadUrl);

            var filePath = await DownloadFileAsync(
                apiKey, photoInfo.Value.DownloadUrl, "jpg", cancellationToken);

            return new BRollResult
            {
                FilePath = filePath,
                DurationSeconds = DefaultPhotoDurationSeconds,
                IsVideo = false,
                Attribution = $"Foto: {photoInfo.Value.Photographer} / Pexels"
            };
        }

        return null;
    }

    /// <summary>
    /// Sends an authenticated GET request to the Pexels API and returns the raw JSON response.
    /// Returns null if the request fails.
    /// </summary>
    private async Task<string?> SendPexelsRequestAsync(
        string apiKey,
        string url,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        client.Timeout = timeout;

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Authorization", apiKey);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Pexels: HTTP request failed for {Url}", url);
            throw new BRollGenerationException($"Failed to connect to Pexels API: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Pexels: Request timed out for {Url}", url);
            throw new BRollGenerationException("Pexels API request timed out", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Pexels API returned {Status} for {Url}: {Body}",
                response.StatusCode, url, errorBody);
            return null;
        }

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    /// <summary>
    /// Parses the Pexels photo search response JSON and extracts the first photo's
    /// download URL and photographer attribution.
    /// </summary>
    internal static PhotoInfo? ParsePhotoResponse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("photos", out var photos))
                return null;

            var photosArray = photos.EnumerateArray();
            if (!photosArray.MoveNext())
                return null;

            var firstPhoto = photosArray.Current;

            var photographer = firstPhoto.TryGetProperty("photographer", out var p)
                ? p.GetString() ?? "Unknown"
                : "Unknown";

            if (!firstPhoto.TryGetProperty("src", out var src))
                return null;

            var downloadUrl = src.TryGetProperty("large2x", out var large2x)
                ? large2x.GetString()
                : null;

            if (string.IsNullOrEmpty(downloadUrl))
                return null;

            return new PhotoInfo
            {
                DownloadUrl = downloadUrl,
                Photographer = photographer
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Parses the Pexels video search response JSON and extracts the best video file URL
    /// (closest to 1920px width, MP4 format) and the video duration.
    /// </summary>
    internal static VideoInfo? ParseVideoResponse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("videos", out var videos))
                return null;

            var videosArray = videos.EnumerateArray();
            if (!videosArray.MoveNext())
                return null;

            var firstVideo = videosArray.Current;

            var duration = firstVideo.TryGetProperty("duration", out var d)
                ? d.GetDouble()
                : 8.0;

            if (!firstVideo.TryGetProperty("video_files", out var videoFiles))
                return null;

            // Find the best MP4 video file: closest to TargetVideoWidth
            string? bestUrl = null;
            int bestWidthDiff = int.MaxValue;

            foreach (var file in videoFiles.EnumerateArray())
            {
                var fileType = file.TryGetProperty("file_type", out var ft)
                    ? ft.GetString()
                    : null;

                if (fileType != "video/mp4")
                    continue;

                var width = file.TryGetProperty("width", out var w)
                    ? w.GetInt32()
                    : 0;

                var link = file.TryGetProperty("link", out var l)
                    ? l.GetString()
                    : null;

                if (string.IsNullOrEmpty(link))
                    continue;

                var widthDiff = Math.Abs(width - TargetVideoWidth);
                if (widthDiff < bestWidthDiff)
                {
                    bestWidthDiff = widthDiff;
                    bestUrl = link;
                }
            }

            if (string.IsNullOrEmpty(bestUrl))
                return null;

            return new VideoInfo
            {
                DownloadUrl = bestUrl,
                Duration = duration
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Downloads a media file from the given URL to the local broll storage directory.
    /// Uses URL-based hashing for cache: if the file already exists, the download is skipped.
    /// </summary>
    private async Task<string> DownloadFileAsync(
        string apiKey,
        string downloadUrl,
        string extension,
        CancellationToken cancellationToken)
    {
        var hash = ComputeUrlHash(downloadUrl);
        var filePath = Path.Combine(StorageDirectory, $"{hash}.{extension}");

        // Cache check: skip download if file already exists
        if (File.Exists(filePath))
        {
            _logger.LogInformation("Pexels: Cache hit for {FilePath}", filePath);
            return filePath;
        }

        // Ensure storage directory exists
        Directory.CreateDirectory(StorageDirectory);

        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(60);

        // Pexels CDN URLs typically don't require auth, but we include it just in case
        using var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
        request.Headers.Add("Authorization", apiKey);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Pexels: Download failed for {Url}", downloadUrl);
            throw new BRollGenerationException($"Failed to download media from Pexels: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Pexels: Download timed out for {Url}", downloadUrl);
            throw new BRollGenerationException("Pexels media download timed out", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Pexels: Download returned {Status} for {Url}: {Body}",
                response.StatusCode, downloadUrl, errorBody);
            throw new BRollGenerationException(
                $"Pexels media download failed with {response.StatusCode}");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        if (bytes.Length == 0)
            throw new BRollGenerationException("Pexels returned empty media file");

        await File.WriteAllBytesAsync(filePath, bytes, cancellationToken);

        _logger.LogInformation(
            "Pexels: Downloaded {Size} bytes to {FilePath}",
            bytes.Length, filePath);

        return filePath;
    }

    /// <summary>
    /// Computes a SHA256-based hash of a URL for use as a cache-safe filename.
    /// </summary>
    internal static string ComputeUrlHash(string url)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(url));
        return Convert.ToHexString(bytes)[..32].ToLowerInvariant();
    }

    internal struct PhotoInfo
    {
        public string DownloadUrl;
        public string Photographer;
    }

    internal struct VideoInfo
    {
        public string DownloadUrl;
        public double Duration;
    }
}
