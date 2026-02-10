using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using NewsRoom.Core.Interfaces;

namespace NewsRoom.Infrastructure.Images;

public class OgImageExtractor : IEditorialImageExtractor
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OgImageExtractor> _logger;

    public OgImageExtractor(IHttpClientFactory httpClientFactory, ILogger<OgImageExtractor> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string?> ExtractImageUrlAsync(string articleUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("NewsRoomAI/1.0 (Portfolio Project)");

            var response = await client.GetAsync(articleUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("OG extractor: HTTP {Status} for {Url}", response.StatusCode, articleUrl);
                return null;
            }

            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Fallback chain: og:image -> twitter:image -> first img
            var imageUrl = ExtractMetaContent(doc, "og:image")
                ?? ExtractMetaContent(doc, "twitter:image")
                ?? ExtractFirstImage(doc);

            if (!string.IsNullOrEmpty(imageUrl))
            {
                // Make relative URLs absolute
                if (imageUrl.StartsWith("//"))
                    imageUrl = "https:" + imageUrl;
                else if (imageUrl.StartsWith("/"))
                {
                    var uri = new Uri(articleUrl);
                    imageUrl = $"{uri.Scheme}://{uri.Host}{imageUrl}";
                }

                _logger.LogDebug("OG extractor: Found image {ImageUrl} for {ArticleUrl}", imageUrl, articleUrl);
            }

            return imageUrl;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "OG extractor: Error extracting from {Url}", articleUrl);
            return null;
        }
    }

    private static string? ExtractMetaContent(HtmlDocument doc, string property)
    {
        var node = doc.DocumentNode.SelectSingleNode($"//meta[@property='{property}']")
            ?? doc.DocumentNode.SelectSingleNode($"//meta[@name='{property}']");
        var value = node?.GetAttributeValue("content", string.Empty);
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static string? ExtractFirstImage(HtmlDocument doc)
    {
        var img = doc.DocumentNode.SelectSingleNode("//article//img[@src]")
            ?? doc.DocumentNode.SelectSingleNode("//main//img[@src]");
        var value = img?.GetAttributeValue("src", string.Empty);
        return string.IsNullOrEmpty(value) ? null : value;
    }
}
