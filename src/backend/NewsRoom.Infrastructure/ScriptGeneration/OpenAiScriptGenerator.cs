using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NewsRoom.Core.Enums;
using NewsRoom.Core.Exceptions;
using NewsRoom.Core.Interfaces;
using NewsRoom.Core.Models;

namespace NewsRoom.Infrastructure.ScriptGeneration;

public class OpenAiScriptGenerator : IScriptGenerator
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAiScriptGenerator> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    public OpenAiScriptGenerator(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<OpenAiScriptGenerator> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<BroadcastScript> GenerateScriptAsync(
        IReadOnlyList<NewsArticle> articles,
        CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["LLM_OPENAI_API_KEY"]
            ?? Environment.GetEnvironmentVariable("LLM_OPENAI_API_KEY");

        if (string.IsNullOrEmpty(apiKey))
            throw new ScriptGenerationException("OpenAI API key not configured. Set LLM_OPENAI_API_KEY.");

        _logger.LogInformation("OpenAI: Generating script for {Count} articles", articles.Count);

        var systemPrompt = BuildSystemPrompt();
        var userPrompt = BuildUserPrompt(articles);

        var requestBody = new
        {
            model = "gpt-4o",
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = 0.7,
            max_tokens = 8000,
            response_format = new { type = "json_object" }
        };

        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(120);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("OpenAI API error: {Status} - {Body}", response.StatusCode, errorBody);
            throw new ScriptGenerationException($"OpenAI API returned {response.StatusCode}");
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var apiResponse = JsonSerializer.Deserialize<OpenAiResponse>(responseBody);
        var scriptJson = apiResponse?.Choices?.FirstOrDefault()?.Message?.Content;

        if (string.IsNullOrEmpty(scriptJson))
            throw new ScriptGenerationException("OpenAI returned empty response");

        _logger.LogDebug("OpenAI raw response: {Response}", scriptJson);

        var script = ParseScript(scriptJson, articles);

        _logger.LogInformation("OpenAI: Generated script with {Segments} segments, est. {Duration}s",
            script.TotalSegments, script.EstimatedDurationSeconds);

        return script;
    }

    private static string BuildSystemPrompt()
    {
        return """
            Du är en erfaren svensk nyhetsredaktör och manusförfattare för TV-nyheter.
            Du skriver manus för en AI-genererad nyhetssändning i stil med SVT Rapport eller TV4 Nyheterna.

            REGLER:
            1. Skriv ALLTID på naturlig svensk nyhetssvenska — inte översatt engelska.
            2. Använd rätt ton per nyhetstyp (allvarlig för krig/katastrofer, neutral för politik, lättsam för kultur/nöje).
            3. Varje segment ska ha: anchor_intro (10-15s), broll_voiceover (30-60s), anchor_outro (5-10s).
            4. Total sändningstid ska vara MINST 5 minuter (300 sekunder).
            5. Inkludera 5-10 segment.
            6. Attributera ALLTID källa (source) korrekt.
            7. ALDRIG fabricera fakta — omskriv befintliga nyheter med egna ord.
            8. visual_content ska ha rätt strategi per nyhetstyp:
               - Namngivna personer/platser → editorial_image (OG-bild från artikeln)
               - Ekonomi/börs/val → generated_graphic (diagram, grafer)
               - Väder/geografi → generated_map + stock_footage
               - Teknik/AI/cyber → ai_generated_image (konceptuella illustrationer)
               - Generella samhällsnyheter → stock_footage

            VISUELL BESLUTSMATRIS:
            - NIVÅ 1 (editorial_image): För verkliga personer, specifika platser, sportevenemang
            - NIVÅ 2 (generated_map, generated_graphic): För väder, ekonomi, val, geopolitik
            - NIVÅ 3 (stock_footage): För sjukvård, utbildning, infrastruktur
            - NIVÅ 4 (ai_generated_image): För teknik, AI, cyber, abstrakta ämnen
            - NIVÅ 5 (mix): De flesta nyheter kombinerar flera nivåer

            AI-BILDER FÅR ALDRIG:
            - Föreställa namngivna verkliga personer
            - Visa specifika verkliga byggnader
            - Simulera specifika verkliga händelser

            Svara ALLTID med valid JSON som matchar detta schema exakt.
            """;
    }

    private static string BuildUserPrompt(IReadOnlyList<NewsArticle> articles)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Generera ett komplett nyhetsmanus baserat på följande artiklar:");
        sb.AppendLine();

        for (int i = 0; i < articles.Count; i++)
        {
            var a = articles[i];
            sb.AppendLine($"ARTIKEL {i + 1}:");
            sb.AppendLine($"  Rubrik: {a.Title}");
            sb.AppendLine($"  Sammanfattning: {a.Summary}");
            sb.AppendLine($"  Innehåll: {a.Content}");
            sb.AppendLine($"  Källa: {a.SourceName}");
            sb.AppendLine($"  URL: {a.SourceUrl}");
            sb.AppendLine($"  Bild-URL: {a.ImageUrl ?? "saknas"}");
            sb.AppendLine($"  Kategori: {a.Category}");
            sb.AppendLine($"  Publicerad: {a.PublishedAt:yyyy-MM-dd HH:mm}");
            sb.AppendLine();
        }

        sb.AppendLine("""
            Svara med JSON i detta format:
            {
              "broadcast_id": "uuid",
              "language": "sv-SE",
              "total_segments": <antal>,
              "estimated_duration_seconds": <minst 300>,
              "intro": {
                "anchor_text": "<välkomstfras>",
                "tone": "warm, professional"
              },
              "segments": [
                {
                  "segment_number": 1,
                  "category": "<kategori>",
                  "headline": "<rubrik>",
                  "source": "<källnamn>",
                  "source_url": "<url>",
                  "priority": "top_story|major|standard|light",
                  "anchor_intro": { "text": "<text>", "tone": "<ton>", "estimated_seconds": <10-15> },
                  "broll_voiceover": { "text": "<text>", "tone": "<ton>", "estimated_seconds": <30-60> },
                  "visual_content": {
                    "type": "<nyhetstyp>",
                    "visual_strategy": "<strategi>",
                    "scenes": [
                      { "description": "<beskrivning>", "type": "editorial_image|generated_map|stock_footage|ai_generated_image|generated_graphic", "duration_seconds": 8, "prompt": "<om ai>", "search_terms": ["<om stock>"] }
                    ]
                  },
                  "anchor_outro": { "text": "<text>", "tone": "transitional", "estimated_seconds": <4-8> },
                  "lower_third": { "title": "<VERSALER>", "subtitle": "<kort text>" }
                }
              ],
              "outro": {
                "anchor_text": "<avslutningsfras>",
                "tone": "warm, closing"
              }
            }
            """);

        return sb.ToString();
    }

    private BroadcastScript ParseScript(string json, IReadOnlyList<NewsArticle> articles)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var script = new BroadcastScript
            {
                BroadcastId = root.TryGetProperty("broadcast_id", out var bid) ? bid.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString(),
                Language = root.TryGetProperty("language", out var lang) ? lang.GetString() ?? "sv-SE" : "sv-SE",
                TotalSegments = root.TryGetProperty("total_segments", out var ts) ? ts.GetInt32() : 0,
                EstimatedDurationSeconds = root.TryGetProperty("estimated_duration_seconds", out var ed) ? ed.GetInt32() : 0,
            };

            if (root.TryGetProperty("intro", out var intro))
            {
                script.Intro = new ScriptIntro
                {
                    AnchorText = intro.TryGetProperty("anchor_text", out var at) ? at.GetString() ?? "" : "",
                    Tone = intro.TryGetProperty("tone", out var t) ? t.GetString() ?? "" : "warm, professional",
                };
            }

            if (root.TryGetProperty("segments", out var segments))
            {
                foreach (var seg in segments.EnumerateArray())
                {
                    var segment = new ScriptSegment
                    {
                        SegmentNumber = seg.TryGetProperty("segment_number", out var sn) ? sn.GetInt32() : 0,
                        Headline = seg.TryGetProperty("headline", out var hl) ? hl.GetString() ?? "" : "",
                        Source = seg.TryGetProperty("source", out var src) ? src.GetString() ?? "" : "",
                        SourceUrl = seg.TryGetProperty("source_url", out var su) ? su.GetString() ?? "" : "",
                        Category = ParseCategory(seg.TryGetProperty("category", out var cat) ? cat.GetString() : null),
                        Priority = ParsePriority(seg.TryGetProperty("priority", out var pr) ? pr.GetString() : null),
                    };

                    if (seg.TryGetProperty("anchor_intro", out var ai))
                        segment.AnchorIntro = ParseAnchorSection(ai);
                    if (seg.TryGetProperty("broll_voiceover", out var bv))
                        segment.BRollVoiceover = ParseVoiceoverSection(bv);
                    if (seg.TryGetProperty("anchor_outro", out var ao))
                        segment.AnchorOutro = ParseAnchorSection(ao);
                    if (seg.TryGetProperty("visual_content", out var vc))
                        segment.VisualContent = ParseVisualContent(vc);
                    if (seg.TryGetProperty("lower_third", out var lt))
                        segment.LowerThird = new LowerThird
                        {
                            Title = lt.TryGetProperty("title", out var ltTitle) ? ltTitle.GetString() ?? "" : "",
                            Subtitle = lt.TryGetProperty("subtitle", out var ltSub) ? ltSub.GetString() ?? "" : "",
                        };

                    script.Segments.Add(segment);
                }
            }

            script.TotalSegments = script.Segments.Count;

            if (root.TryGetProperty("outro", out var outro))
            {
                script.Outro = new ScriptOutro
                {
                    AnchorText = outro.TryGetProperty("anchor_text", out var oat) ? oat.GetString() ?? "" : "",
                    Tone = outro.TryGetProperty("tone", out var ot) ? ot.GetString() ?? "" : "warm, closing",
                };
            }

            return script;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse script JSON: {Json}", json[..Math.Min(500, json.Length)]);
            throw new ScriptGenerationException("Failed to parse generated script JSON", ex);
        }
    }

    private static AnchorSection ParseAnchorSection(JsonElement el) => new()
    {
        Text = el.TryGetProperty("text", out var t) ? t.GetString() ?? "" : "",
        Tone = el.TryGetProperty("tone", out var to) ? to.GetString() ?? "" : "",
        EstimatedSeconds = el.TryGetProperty("estimated_seconds", out var es) ? es.GetInt32() : 10,
    };

    private static VoiceoverSection ParseVoiceoverSection(JsonElement el) => new()
    {
        Text = el.TryGetProperty("text", out var t) ? t.GetString() ?? "" : "",
        Tone = el.TryGetProperty("tone", out var to) ? to.GetString() ?? "" : "",
        EstimatedSeconds = el.TryGetProperty("estimated_seconds", out var es) ? es.GetInt32() : 30,
    };

    private static VisualContent ParseVisualContent(JsonElement el)
    {
        var vc = new VisualContent
        {
            Type = el.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "",
        };

        if (el.TryGetProperty("visual_strategy", out var vs))
        {
            var stratStr = vs.GetString()?.ToLowerInvariant().Replace("_", "").Replace(" ", "") ?? "";
            vc.VisualStrategy = stratStr switch
            {
                "editorialimagesonly" => VisualStrategy.EditorialImagesOnly,
                "editorialimageswithmaps" => VisualStrategy.EditorialImagesWithMaps,
                "mapsanddatagraphics" => VisualStrategy.MapsAndDataGraphics,
                "stockfootage" => VisualStrategy.StockFootage,
                "aigeneratedillustrations" => VisualStrategy.AiGeneratedIllustrations,
                _ => VisualStrategy.Combined,
            };
        }

        if (el.TryGetProperty("scenes", out var scenes))
        {
            foreach (var scene in scenes.EnumerateArray())
            {
                var s = new VisualScene
                {
                    Description = scene.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "",
                    DurationSeconds = scene.TryGetProperty("duration_seconds", out var ds) ? ds.GetInt32() : 8,
                    Prompt = scene.TryGetProperty("prompt", out var p) ? p.GetString() : null,
                    SourceHint = scene.TryGetProperty("source_hint", out var sh) ? sh.GetString() : null,
                    DataHint = scene.TryGetProperty("data_hint", out var dh) ? dh.GetString() : null,
                };

                if (scene.TryGetProperty("type", out var st))
                {
                    var typeStr = st.GetString()?.ToLowerInvariant().Replace("_", "") ?? "";
                    s.Type = typeStr switch
                    {
                        "editorialimage" => VisualContentType.EditorialImage,
                        "generatedmap" => VisualContentType.GeneratedMap,
                        "stockfootage" => VisualContentType.StockFootage,
                        "aigeneratedimage" => VisualContentType.AiGeneratedImage,
                        "generatedgraphic" => VisualContentType.GeneratedGraphic,
                        _ => VisualContentType.StockFootage,
                    };
                }

                if (scene.TryGetProperty("search_terms", out var terms) && terms.ValueKind == JsonValueKind.Array)
                {
                    s.SearchTerms = terms.EnumerateArray()
                        .Select(term => term.GetString() ?? "")
                        .Where(term => !string.IsNullOrEmpty(term))
                        .ToList();
                }

                vc.Scenes.Add(s);
            }
        }

        return vc;
    }

    private static NewsCategory ParseCategory(string? cat)
    {
        if (string.IsNullOrEmpty(cat)) return NewsCategory.Inrikes;
        return Enum.TryParse<NewsCategory>(cat, true, out var result) ? result : NewsCategory.Inrikes;
    }

    private static SegmentPriority ParsePriority(string? priority)
    {
        var p = priority?.ToLowerInvariant().Replace("_", "") ?? "";
        return p switch
        {
            "topstory" => SegmentPriority.TopStory,
            "major" => SegmentPriority.Major,
            "standard" => SegmentPriority.Standard,
            "light" => SegmentPriority.Light,
            "closing" => SegmentPriority.Closing,
            _ => SegmentPriority.Standard,
        };
    }

    // OpenAI response models
    private class OpenAiResponse
    {
        [JsonPropertyName("choices")]
        public List<Choice>? Choices { get; set; }
    }

    private class Choice
    {
        [JsonPropertyName("message")]
        public Message? Message { get; set; }
    }

    private class Message
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}
