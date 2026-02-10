using FluentAssertions;
using NewsRoom.Core.Enums;
using NewsRoom.Core.Models;
using NewsRoom.Infrastructure.Persistence;

namespace NewsRoom.Tests.Unit.Services;

public class InMemoryBroadcastRepositoryTests
{
    private readonly InMemoryBroadcastRepository _sut = new();

    [Fact]
    public async Task CreateAsync_StoresJob()
    {
        var job = new BroadcastJob { Id = "test-1" };

        var result = await _sut.CreateAsync(job);

        result.Should().BeSameAs(job);
        var fetched = await _sut.GetByIdAsync("test-1");
        fetched.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        var result = await _sut.GetByIdAsync("nonexistent");
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesExistingJob()
    {
        var job = new BroadcastJob { Id = "test-2", Status = BroadcastStatus.Pending };
        await _sut.CreateAsync(job);

        job.Status = BroadcastStatus.Completed;
        job.ProgressPercent = 100;
        await _sut.UpdateAsync(job);

        var fetched = await _sut.GetByIdAsync("test-2");
        fetched!.Status.Should().Be(BroadcastStatus.Completed);
        fetched.ProgressPercent.Should().Be(100);
    }

    [Fact]
    public async Task GetRecentAsync_ReturnsJobsOrderedByCreatedAt()
    {
        for (int i = 0; i < 5; i++)
        {
            await _sut.CreateAsync(new BroadcastJob
            {
                Id = $"job-{i}",
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }

        var recent = await _sut.GetRecentAsync(3);

        recent.Should().HaveCount(3);
        recent[0].Id.Should().Be("job-0"); // newest first
        recent[1].Id.Should().Be("job-1");
        recent[2].Id.Should().Be("job-2");
    }

    [Fact]
    public async Task GetRecentAsync_RespectsCountLimit()
    {
        for (int i = 0; i < 15; i++)
        {
            await _sut.CreateAsync(new BroadcastJob { Id = $"job-{i}" });
        }

        var recent = await _sut.GetRecentAsync(10);
        recent.Should().HaveCount(10);
    }
}
