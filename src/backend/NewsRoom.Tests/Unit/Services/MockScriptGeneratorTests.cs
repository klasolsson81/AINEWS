using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NewsRoom.Core.Enums;
using NewsRoom.Core.Models;
using NewsRoom.Infrastructure.Mocks;

namespace NewsRoom.Tests.Unit.Services;

public class MockScriptGeneratorTests
{
    private readonly MockScriptGenerator _sut;

    public MockScriptGeneratorTests()
    {
        _sut = new MockScriptGenerator(Mock.Of<ILogger<MockScriptGenerator>>());
    }

    [Fact]
    public async Task GenerateScriptAsync_ReturnsScript_WithCorrectSegmentCount()
    {
        // Arrange
        var articles = CreateMockArticles(5);

        // Act
        var script = await _sut.GenerateScriptAsync(articles);

        // Assert
        script.Should().NotBeNull();
        script.TotalSegments.Should().Be(5);
        script.Segments.Should().HaveCount(5);
    }

    [Fact]
    public async Task GenerateScriptAsync_HasIntroAndOutro()
    {
        // Arrange
        var articles = CreateMockArticles(3);

        // Act
        var script = await _sut.GenerateScriptAsync(articles);

        // Assert
        script.Intro.Should().NotBeNull();
        script.Intro.AnchorText.Should().NotBeNullOrEmpty();
        script.Outro.Should().NotBeNull();
        script.Outro.AnchorText.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateScriptAsync_EstimatedDuration_IsAtLeast5Minutes()
    {
        // Arrange
        var articles = CreateMockArticles(5);

        // Act
        var script = await _sut.GenerateScriptAsync(articles);

        // Assert
        script.EstimatedDurationSeconds.Should().BeGreaterThanOrEqualTo(300,
            "en nyhetssändning måste vara minst 5 minuter lång");
    }

    [Fact]
    public async Task GenerateScriptAsync_EachSegment_HasRequiredSections()
    {
        // Arrange
        var articles = CreateMockArticles(3);

        // Act
        var script = await _sut.GenerateScriptAsync(articles);

        // Assert
        foreach (var segment in script.Segments)
        {
            segment.Headline.Should().NotBeNullOrEmpty();
            segment.AnchorIntro.Text.Should().NotBeNullOrEmpty();
            segment.AnchorIntro.EstimatedSeconds.Should().BeGreaterThan(0);
            segment.BRollVoiceover.Text.Should().NotBeNullOrEmpty();
            segment.BRollVoiceover.EstimatedSeconds.Should().BeGreaterThan(0);
            segment.AnchorOutro.Text.Should().NotBeNullOrEmpty();
            segment.VisualContent.Scenes.Should().NotBeEmpty();
            segment.LowerThird.Title.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task GenerateScriptAsync_FirstSegment_IsTopStory()
    {
        // Arrange
        var articles = CreateMockArticles(5);

        // Act
        var script = await _sut.GenerateScriptAsync(articles);

        // Assert
        script.Segments.First().Priority.Should().Be(SegmentPriority.TopStory);
    }

    [Fact]
    public async Task GenerateScriptAsync_SegmentsHaveSequentialNumbers()
    {
        // Arrange
        var articles = CreateMockArticles(5);

        // Act
        var script = await _sut.GenerateScriptAsync(articles);

        // Assert
        for (int i = 0; i < script.Segments.Count; i++)
        {
            script.Segments[i].SegmentNumber.Should().Be(i + 1);
        }
    }

    private static IReadOnlyList<NewsArticle> CreateMockArticles(int count)
    {
        return Enumerable.Range(1, count).Select(i => new NewsArticle
        {
            Id = $"test-{i}",
            Title = $"Testnyhet nummer {i}",
            Summary = $"Sammanfattning av testnyhet {i}. En kort beskrivning av händelsen.",
            Content = $"Detaljerat innehåll för testnyhet {i}. Här beskrivs händelsen mer utförligt med alla viktiga detaljer.",
            SourceName = "Test Nyheter",
            SourceUrl = $"https://test.se/nyhet/{i}",
            Category = (NewsCategory)(i % 7),
            PublishedAt = DateTime.UtcNow.AddHours(-i)
        }).ToList();
    }
}
