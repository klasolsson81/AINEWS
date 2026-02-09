using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NewsRoom.Core.Enums;
using NewsRoom.Infrastructure.Mocks;

namespace NewsRoom.Tests.Unit.Services;

public class MockNewsSourceTests
{
    private readonly MockNewsSource _sut;

    public MockNewsSourceTests()
    {
        _sut = new MockNewsSource(Mock.Of<ILogger<MockNewsSource>>());
    }

    [Fact]
    public async Task FetchArticlesAsync_ReturnsArticles_ForValidCategories()
    {
        // Act
        var articles = await _sut.FetchArticlesAsync(
            24,
            new[] { NewsCategory.Inrikes, NewsCategory.Utrikes, NewsCategory.Sport },
            5);

        // Assert
        articles.Should().NotBeEmpty();
        articles.Should().HaveCountLessThanOrEqualTo(5);
        articles.Should().OnlyContain(a =>
            a.Category == NewsCategory.Inrikes ||
            a.Category == NewsCategory.Utrikes ||
            a.Category == NewsCategory.Sport);
    }

    [Fact]
    public async Task FetchArticlesAsync_RespectsMaxArticles()
    {
        // Act
        var articles = await _sut.FetchArticlesAsync(
            24,
            new[] { NewsCategory.Inrikes, NewsCategory.Utrikes, NewsCategory.Sport,
                    NewsCategory.Ekonomi, NewsCategory.Teknik, NewsCategory.Politik, NewsCategory.Kultur },
            3);

        // Assert
        articles.Should().HaveCountLessThanOrEqualTo(3);
    }

    [Fact]
    public async Task FetchArticlesAsync_ReturnsEmpty_ForUnmatchedCategory()
    {
        // Act
        var articles = await _sut.FetchArticlesAsync(
            24,
            new[] { NewsCategory.Vader },
            10);

        // Assert - no mock articles have Vader category
        articles.Should().BeEmpty();
    }

    [Fact]
    public async Task FetchArticlesAsync_AllArticlesHaveRequiredFields()
    {
        // Act
        var articles = await _sut.FetchArticlesAsync(
            24,
            Enum.GetValues<NewsCategory>(),
            10);

        // Assert
        articles.Should().OnlyContain(a =>
            !string.IsNullOrEmpty(a.Id) &&
            !string.IsNullOrEmpty(a.Title) &&
            !string.IsNullOrEmpty(a.Summary) &&
            !string.IsNullOrEmpty(a.SourceName) &&
            !string.IsNullOrEmpty(a.SourceUrl));
    }
}
