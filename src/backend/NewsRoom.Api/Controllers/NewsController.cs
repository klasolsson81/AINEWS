using Microsoft.AspNetCore.Mvc;
using NewsRoom.Core.DTOs;
using NewsRoom.Core.Enums;
using NewsRoom.Core.Interfaces;

namespace NewsRoom.Api.Controllers;

/// <summary>
/// Controller for previewing news articles before generating a broadcast.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class NewsController : ControllerBase
{
    private readonly INewsSource _newsSource;
    private readonly ILogger<NewsController> _logger;

    public NewsController(
        INewsSource newsSource,
        ILogger<NewsController> logger)
    {
        _newsSource = newsSource;
        _logger = logger;
    }

    /// <summary>
    /// Fetch available news articles matching the specified filters.
    /// This endpoint allows the frontend to preview which articles will
    /// be included in a broadcast before starting generation.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<NewsArticleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<NewsArticleDto>>> GetArticlesAsync(
        [FromQuery] int timePeriodHours = 24,
        [FromQuery] string? categories = null,
        [FromQuery] int maxArticles = 7,
        CancellationToken cancellationToken = default)
    {
        // Parse categories from comma-separated string
        var categoryList = ParseCategories(categories);

        if (categoryList.Count == 0)
        {
            return BadRequest(new { error = "Minst en giltig kategori krävs." });
        }

        _logger.LogInformation(
            "GetArticles: TimePeriod={Hours}h, Categories=[{Categories}], Max={Max}",
            timePeriodHours,
            string.Join(", ", categoryList),
            maxArticles);

        var articles = await _newsSource.FetchArticlesAsync(
            timePeriodHours,
            categoryList,
            maxArticles,
            cancellationToken);

        var result = articles.Select(a => new NewsArticleDto
        {
            Id = a.Id,
            Title = a.Title,
            Summary = a.Summary,
            SourceName = a.SourceName,
            SourceUrl = a.SourceUrl,
            ImageUrl = a.ImageUrl,
            Category = a.Category.ToString(),
            PublishedAt = a.PublishedAt
        });

        return Ok(result);
    }

    /// <summary>
    /// Get the list of available news categories.
    /// </summary>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<string>> GetCategories()
    {
        var categories = Enum.GetNames<NewsCategory>();
        return Ok(categories);
    }

    /// <summary>
    /// Parse a comma-separated categories string into a list of NewsCategory values.
    /// If null or empty, returns all categories as default.
    /// </summary>
    private static List<NewsCategory> ParseCategories(string? categories)
    {
        if (string.IsNullOrWhiteSpace(categories))
        {
            return Enum.GetValues<NewsCategory>().ToList();
        }

        return categories
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(c => Enum.TryParse<NewsCategory>(c, ignoreCase: true, out var cat) ? cat : (NewsCategory?)null)
            .Where(c => c.HasValue)
            .Select(c => c!.Value)
            .ToList();
    }
}
