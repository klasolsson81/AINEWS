using Microsoft.Extensions.Logging;
using NewsRoom.Core.Enums;
using NewsRoom.Core.Interfaces;
using NewsRoom.Core.Models;

namespace NewsRoom.Infrastructure.Mocks;

public class MockNewsSource : INewsSource
{
    private readonly ILogger<MockNewsSource> _logger;

    public MockNewsSource(ILogger<MockNewsSource> logger)
    {
        _logger = logger;
    }

    public Task<IReadOnlyList<NewsArticle>> FetchArticlesAsync(
        int timePeriodHours,
        IEnumerable<NewsCategory> categories,
        int maxArticles,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MockNewsSource: Fetching {MaxArticles} articles for period {Hours}h", maxArticles, timePeriodHours);

        var categoryList = categories.ToList();
        var articles = GetMockArticles()
            .Where(a => categoryList.Contains(a.Category))
            .Take(maxArticles)
            .ToList();

        _logger.LogInformation("MockNewsSource: Returning {Count} mock articles", articles.Count);
        return Task.FromResult<IReadOnlyList<NewsArticle>>(articles);
    }

    private static List<NewsArticle> GetMockArticles() => new()
    {
        new NewsArticle
        {
            Id = "mock-001",
            Title = "Kraftiga översvämningar drabbar Västsverige",
            Summary = "Räddningstjänsten har under natten fått in över tvåhundra larm om översvämningar i Göteborgsregionen.",
            Content = "Under det senaste dygnet har kraftiga skyfall orsakat stora översvämningar i Göteborgsregionen. Räddningstjänsten har fått in över 200 larm och flera vägar har stängts av.",
            SourceName = "SVT Nyheter",
            SourceUrl = "https://www.svt.se/nyheter/lokalt/vast/oversvamningar-goteborg",
            ImageUrl = "https://www.svt.se/image/wide/992/oversvamning.jpg",
            Category = NewsCategory.Inrikes,
            PublishedAt = DateTime.UtcNow.AddHours(-2)
        },
        new NewsArticle
        {
            Id = "mock-002",
            Title = "EU skärper reglerna för artificiell intelligens",
            Summary = "EU-parlamentet har röstat igenom nya skärpta regler för AI-system.",
            Content = "EU-parlamentet har med bred majoritet antagit nya regler för artificiell intelligens. Reglerna innebär krav på transparens för AI-genererat innehåll.",
            SourceName = "Dagens Nyheter",
            SourceUrl = "https://www.dn.se/varlden/eu-skarper-ai-regler",
            ImageUrl = "https://www.dn.se/images/eu-parlament-ai.jpg",
            Category = NewsCategory.Utrikes,
            PublishedAt = DateTime.UtcNow.AddHours(-4)
        },
        new NewsArticle
        {
            Id = "mock-003",
            Title = "Malmö FF vinner svenska cupen",
            Summary = "Malmö FF besegrade AIK med 3-2 i en dramatisk cupfinal.",
            Content = "Malmö FF tog hem svenska cupen efter en rafflande final mot AIK. Matchen slutade 3-2.",
            SourceName = "Expressen",
            SourceUrl = "https://www.expressen.se/sport/fotboll/malmo-ff-vinner-cupen",
            ImageUrl = "https://www.expressen.se/images/malmo-cup.jpg",
            Category = NewsCategory.Sport,
            PublishedAt = DateTime.UtcNow.AddHours(-1)
        },
        new NewsArticle
        {
            Id = "mock-004",
            Title = "Riksbanken sänker styrräntan till 2,5 procent",
            Summary = "Riksbanken meddelar att styrräntan sänks med 0,25 procentenheter.",
            Content = "Riksbanken har beslutat att sänka reporäntan med 0,25 procentenheter till 2,5 procent.",
            SourceName = "SVT Nyheter",
            SourceUrl = "https://www.svt.se/nyheter/ekonomi/riksbanken-sanker-rantan",
            ImageUrl = "https://www.svt.se/image/wide/992/riksbanken.jpg",
            Category = NewsCategory.Ekonomi,
            PublishedAt = DateTime.UtcNow.AddHours(-3)
        },
        new NewsArticle
        {
            Id = "mock-005",
            Title = "Ny cybersäkerhetsattack mot svenska myndigheter",
            Summary = "Flera svenska myndigheter har drabbats av en koordinerad cyberattack.",
            Content = "MSB bekräftar att flera svenska myndigheter utsatts för en koordinerad cyberattack under natten.",
            SourceName = "Sveriges Radio",
            SourceUrl = "https://sverigesradio.se/artikel/cyberattack",
            Category = NewsCategory.Teknik,
            PublishedAt = DateTime.UtcNow.AddHours(-5)
        },
        new NewsArticle
        {
            Id = "mock-006",
            Title = "Kulturministern presenterar ny filmsatsning",
            Summary = "Regeringen satsar 200 miljoner kronor på svensk filmproduktion.",
            Content = "Kulturministern presenterade en ny satsning på svensk film. Totalt 200 miljoner kronor fördelas under tre år.",
            SourceName = "Dagens Nyheter",
            SourceUrl = "https://www.dn.se/kultur-noje/ny-filmsatsning",
            ImageUrl = "https://www.dn.se/images/film.jpg",
            Category = NewsCategory.Kultur,
            PublishedAt = DateTime.UtcNow.AddHours(-6)
        },
        new NewsArticle
        {
            Id = "mock-007",
            Title = "Regeringen presenterar ny energipolitik",
            Summary = "Energiministern lägger fram förslag om utbyggd kärnkraft och vindkraft.",
            Content = "Regeringen har presenterat en ny energipolitisk strategi med fokus på kärnkraft och vindkraft.",
            SourceName = "SVT Nyheter",
            SourceUrl = "https://www.svt.se/nyheter/inrikes/ny-energipolitik",
            ImageUrl = "https://www.svt.se/image/wide/992/energi.jpg",
            Category = NewsCategory.Politik,
            PublishedAt = DateTime.UtcNow.AddHours(-7)
        }
    };
}
