using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NewsRoom.Core.Interfaces;
using NewsRoom.Core.Messages;
using NewsRoom.Workers.Infrastructure;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NewsRoom.Workers.Workers;

/// <summary>
/// Background worker that consumes B-roll generation messages from RabbitMQ.
/// Deserializes BRollGenerationMessage, resolves IBRollProvider from a scoped container,
/// and generates visual content (stock footage, AI images, editorial images, etc.)
/// for each scene within a broadcast segment.
/// </summary>
public class BRollWorker : BackgroundService
{
    private readonly RabbitMqConnection _rabbitMqConnection;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BRollWorker> _logger;

    private IChannel? _channel;

    public BRollWorker(
        RabbitMqConnection rabbitMqConnection,
        IServiceProvider serviceProvider,
        ILogger<BRollWorker> logger)
    {
        _rabbitMqConnection = rabbitMqConnection;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BRollWorker starting. Listening on queue '{Queue}'", QueueNames.BRollGeneration);

        try
        {
            _channel = await _rabbitMqConnection.CreateChannelAsync();

            await _channel.QueueDeclareAsync(
                queue: QueueNames.BRollGeneration,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            // Allow slightly higher prefetch for B-roll since scenes are lightweight
            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 2, global: false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (sender, ea) =>
            {
                await ProcessMessageAsync(ea, stoppingToken);
            };

            await _channel.BasicConsumeAsync(
                queue: QueueNames.BRollGeneration,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation("BRollWorker is now consuming messages from '{Queue}'", QueueNames.BRollGeneration);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("BRollWorker shutting down gracefully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BRollWorker encountered a fatal error");
            throw;
        }
        finally
        {
            if (_channel is not null)
            {
                await _channel.CloseAsync();
                _channel.Dispose();
                _logger.LogInformation("BRollWorker channel closed");
            }
        }
    }

    private async Task ProcessMessageAsync(BasicDeliverEventArgs ea, CancellationToken stoppingToken)
    {
        var body = ea.Body.ToArray();
        var json = Encoding.UTF8.GetString(body);

        BRollGenerationMessage? message = null;

        try
        {
            message = JsonSerializer.Deserialize<BRollGenerationMessage>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (message is null)
            {
                _logger.LogWarning("BRollWorker received null message, discarding. Raw: {Raw}", json);
                await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
                return;
            }

            _logger.LogInformation(
                "BRollWorker processing message: BroadcastId={BroadcastId}, Segment={Segment}, Scene={SceneIndex}, ContentType={ContentType}, Duration={Duration}s, CorrelationId={CorrelationId}",
                message.BroadcastId, message.SegmentNumber, message.SceneIndex,
                message.ContentType, message.DurationSeconds, message.CorrelationId);

            using var scope = _serviceProvider.CreateScope();
            var brollProvider = scope.ServiceProvider.GetRequiredService<IBRollProvider>();

            var result = await brollProvider.GenerateAsync(
                message.ContentType,
                message.Description,
                message.Prompt,
                message.SearchTerms,
                stoppingToken);

            _logger.LogInformation(
                "BRollWorker completed: BroadcastId={BroadcastId}, Segment={Segment}, Scene={SceneIndex}, FilePath={FilePath}, IsVideo={IsVideo}, Duration={Duration:F1}s, Attribution={Attribution}",
                message.BroadcastId, message.SegmentNumber, message.SceneIndex,
                result.FilePath, result.IsVideo, result.DurationSeconds, result.Attribution ?? "none");

            await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "BRollWorker message processing cancelled: BroadcastId={BroadcastId}, Segment={Segment}, Scene={SceneIndex}",
                message?.BroadcastId ?? "unknown", message?.SegmentNumber ?? -1, message?.SceneIndex ?? -1);

            await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "BRollWorker failed to process message: BroadcastId={BroadcastId}, Segment={Segment}, Scene={SceneIndex}, ContentType={ContentType}",
                message?.BroadcastId ?? "unknown", message?.SegmentNumber ?? -1,
                message?.SceneIndex ?? -1, message?.ContentType.ToString() ?? "unknown");

            await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
        }
    }
}
