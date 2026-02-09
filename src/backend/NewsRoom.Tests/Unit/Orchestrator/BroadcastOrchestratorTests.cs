using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NewsRoom.Core.Enums;
using NewsRoom.Core.Interfaces;
using NewsRoom.Core.Models;
using NewsRoom.Infrastructure.Services;

namespace NewsRoom.Tests.Unit.Orchestrator;

public class BroadcastOrchestratorTests
{
    private readonly Mock<INewsSource> _newsSourceMock;
    private readonly Mock<IScriptGenerator> _scriptGeneratorMock;
    private readonly Mock<ITtsProvider> _ttsProviderMock;
    private readonly Mock<IAvatarGenerator> _avatarGeneratorMock;
    private readonly Mock<IBRollOrchestrator> _brollOrchestratorMock;
    private readonly Mock<IVideoComposer> _videoComposerMock;
    private readonly Mock<IBroadcastRepository> _repositoryMock;
    private readonly BroadcastOrchestratorService _sut;

    public BroadcastOrchestratorTests()
    {
        _newsSourceMock = new Mock<INewsSource>();
        _scriptGeneratorMock = new Mock<IScriptGenerator>();
        _ttsProviderMock = new Mock<ITtsProvider>();
        _avatarGeneratorMock = new Mock<IAvatarGenerator>();
        _brollOrchestratorMock = new Mock<IBRollOrchestrator>();
        _videoComposerMock = new Mock<IVideoComposer>();
        _repositoryMock = new Mock<IBroadcastRepository>();

        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<BroadcastJob>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BroadcastJob job, CancellationToken _) => job);

        _repositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<BroadcastJob>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _sut = new BroadcastOrchestratorService(
            _newsSourceMock.Object,
            _scriptGeneratorMock.Object,
            _ttsProviderMock.Object,
            _avatarGeneratorMock.Object,
            _brollOrchestratorMock.Object,
            _videoComposerMock.Object,
            _repositoryMock.Object,
            Mock.Of<ILogger<BroadcastOrchestratorService>>());
    }

    [Fact]
    public async Task StartBroadcastAsync_CreatesJobWithPendingStatus()
    {
        // Arrange
        var request = new BroadcastRequest
        {
            TimePeriodHours = 24,
            Categories = new List<NewsCategory> { NewsCategory.Inrikes },
            MaxArticles = 5
        };

        // Act
        var job = await _sut.StartBroadcastAsync(request);

        // Assert
        job.Should().NotBeNull();
        job.Id.Should().NotBeNullOrEmpty();
        job.CorrelationId.Should().NotBeNullOrEmpty();
        job.Request.Should().Be(request);
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<BroadcastJob>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetJobStatusAsync_ReturnsNull_WhenJobNotFound()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetByIdAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((BroadcastJob?)null);

        // Act
        var result = await _sut.GetJobStatusAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetJobStatusAsync_ReturnsJob_WhenExists()
    {
        // Arrange
        var existingJob = new BroadcastJob
        {
            Id = "test-123",
            Status = BroadcastStatus.GeneratingScript,
            ProgressPercent = 25
        };

        _repositoryMock
            .Setup(r => r.GetByIdAsync("test-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingJob);

        // Act
        var result = await _sut.GetJobStatusAsync("test-123");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("test-123");
        result.Status.Should().Be(BroadcastStatus.GeneratingScript);
    }
}
