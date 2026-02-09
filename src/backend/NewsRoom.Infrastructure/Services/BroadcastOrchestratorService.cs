using Microsoft.Extensions.Logging;
using NewsRoom.Core.Enums;
using NewsRoom.Core.Interfaces;
using NewsRoom.Core.Models;

namespace NewsRoom.Infrastructure.Services;

public class BroadcastOrchestratorService : IBroadcastOrchestrator
{
    private readonly INewsSource _newsSource;
    private readonly IScriptGenerator _scriptGenerator;
    private readonly ITtsProvider _ttsProvider;
    private readonly IAvatarGenerator _avatarGenerator;
    private readonly IBRollOrchestrator _brollOrchestrator;
    private readonly IVideoComposer _videoComposer;
    private readonly IBroadcastRepository _repository;
    private readonly ILogger<BroadcastOrchestratorService> _logger;

    public BroadcastOrchestratorService(
        INewsSource newsSource,
        IScriptGenerator scriptGenerator,
        ITtsProvider ttsProvider,
        IAvatarGenerator avatarGenerator,
        IBRollOrchestrator brollOrchestrator,
        IVideoComposer videoComposer,
        IBroadcastRepository repository,
        ILogger<BroadcastOrchestratorService> logger)
    {
        _newsSource = newsSource;
        _scriptGenerator = scriptGenerator;
        _ttsProvider = ttsProvider;
        _avatarGenerator = avatarGenerator;
        _brollOrchestrator = brollOrchestrator;
        _videoComposer = videoComposer;
        _repository = repository;
        _logger = logger;
    }

    public async Task<BroadcastJob> StartBroadcastAsync(
        BroadcastRequest request,
        CancellationToken cancellationToken = default)
    {
        var job = new BroadcastJob
        {
            Request = request,
            Status = BroadcastStatus.Pending,
            StatusMessage = "Sändning skapad"
        };

        await _repository.CreateAsync(job, cancellationToken);
        _logger.LogInformation("Broadcast job {JobId} created with correlation {CorrelationId}",
            job.Id, job.CorrelationId);

        // Run the pipeline in background
        _ = Task.Run(() => ExecutePipelineAsync(job, cancellationToken), cancellationToken);

        return job;
    }

    public async Task<BroadcastJob?> GetJobStatusAsync(string jobId, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByIdAsync(jobId, cancellationToken);
    }

    private async Task ExecutePipelineAsync(BroadcastJob job, CancellationToken cancellationToken)
    {
        try
        {
            // Step 1: Fetch news
            await UpdateStatusAsync(job, BroadcastStatus.FetchingNews, "Hämtar nyheter...", 10);
            var articles = await _newsSource.FetchArticlesAsync(
                job.Request.TimePeriodHours,
                job.Request.Categories,
                job.Request.MaxArticles,
                cancellationToken);

            _logger.LogInformation("Job {JobId}: Fetched {Count} articles", job.Id, articles.Count);

            // Step 2: Generate script
            await UpdateStatusAsync(job, BroadcastStatus.GeneratingScript, "Skriver nyhetsmanus...", 25);
            var script = await _scriptGenerator.GenerateScriptAsync(articles, cancellationToken);
            job.Script = script;
            await _repository.UpdateAsync(job, cancellationToken);

            _logger.LogInformation("Job {JobId}: Script generated with {Segments} segments", job.Id, script.TotalSegments);

            // Step 3: Generate TTS audio
            await UpdateStatusAsync(job, BroadcastStatus.GeneratingAudio, "Genererar tal...", 40);
            var compositionAssets = new CompositionAssets { BroadcastId = script.BroadcastId };

            // Generate intro/outro TTS
            var introTts = await _ttsProvider.GenerateSpeechAsync(script.Intro.AnchorText, script.Intro.Tone, cancellationToken);
            compositionAssets.IntroAudioPath = introTts.AudioFilePath;

            var outroTts = await _ttsProvider.GenerateSpeechAsync(script.Outro.AnchorText, script.Outro.Tone, cancellationToken);
            compositionAssets.OutroAudioPath = outroTts.AudioFilePath;

            // Generate per-segment TTS
            foreach (var segment in script.Segments)
            {
                var segAssets = new SegmentAssets { SegmentNumber = segment.SegmentNumber };

                var introAudio = await _ttsProvider.GenerateSpeechAsync(
                    segment.AnchorIntro.Text, segment.AnchorIntro.Tone, cancellationToken);
                segAssets.AnchorIntroAudioPath = introAudio.AudioFilePath;

                var voAudio = await _ttsProvider.GenerateSpeechAsync(
                    segment.BRollVoiceover.Text, segment.BRollVoiceover.Tone, cancellationToken);
                segAssets.VoiceoverAudioPath = voAudio.AudioFilePath;

                var outroAudio = await _ttsProvider.GenerateSpeechAsync(
                    segment.AnchorOutro.Text, segment.AnchorOutro.Tone, cancellationToken);
                segAssets.AnchorOutroAudioPath = outroAudio.AudioFilePath;

                compositionAssets.SegmentAssets[segment.SegmentNumber] = segAssets;
            }

            // Step 4: Generate avatars (parallel with B-roll)
            await UpdateStatusAsync(job, BroadcastStatus.GeneratingAvatars, "Genererar nyhetsankare...", 55);

            var avatarTasks = new List<Task>();
            foreach (var segment in script.Segments)
            {
                var segAssets = compositionAssets.SegmentAssets[segment.SegmentNumber];
                avatarTasks.Add(Task.Run(async () =>
                {
                    if (segAssets.AnchorIntroAudioPath != null)
                    {
                        var result = await _avatarGenerator.GenerateAvatarVideoAsync(
                            segAssets.AnchorIntroAudioPath, segment.AnchorIntro.Tone, cancellationToken);
                        segAssets.AnchorIntroVideoPath = result.VideoFilePath;
                    }
                    if (segAssets.AnchorOutroAudioPath != null)
                    {
                        var result = await _avatarGenerator.GenerateAvatarVideoAsync(
                            segAssets.AnchorOutroAudioPath, segment.AnchorOutro.Tone, cancellationToken);
                        segAssets.AnchorOutroVideoPath = result.VideoFilePath;
                    }
                }, cancellationToken));
            }

            // Step 5: Generate B-roll (parallel)
            await UpdateStatusAsync(job, BroadcastStatus.GeneratingBRoll, "Hämtar visuellt material...", 65);

            var brollTasks = new List<Task>();
            foreach (var segment in script.Segments)
            {
                var segAssets = compositionAssets.SegmentAssets[segment.SegmentNumber];
                brollTasks.Add(Task.Run(async () =>
                {
                    var brollResults = await _brollOrchestrator.GenerateBRollForSegmentAsync(segment, cancellationToken);
                    segAssets.BRollPaths = brollResults.Select(r => r.FilePath).ToList();
                }, cancellationToken));
            }

            await Task.WhenAll(avatarTasks.Concat(brollTasks));

            // Step 6: Compose final video
            await UpdateStatusAsync(job, BroadcastStatus.Composing, "Monterar slutlig video...", 85);
            var videoPath = await _videoComposer.ComposeAsync(script, compositionAssets, cancellationToken);

            // Done!
            job.OutputVideoPath = videoPath;
            job.Status = BroadcastStatus.Completed;
            job.StatusMessage = "Sändning klar!";
            job.ProgressPercent = 100;
            job.CompletedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(job, cancellationToken);

            _logger.LogInformation("Job {JobId}: Broadcast completed. Video: {Path}", job.Id, videoPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job {JobId}: Pipeline failed", job.Id);
            job.Status = BroadcastStatus.Failed;
            job.StatusMessage = "Sändning misslyckades";
            job.ErrorMessage = ex.Message;
            await _repository.UpdateAsync(job, cancellationToken);
        }
    }

    private async Task UpdateStatusAsync(BroadcastJob job, BroadcastStatus status, string message, int progress)
    {
        job.Status = status;
        job.StatusMessage = message;
        job.ProgressPercent = progress;
        await _repository.UpdateAsync(job, CancellationToken.None);
        _logger.LogInformation("Job {JobId}: {Status} ({Progress}%) - {Message}",
            job.Id, status, progress, message);
    }
}
