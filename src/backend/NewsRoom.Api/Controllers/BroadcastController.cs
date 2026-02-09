using Microsoft.AspNetCore.Mvc;
using NewsRoom.Core.DTOs;
using NewsRoom.Core.Enums;
using NewsRoom.Core.Interfaces;
using NewsRoom.Core.Models;

namespace NewsRoom.Api.Controllers;

/// <summary>
/// Controller for creating and monitoring AI news broadcast jobs.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BroadcastController : ControllerBase
{
    private readonly IBroadcastOrchestrator _orchestrator;
    private readonly IBroadcastRepository _repository;
    private readonly ILogger<BroadcastController> _logger;

    public BroadcastController(
        IBroadcastOrchestrator orchestrator,
        IBroadcastRepository repository,
        ILogger<BroadcastController> logger)
    {
        _orchestrator = orchestrator;
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Start a new broadcast generation.
    /// Returns 202 Accepted with the job ID for status polling.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(BroadcastStatusDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BroadcastStatusDto>> CreateBroadcastAsync(
        [FromBody] BroadcastRequestDto request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "CreateBroadcast: TimePeriod={Hours}h, Categories=[{Categories}], MaxArticles={Max}",
            request.TimePeriodHours,
            string.Join(", ", request.Categories),
            request.MaxArticles);

        // Parse and validate categories
        var categories = request.Categories
            .Select(c => Enum.TryParse<NewsCategory>(c, ignoreCase: true, out var cat) ? cat : (NewsCategory?)null)
            .Where(c => c.HasValue)
            .Select(c => c!.Value)
            .ToList();

        if (categories.Count == 0)
        {
            _logger.LogWarning("CreateBroadcast: No valid categories provided: [{Categories}]",
                string.Join(", ", request.Categories));
            return BadRequest(new { error = "Minst en giltig kategori krävs." });
        }

        var broadcastRequest = new BroadcastRequest
        {
            TimePeriodHours = request.TimePeriodHours,
            Categories = categories,
            MaxArticles = request.MaxArticles
        };

        var job = await _orchestrator.StartBroadcastAsync(broadcastRequest, cancellationToken);

        _logger.LogInformation("CreateBroadcast: Job {JobId} started (correlation: {CorrelationId})",
            job.Id, job.CorrelationId);

        var statusDto = MapToStatusDto(job);
        return Accepted(statusDto);
    }

    /// <summary>
    /// Get the current status of a broadcast job.
    /// </summary>
    [HttpGet("{jobId}")]
    [ProducesResponseType(typeof(BroadcastStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BroadcastStatusDto>> GetStatusAsync(
        string jobId,
        CancellationToken cancellationToken)
    {
        var job = await _orchestrator.GetJobStatusAsync(jobId, cancellationToken);

        if (job == null)
        {
            return NotFound(new { error = "Sändningsjobb hittades inte.", jobId });
        }

        return Ok(MapToStatusDto(job));
    }

    /// <summary>
    /// Get a list of recent broadcast jobs.
    /// </summary>
    [HttpGet("recent")]
    [ProducesResponseType(typeof(IEnumerable<BroadcastStatusDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<BroadcastStatusDto>>> GetRecentAsync(
        [FromQuery] int count = 10,
        CancellationToken cancellationToken = default)
    {
        var jobs = await _repository.GetRecentAsync(count, cancellationToken);

        var result = jobs.Select(MapToStatusDto);
        return Ok(result);
    }

    /// <summary>
    /// Map a BroadcastJob entity to a BroadcastStatusDto.
    /// </summary>
    private static BroadcastStatusDto MapToStatusDto(BroadcastJob job)
    {
        return new BroadcastStatusDto
        {
            JobId = job.Id,
            Status = job.Status.ToString(),
            StatusMessage = job.StatusMessage,
            ProgressPercent = job.ProgressPercent,
            VideoUrl = job.OutputVideoPath != null ? $"/api/broadcast/{job.Id}/video" : null,
            ErrorMessage = job.ErrorMessage
        };
    }
}
