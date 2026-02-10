using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using NewsRoom.Core.Enums;
using NewsRoom.Core.Exceptions;
using NewsRoom.Infrastructure.BRoll;

namespace NewsRoom.Tests.Unit.Services;

public class PexelsBRollProviderTests : IDisposable
{
    private const string TestApiKey = "test-pexels-api-key-12345";

    // Realistic Pexels photo search response JSON
    private const string PhotoResponseJson = """
        {
            "total_results": 42,
            "page": 1,
            "per_page": 5,
            "photos": [
                {
                    "id": 12345,
                    "width": 1920,
                    "height": 1080,
                    "url": "https://www.pexels.com/photo/flooding-12345/",
                    "photographer": "Anna Svensson",
                    "photographer_url": "https://www.pexels.com/@annasvensson",
                    "src": {
                        "original": "https://images.pexels.com/photos/12345/pexels-photo-12345.jpeg",
                        "large2x": "https://images.pexels.com/photos/12345/pexels-photo-12345.jpeg?auto=compress&cs=tinysrgb&dpr=2&h=650&w=940",
                        "large": "https://images.pexels.com/photos/12345/pexels-photo-12345.jpeg?auto=compress&cs=tinysrgb&h=650&w=940",
                        "medium": "https://images.pexels.com/photos/12345/pexels-photo-12345.jpeg?auto=compress&cs=tinysrgb&h=350"
                    }
                }
            ]
        }
        """;

    // Realistic Pexels video search response JSON
    private const string VideoResponseJson = """
        {
            "total_results": 15,
            "page": 1,
            "per_page": 3,
            "videos": [
                {
                    "id": 67890,
                    "width": 1920,
                    "height": 1080,
                    "url": "https://www.pexels.com/video/flooding-67890/",
                    "duration": 14.5,
                    "video_files": [
                        {
                            "id": 1001,
                            "quality": "sd",
                            "file_type": "video/mp4",
                            "width": 640,
                            "height": 360,
                            "link": "https://videos.pexels.com/video-files/67890/67890-sd_640_360_25fps.mp4"
                        },
                        {
                            "id": 1002,
                            "quality": "hd",
                            "file_type": "video/mp4",
                            "width": 1920,
                            "height": 1080,
                            "link": "https://videos.pexels.com/video-files/67890/67890-hd_1920_1080_25fps.mp4"
                        },
                        {
                            "id": 1003,
                            "quality": "hd",
                            "file_type": "video/mp4",
                            "width": 1280,
                            "height": 720,
                            "link": "https://videos.pexels.com/video-files/67890/67890-hd_1280_720_25fps.mp4"
                        }
                    ]
                }
            ]
        }
        """;

    // Empty results response
    private const string EmptyPhotosResponseJson = """
        {
            "total_results": 0,
            "page": 1,
            "per_page": 5,
            "photos": []
        }
        """;

    private const string EmptyVideosResponseJson = """
        {
            "total_results": 0,
            "page": 1,
            "per_page": 3,
            "videos": []
        }
        """;

    private readonly string _tempStorageDir;

    public PexelsBRollProviderTests()
    {
        // Use a unique temp directory for each test run to avoid cross-test interference
        _tempStorageDir = Path.Combine(Path.GetTempPath(), $"pexels_test_{Guid.NewGuid():N}");
    }

    public void Dispose()
    {
        // Clean up temp storage directory if created
        if (Directory.Exists(_tempStorageDir))
            Directory.Delete(_tempStorageDir, true);
    }

    [Fact]
    public async Task GenerateAsync_PhotoSearch_ReturnsPhotoResult()
    {
        // Arrange
        var fakeImageBytes = Encoding.UTF8.GetBytes("fake-jpeg-data-for-testing");
        var handler = CreateMockHandler(new Dictionary<string, HttpResponseMessage>
        {
            ["https://api.pexels.com/v1/search"] = CreateJsonResponse(PhotoResponseJson),
            ["https://images.pexels.com"] = CreateBinaryResponse(fakeImageBytes, "image/jpeg"),
        });

        var provider = CreateProvider(handler, TestApiKey);

        // Act
        var result = await provider.GenerateAsync(
            VisualContentType.EditorialImage,
            "flooding urban street",
            cancellationToken: CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsVideo.Should().BeFalse();
        result.DurationSeconds.Should().Be(8.0);
        result.FilePath.Should().NotBeNullOrEmpty();
        result.FilePath.Should().EndWith(".jpg");
        result.Attribution.Should().Be("Foto: Anna Svensson / Pexels");
    }

    [Fact]
    public async Task GenerateAsync_VideoSearch_ReturnsVideoResult()
    {
        // Arrange
        var fakeVideoBytes = Encoding.UTF8.GetBytes("fake-mp4-data-for-testing");
        var handler = CreateMockHandler(new Dictionary<string, HttpResponseMessage>
        {
            ["https://api.pexels.com/videos/search"] = CreateJsonResponse(VideoResponseJson),
            ["https://videos.pexels.com"] = CreateBinaryResponse(fakeVideoBytes, "video/mp4"),
        });

        var provider = CreateProvider(handler, TestApiKey);

        // Act
        var result = await provider.GenerateAsync(
            VisualContentType.StockFootage,
            "flooding rescue team",
            cancellationToken: CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsVideo.Should().BeTrue();
        result.DurationSeconds.Should().Be(14.5);
        result.FilePath.Should().NotBeNullOrEmpty();
        result.FilePath.Should().EndWith(".mp4");
        result.Attribution.Should().Be("Video: Pexels");
    }

    [Fact]
    public async Task GenerateAsync_MissingApiKey_ThrowsBRollGenerationException()
    {
        // Arrange — no API key configured
        var handler = CreateMockHandler(new Dictionary<string, HttpResponseMessage>());
        var provider = CreateProvider(handler, apiKey: null);

        // Act
        var act = () => provider.GenerateAsync(
            VisualContentType.StockFootage,
            "some description",
            cancellationToken: CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BRollGenerationException>()
            .WithMessage("*Pexels API key not configured*");
    }

    [Fact]
    public async Task GenerateAsync_UsesSearchTerms_WhenProvided()
    {
        // Arrange
        var fakeImageBytes = Encoding.UTF8.GetBytes("fake-jpeg-data");
        string? capturedSearchUrl = null;

        var handler = CreateMockHandler(new Dictionary<string, HttpResponseMessage>
        {
            ["https://api.pexels.com/v1/search"] = CreateJsonResponse(PhotoResponseJson),
            ["https://images.pexels.com"] = CreateBinaryResponse(fakeImageBytes, "image/jpeg"),
        }, onRequest: url => capturedSearchUrl ??= url);

        var provider = CreateProvider(handler, TestApiKey);
        var searchTerms = new[] { "flooding urban street sweden", "rescue team water" };

        // Act
        var result = await provider.GenerateAsync(
            VisualContentType.EditorialImage,
            "A completely different description",
            searchTerms: searchTerms,
            cancellationToken: CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        // The first search term should be used as the query parameter
        capturedSearchUrl.Should().NotBeNull();
        capturedSearchUrl.Should().Contain("flooding urban street sweden");
    }

    [Fact]
    public async Task GenerateAsync_StockFootage_FallsBackToPhotos_WhenNoVideosFound()
    {
        // Arrange — video search returns empty, photo search returns a result
        var fakeImageBytes = Encoding.UTF8.GetBytes("fake-jpeg-fallback");
        var handler = CreateMockHandler(new Dictionary<string, HttpResponseMessage>
        {
            ["https://api.pexels.com/videos/search"] = CreateJsonResponse(EmptyVideosResponseJson),
            ["https://api.pexels.com/v1/search"] = CreateJsonResponse(PhotoResponseJson),
            ["https://images.pexels.com"] = CreateBinaryResponse(fakeImageBytes, "image/jpeg"),
        });

        var provider = CreateProvider(handler, TestApiKey);

        // Act
        var result = await provider.GenerateAsync(
            VisualContentType.StockFootage,
            "some topic",
            cancellationToken: CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsVideo.Should().BeFalse();
        result.Attribution.Should().Contain("Anna Svensson");
    }

    [Fact]
    public async Task GenerateAsync_NoResults_ReturnsEmptyResult()
    {
        // Arrange — both searches return empty
        var handler = CreateMockHandler(new Dictionary<string, HttpResponseMessage>
        {
            ["https://api.pexels.com/videos/search"] = CreateJsonResponse(EmptyVideosResponseJson),
            ["https://api.pexels.com/v1/search"] = CreateJsonResponse(EmptyPhotosResponseJson),
        });

        var provider = CreateProvider(handler, TestApiKey);

        // Act
        var result = await provider.GenerateAsync(
            VisualContentType.StockFootage,
            "extremely obscure topic xyz123",
            cancellationToken: CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.FilePath.Should().BeEmpty();
        result.DurationSeconds.Should().Be(0);
        result.Attribution.Should().BeNull();
    }

    [Fact]
    public void ParsePhotoResponse_ValidJson_ReturnsPhotoInfo()
    {
        // Act
        var result = PexelsBRollProvider.ParsePhotoResponse(PhotoResponseJson);

        // Assert
        result.Should().NotBeNull();
        result!.Value.Photographer.Should().Be("Anna Svensson");
        result.Value.DownloadUrl.Should().Contain("pexels-photo-12345.jpeg");
    }

    [Fact]
    public void ParseVideoResponse_ValidJson_SelectsBestResolution()
    {
        // Act
        var result = PexelsBRollProvider.ParseVideoResponse(VideoResponseJson);

        // Assert
        result.Should().NotBeNull();
        result!.Value.Duration.Should().Be(14.5);
        // Should pick the 1920px wide file (closest to target 1920)
        result.Value.DownloadUrl.Should().Contain("1920_1080");
    }

    [Fact]
    public void ParsePhotoResponse_EmptyArray_ReturnsNull()
    {
        var result = PexelsBRollProvider.ParsePhotoResponse(EmptyPhotosResponseJson);
        result.Should().BeNull();
    }

    [Fact]
    public void ParseVideoResponse_EmptyArray_ReturnsNull()
    {
        var result = PexelsBRollProvider.ParseVideoResponse(EmptyVideosResponseJson);
        result.Should().BeNull();
    }

    [Fact]
    public void ComputeUrlHash_SameUrl_ReturnsSameHash()
    {
        var url = "https://images.pexels.com/photos/12345/photo.jpeg";
        var hash1 = PexelsBRollProvider.ComputeUrlHash(url);
        var hash2 = PexelsBRollProvider.ComputeUrlHash(url);

        hash1.Should().Be(hash2);
        hash1.Should().HaveLength(32);
    }

    [Fact]
    public void ComputeUrlHash_DifferentUrls_ReturnDifferentHashes()
    {
        var hash1 = PexelsBRollProvider.ComputeUrlHash("https://example.com/photo1.jpg");
        var hash2 = PexelsBRollProvider.ComputeUrlHash("https://example.com/photo2.jpg");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void BuildSearchQueries_WithSearchTerms_PutsTermsFirst()
    {
        var queries = PexelsBRollProvider.BuildSearchQueries(
            "fallback description",
            new[] { "primary term", "secondary term" });

        queries.Should().HaveCount(3);
        queries[0].Should().Be("primary term");
        queries[1].Should().Be("secondary term");
        queries[2].Should().Be("fallback description");
    }

    [Fact]
    public void BuildSearchQueries_NullSearchTerms_UsesDescriptionOnly()
    {
        var queries = PexelsBRollProvider.BuildSearchQueries("the description", null);

        queries.Should().HaveCount(1);
        queries[0].Should().Be("the description");
    }

    [Fact]
    public void BuildSearchQueries_EmptySearchTerms_UsesDescriptionOnly()
    {
        var queries = PexelsBRollProvider.BuildSearchQueries("the description", Array.Empty<string>());

        queries.Should().HaveCount(1);
        queries[0].Should().Be("the description");
    }

    // ──── Helper methods ────

    /// <summary>
    /// Creates a PexelsBRollProvider with a mocked HttpMessageHandler and configuration.
    /// </summary>
    private PexelsBRollProvider CreateProvider(Mock<HttpMessageHandler> handler, string? apiKey)
    {
        var httpClient = new HttpClient(handler.Object);
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(handler.Object));

        var configData = new Dictionary<string, string?>();
        if (apiKey != null)
            configData["BROLL_PEXELS_API_KEY"] = apiKey;

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var logger = Mock.Of<ILogger<PexelsBRollProvider>>();

        return new PexelsBRollProvider(httpClientFactory.Object, configuration, logger);
    }

    /// <summary>
    /// Creates a mock HttpMessageHandler that routes requests based on URL prefix matching.
    /// Supports an optional callback to capture the requested URL.
    /// </summary>
    private static Mock<HttpMessageHandler> CreateMockHandler(
        Dictionary<string, HttpResponseMessage> responses,
        Action<string>? onRequest = null)
    {
        var handler = new Mock<HttpMessageHandler>();

        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken _) =>
            {
                var url = request.RequestUri!.ToString();
                onRequest?.Invoke(url);

                foreach (var kvp in responses)
                {
                    if (url.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase))
                        return kvp.Value;
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

        return handler;
    }

    private static HttpResponseMessage CreateJsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private static HttpResponseMessage CreateBinaryResponse(byte[] data, string contentType)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(data)
        };
        response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        return response;
    }
}
