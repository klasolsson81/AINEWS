using System.ServiceModel.Syndication;
using System.Xml;
using Microsoft.Extensions.Logging;
using NewsRoom.Core.Enums;
using NewsRoom.Core.Interfaces;
using NewsRoom.Core.Models;

namespace NewsRoom.Infrastructure.News;

public class RssNewsSource : INewsSource
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IEditorialImageExtractor _imageExtractor;
    private readonly ILogger<RssNewsSource> _logger;

    private static readonly Dictionary<string, RssFeedConfig> Feeds = new()
    {
        ["svt"] = new("https://www.svt.se/nyheter/rss.xml", "SVT Nyheter", new Dictionary<string, NewsCategory>
        {
            ["inrikes"] = NewsCategory.Inrikes,
            ["utrikes"] = NewsCategory.Utrikes,
            ["ekonomi"] = NewsCategory.Ekonomi,
            ["sport"] = NewsCategory.Sport,
            ["kultur"] = NewsCategory.Kultur,
            ["vetenskap"] = NewsCategory.Teknik,
        }),
        ["sr"] = new("https://api.sr.se/api/rss/program/83", "Sveriges Radio Ekot", new Dictionary<string, NewsCategory>
        {
            ["inrikes"] = NewsCategory.Inrikes,
        }),
        ["dn"] = new("https://www.dn.se/rss/senaste-nytt/", "Dagens Nyheter", new Dictionary<string, NewsCategory>
        {
            ["sverige"] = NewsCategory.Inrikes,
            ["varlden"] = NewsCategory.Utrikes,
            ["ekonomi"] = NewsCategory.Ekonomi,
            ["sport"] = NewsCategory.Sport,
            ["kultur"] = NewsCategory.Kultur,
        }),
        ["expressen"] = new("https://feeds.expressen.se/nyheter/", "Expressen", new Dictionary<string, NewsCategory>
        {
            ["nyheter"] = NewsCategory.Inrikes,
        }),
    };

    public RssNewsSource(
        IHttpClientFactory httpClientFactory,
        IEditorialImageExtractor imageExtractor,
        ILogger<RssNewsSource> logger)
    {
        _httpClientFactory = httpClientFactory;
        _imageExtractor = imageExtractor;
        _logger = logger;
    }

    public async Task<IReadOnlyList<NewsArticle>> FetchArticlesAsync(
        int timePeriodHours,
        IEnumerable<NewsCategory> categories,
        int maxArticles,
        CancellationToken cancellationToken = default)
    {
        var categoryList = categories.ToHashSet();
        var cutoffTime = DateTime.UtcNow.AddHours(-timePeriodHours);
        var allArticles = new List<NewsArticle>();

        foreach (var (feedId, config) in Feeds)
        {
            try
            {
                var articles = await FetchFeedAsync(feedId, config, cutoffTime, categoryList, cancellationToken);
                allArticles.AddRange(articles);
                _logger.LogInformation("RSS: Fetched {Count} articles from {Source}", articles.Count, config.SourceName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RSS: Failed to fetch from {Source}, skipping", config.SourceName);
            }
        }

        // Deduplicate by title similarity, sort by date, take max
        var result = allArticles
            .DistinctBy(a => NormalizeTitle(a.Title))
            .OrderByDescending(a => a.PublishedAt)
            .Take(maxArticles)
            .ToList();

        _logger.LogInformation("RSS: Returning {Count} articles (from {Total} total) for period {Hours}h",
            result.Count, allArticles.Count, timePeriodHours);

        return result;
    }

    private async Task<List<NewsArticle>> FetchFeedAsync(
        string feedId,
        RssFeedConfig config,
        DateTime cutoffTime,
        HashSet<NewsCategory> requestedCategories,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(15);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("NewsRoomAI/1.0 (Portfolio Project)");

        var response = await client.GetAsync(config.FeedUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = XmlReader.Create(stream);
        var feed = SyndicationFeed.Load(reader);

        var articles = new List<NewsArticle>();

        foreach (var item in feed.Items)
        {
            var publishDate = item.PublishDate.UtcDateTime;
            if (publishDate < cutoffTime) continue;

            var category = CategorizeArticle(item, config);
            if (!requestedCategories.Contains(category)) continue;

            var articleUrl = item.Links.FirstOrDefault()?.Uri.ToString() ?? string.Empty;
            var imageUrl = ExtractImageFromFeedItem(item);

            // Try to get OG image if no image in feed
            if (string.IsNullOrEmpty(imageUrl) && !string.IsNullOrEmpty(articleUrl))
            {
                try
                {
                    imageUrl = await _imageExtractor.ExtractImageUrlAsync(articleUrl, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not extract OG image from {Url}", articleUrl);
                }
            }

            articles.Add(new NewsArticle
            {
                Title = CleanText(item.Title?.Text ?? string.Empty),
                Summary = CleanText(item.Summary?.Text ?? string.Empty),
                Content = CleanText(item.Summary?.Text ?? string.Empty),
                SourceName = config.SourceName,
                SourceUrl = articleUrl,
                ImageUrl = imageUrl,
                Category = category,
                PublishedAt = publishDate,
                FetchedAt = DateTime.UtcNow,
            });
        }

        return articles;
    }

    private static NewsCategory CategorizeArticle(SyndicationItem item, RssFeedConfig config)
    {
        // Check item categories
        foreach (var cat in item.Categories)
        {
            var catName = cat.Name?.ToLowerInvariant() ?? string.Empty;
            foreach (var (keyword, newsCategory) in config.CategoryMapping)
            {
                if (catName.Contains(keyword))
                    return newsCategory;
            }
        }

        // Check URL for category hints
        var url = item.Links.FirstOrDefault()?.Uri.ToString()?.ToLowerInvariant() ?? string.Empty;
        foreach (var (keyword, newsCategory) in config.CategoryMapping)
        {
            if (url.Contains(keyword))
                return newsCategory;
        }

        // Default to Inrikes for Swedish sources
        return NewsCategory.Inrikes;
    }

    private static string? ExtractImageFromFeedItem(SyndicationItem item)
    {
        // Check for media:content or enclosure
        foreach (var link in item.Links)
        {
            if (link.MediaType?.StartsWith("image/") == true)
                return link.Uri.ToString();
        }

        // Check for enclosure with image type
        foreach (var ext in item.ElementExtensions)
        {
            try
            {
                var element = ext.GetObject<XmlElement>();
                if (element == null) continue;

                // media:content url
                if (element.LocalName == "content" && element.NamespaceURI.Contains("media"))
                {
                    var url = element.GetAttribute("url");
                    if (!string.IsNullOrEmpty(url)) return url;
                }

                // media:thumbnail url
                if (element.LocalName == "thumbnail")
                {
                    var url = element.GetAttribute("url");
                    if (!string.IsNullOrEmpty(url)) return url;
                }

                // enclosure
                if (element.LocalName == "enclosure")
                {
                    var type = element.GetAttribute("type");
                    if (type.StartsWith("image/"))
                        return element.GetAttribute("url");
                }
            }
            catch
            {
                // Skip malformed extensions
            }
        }

        // Try to find image in HTML summary
        var summary = item.Summary?.Text ?? string.Empty;
        var imgStart = summary.IndexOf("src=\"", StringComparison.OrdinalIgnoreCase);
        if (imgStart > -1)
        {
            imgStart += 5;
            var imgEnd = summary.IndexOf('"', imgStart);
            if (imgEnd > imgStart)
                return summary[imgStart..imgEnd];
        }

        return null;
    }

    private static string CleanText(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // Remove HTML tags
        var cleaned = System.Text.RegularExpressions.Regex.Replace(text, "<[^>]+>", " ");
        // Decode HTML entities
        cleaned = System.Net.WebUtility.HtmlDecode(cleaned);
        // Normalize whitespace
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+", " ").Trim();
        return cleaned;
    }

    private static string NormalizeTitle(string title)
    {
        return title.ToLowerInvariant().Trim();
    }

    private record RssFeedConfig(string FeedUrl, string SourceName, Dictionary<string, NewsCategory> CategoryMapping);
}
