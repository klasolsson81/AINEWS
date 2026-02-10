using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NewsRoom.Core.Interfaces;
using NewsRoom.Core.Messages;
using NewsRoom.Core.Models;
using NewsRoom.Workers.Infrastructure;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NewsRoom.Workers.Workers;

/// <summary>
/// Background worker that consumes video composition messages from RabbitMQ.
/// This is the final step in the broadcast pipeline — it retrieves the script
/// and all generated assets, then delegates to IVideoComposer to produce
/// the finished broadcast video.
///
/// For now, it uses placeholder logic to retrieve script/assets since the full
/// asset tracking system is not yet implemented. The IVideoComposer mock will
/// handle the actual composition simulation.
/// </summary>
public class CompositionWorker : BackgroundService
{
    private readonly RabbitMqConnection _rabbitMqConnection;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CompositionWorker> _logger;

    private IChannel? _channel;

    public CompositionWorker(
        RabbitMqConnection rabbitMqConnection,
        IServiceProvider serviceProvider,
        ILogger<CompositionWorker> logger)
    {
        _rabbitMqConnection = rabbitMqConnection;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CompositionWorker starting. Listening on queue '{Queue}'", QueueNames.VideoComposition);

        try
        {
            _channel = await _rabbitMqConnection.CreateChannelAsync();

            await _channel.QueueDeclareAsync(
                queue: QueueNames.VideoComposition,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            // Composition is heavy — only process one at a time
            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (sender, ea) =>
            {
                await ProcessMessageAsync(ea, stoppingToken);
            };

            await _channel.BasicConsumeAsync(
                queue: QueueNames.VideoComposition,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation("CompositionWorker is now consuming messages from '{Queue}'", QueueNames.VideoComposition);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("CompositionWorker shutting down gracefully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CompositionWorker encountered a fatal error");
            throw;
        }
        finally
        {
            if (_channel is not null)
            {
                await _channel.CloseAsync();
                _channel.Dispose();
                _logger.LogInformation("CompositionWorker channel closed");
            }
        }
    }

    private async Task ProcessMessageAsync(BasicDeliverEventArgs ea, CancellationToken stoppingToken)
    {
        var body = ea.Body.ToArray();
        var json = Encoding.UTF8.GetString(body);

        VideoCompositionMessage? message = null;

        try
        {
            message = JsonSerializer.Deserialize<VideoCompositionMessage>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (message is null)
            {
                _logger.LogWarning("CompositionWorker received null message, discarding. Raw: {Raw}", json);
                await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
                return;
            }

            _logger.LogInformation(
                "CompositionWorker processing message: BroadcastId={BroadcastId}, CorrelationId={CorrelationId}",
                message.BroadcastId, message.CorrelationId);

            using var scope = _serviceProvider.CreateScope();
            var videoComposer = scope.ServiceProvider.GetRequiredService<IVideoComposer>();
            var broadcastRepository = scope.ServiceProvider.GetRequiredService<IBroadcastRepository>();

            // Retrieve the broadcast job and its script from the repository
            var broadcastJob = await broadcastRepository.GetByIdAsync(message.BroadcastId, stoppingToken);

            if (broadcastJob?.Script is null)
            {
                _logger.LogWarning(
                    "CompositionWorker: No script found for BroadcastId={BroadcastId}. Using placeholder script for composition.",
                    message.BroadcastId);
            }

            var script = broadcastJob?.Script ?? CreatePlaceholderScript(message.BroadcastId);

            // Build placeholder composition assets
            // In full implementation, this would query an asset tracking service
            // to find all generated TTS audio, avatar videos, and B-roll files
            var assets = BuildPlaceholderAssets(message.BroadcastId, script);

            _logger.LogInformation(
                "CompositionWorker composing video: BroadcastId={BroadcastId}, Segments={SegmentCount}, EstimatedDuration={EstDuration}s",
                message.BroadcastId, script.TotalSegments, script.EstimatedDurationSeconds);

            var outputPath = await videoComposer.ComposeAsync(script, assets, stoppingToken);

            _logger.LogInformation(
                "CompositionWorker completed: BroadcastId={BroadcastId}, OutputPath={OutputPath}",
                message.BroadcastId, outputPath);

            // Update the broadcast job with the output path if we have a real job
            if (broadcastJob is not null)
            {
                broadcastJob.OutputVideoPath = outputPath;
                broadcastJob.Status = Core.Enums.BroadcastStatus.Completed;
                broadcastJob.CompletedAt = DateTime.UtcNow;
                broadcastJob.ProgressPercent = 100;
                broadcastJob.StatusMessage = "Broadcast completed successfully";
                await broadcastRepository.UpdateAsync(broadcastJob, stoppingToken);
            }

            await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "CompositionWorker message processing cancelled: BroadcastId={BroadcastId}",
                message?.BroadcastId ?? "unknown");

            await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "CompositionWorker failed to process message: BroadcastId={BroadcastId}",
                message?.BroadcastId ?? "unknown");

            // Update broadcast job status to failed if possible
            if (message is not null)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var repository = scope.ServiceProvider.GetRequiredService<IBroadcastRepository>();
                    var job = await repository.GetByIdAsync(message.BroadcastId, stoppingToken);

                    if (job is not null)
                    {
                        job.Status = Core.Enums.BroadcastStatus.Failed;
                        job.ErrorMessage = ex.Message;
                        job.StatusMessage = "Video composition failed";
                        await repository.UpdateAsync(job, stoppingToken);
                    }
                }
                catch (Exception innerEx)
                {
                    _logger.LogWarning(innerEx,
                        "CompositionWorker could not update job status to failed for BroadcastId={BroadcastId}",
                        message.BroadcastId);
                }
            }

            await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
        }
    }

    /// <summary>
    /// Creates a minimal placeholder script when the real script is not available.
    /// This allows the composition pipeline to still run with mock data.
    /// </summary>
    private static BroadcastScript CreatePlaceholderScript(string broadcastId)
    {
        return new BroadcastScript
        {
            BroadcastId = broadcastId,
            GeneratedAt = DateTime.UtcNow,
            TotalSegments = 0,
            EstimatedDurationSeconds = 0,
            Intro = new ScriptIntro { AnchorText = "Placeholder intro" },
            Segments = new List<ScriptSegment>(),
            Outro = new ScriptOutro { AnchorText = "Placeholder outro" }
        };
    }

    /// <summary>
    /// Builds placeholder CompositionAssets for the given script.
    /// In the full implementation, this would query the storage/asset tracking
    /// system to locate all generated TTS, avatar, and B-roll files.
    /// </summary>
    private static CompositionAssets BuildPlaceholderAssets(string broadcastId, BroadcastScript script)
    {
        var assets = new CompositionAssets
        {
            BroadcastId = broadcastId,
            SegmentAssets = new Dictionary<int, SegmentAssets>()
        };

        foreach (var segment in script.Segments)
        {
            assets.SegmentAssets[segment.SegmentNumber] = new SegmentAssets
            {
                SegmentNumber = segment.SegmentNumber,
                BRollPaths = new List<string>()
            };
        }

        return assets;
    }
}
