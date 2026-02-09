using Microsoft.Extensions.Logging;
using NewsRoom.Core.Enums;
using NewsRoom.Core.Interfaces;
using NewsRoom.Core.Models;

namespace NewsRoom.Infrastructure.Mocks;

public class MockScriptGenerator : IScriptGenerator
{
    private readonly ILogger<MockScriptGenerator> _logger;

    public MockScriptGenerator(ILogger<MockScriptGenerator> logger)
    {
        _logger = logger;
    }

    public Task<BroadcastScript> GenerateScriptAsync(
        IReadOnlyList<NewsArticle> articles,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MockScriptGenerator: Generating script for {Count} articles", articles.Count);

        var script = new BroadcastScript
        {
            BroadcastId = Guid.NewGuid().ToString(),
            GeneratedAt = DateTime.UtcNow,
            TotalSegments = articles.Count,
            EstimatedDurationSeconds = 60 + (articles.Count * 60) + 30,
            Intro = new ScriptIntro
            {
                AnchorText = "God kväll och välkommen till Nyhetskollen. Jag heter Anna Lindström. Ikväll tar vi en titt på de senaste händelserna.",
                Tone = "warm, professional, welcoming"
            },
            Outro = new ScriptOutro
            {
                AnchorText = "Det var allt för ikväll. Tack för att ni tittade. Vi ses imorgon. God natt.",
                Tone = "warm, closing"
            }
        };

        for (int i = 0; i < articles.Count; i++)
        {
            var article = articles[i];
            var priority = i == 0 ? SegmentPriority.TopStory :
                          i < 3 ? SegmentPriority.Major :
                          i < articles.Count - 1 ? SegmentPriority.Standard :
                          SegmentPriority.Light;

            script.Segments.Add(new ScriptSegment
            {
                SegmentNumber = i + 1,
                Category = article.Category,
                Headline = article.Title,
                Source = article.SourceName,
                SourceUrl = article.SourceUrl,
                Priority = priority,
                AnchorIntro = new AnchorSection
                {
                    Text = $"Vi går vidare till nästa nyhet. {article.Summary}",
                    Tone = GetToneForCategory(article.Category),
                    EstimatedSeconds = 12
                },
                BRollVoiceover = new VoiceoverSection
                {
                    Text = article.Content,
                    Tone = GetToneForCategory(article.Category),
                    EstimatedSeconds = 35
                },
                VisualContent = new VisualContent
                {
                    Type = "mock",
                    VisualStrategy = GetStrategyForCategory(article.Category),
                    Scenes = new List<VisualScene>
                    {
                        new()
                        {
                            Description = $"Illustrativ bild för: {article.Title}",
                            Type = VisualContentType.StockFootage,
                            DurationSeconds = 8,
                            SearchTerms = new List<string> { article.Category.ToString().ToLower() }
                        },
                        new()
                        {
                            Description = $"Kompletterande bild för: {article.Title}",
                            Type = VisualContentType.AiGeneratedImage,
                            DurationSeconds = 8,
                            Prompt = $"Editorial illustration of {article.Category}, conceptual, magazine style"
                        }
                    }
                },
                AnchorOutro = new AnchorSection
                {
                    Text = i < articles.Count - 1 ? "Vi går vidare." : "Och det leder oss till avslutningen av dagens sändning.",
                    Tone = "transitional",
                    EstimatedSeconds = 4
                },
                LowerThird = new LowerThird
                {
                    Title = article.Title.ToUpperInvariant(),
                    Subtitle = article.Summary.Length > 60 ? article.Summary[..60] + "..." : article.Summary
                }
            });
        }

        _logger.LogInformation("MockScriptGenerator: Generated script with {Segments} segments, estimated {Duration}s",
            script.TotalSegments, script.EstimatedDurationSeconds);

        return Task.FromResult(script);
    }

    private static string GetToneForCategory(NewsCategory category) => category switch
    {
        NewsCategory.Inrikes => "serious, informative",
        NewsCategory.Utrikes => "serious, concerned",
        NewsCategory.Sport => "enthusiastic, energetic",
        NewsCategory.Politik => "neutral, analytical",
        NewsCategory.Noje => "light, entertaining",
        NewsCategory.Ekonomi => "neutral, analytical",
        NewsCategory.Teknik => "curious, informative",
        NewsCategory.Vader => "informative, practical",
        NewsCategory.Kultur => "warm, interested",
        _ => "neutral, professional"
    };

    private static VisualStrategy GetStrategyForCategory(NewsCategory category) => category switch
    {
        NewsCategory.Ekonomi => VisualStrategy.MapsAndDataGraphics,
        NewsCategory.Teknik => VisualStrategy.AiGeneratedIllustrations,
        NewsCategory.Vader => VisualStrategy.Combined,
        _ => VisualStrategy.EditorialImagesWithMaps
    };
}
