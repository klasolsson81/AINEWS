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
/// Background worker that consumes TTS generation messages from RabbitMQ.
/// Deserializes TtsGenerationMessage, resolves ITtsProvider from a scoped container,
/// and generates speech audio for each broadcast segment section.
/// </summary>
public class TtsWorker : BackgroundService
{
    private readonly RabbitMqConnection _rabbitMqConnection;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TtsWorker> _logger;

    private IChannel? _channel;

    public TtsWorker(
        RabbitMqConnection rabbitMqConnection,
        IServiceProvider serviceProvider,
        ILogger<TtsWorker> logger)
    {
        _rabbitMqConnection = rabbitMqConnection;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TtsWorker starting. Listening on queue '{Queue}'", QueueNames.TtsGeneration);

        try
        {
            _channel = await _rabbitMqConnection.CreateChannelAsync();

            // Declare the queue (idempotent — safe to call multiple times)
            await _channel.QueueDeclareAsync(
                queue: QueueNames.TtsGeneration,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            // Prefetch 1 message at a time for fair dispatch
            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (sender, ea) =>
            {
                await ProcessMessageAsync(ea, stoppingToken);
            };

            await _channel.BasicConsumeAsync(
                queue: QueueNames.TtsGeneration,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation("TtsWorker is now consuming messages from '{Queue}'", QueueNames.TtsGeneration);

            // Keep the service running until cancellation is requested
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("TtsWorker shutting down gracefully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TtsWorker encountered a fatal error");
            throw;
        }
        finally
        {
            if (_channel is not null)
            {
                await _channel.CloseAsync();
                _channel.Dispose();
                _logger.LogInformation("TtsWorker channel closed");
            }
        }
    }

    private async Task ProcessMessageAsync(BasicDeliverEventArgs ea, CancellationToken stoppingToken)
    {
        var body = ea.Body.ToArray();
        var json = Encoding.UTF8.GetString(body);

        TtsGenerationMessage? message = null;

        try
        {
            message = JsonSerializer.Deserialize<TtsGenerationMessage>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (message is null)
            {
                _logger.LogWarning("TtsWorker received null message, discarding. Raw: {Raw}", json);
                await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
                return;
            }

            _logger.LogInformation(
                "TtsWorker processing message: BroadcastId={BroadcastId}, Segment={Segment}, Section={Section}, CorrelationId={CorrelationId}",
                message.BroadcastId, message.SegmentNumber, message.SectionType, message.CorrelationId);

            // Create a scope to resolve scoped services (ITtsProvider)
            using var scope = _serviceProvider.CreateScope();
            var ttsProvider = scope.ServiceProvider.GetRequiredService<ITtsProvider>();

            var result = await ttsProvider.GenerateSpeechAsync(
                message.Text,
                message.Tone,
                stoppingToken);

            _logger.LogInformation(
                "TtsWorker completed: BroadcastId={BroadcastId}, Segment={Segment}, Section={Section}, AudioPath={AudioPath}, Duration={Duration:F1}s",
                message.BroadcastId, message.SegmentNumber, message.SectionType,
                result.AudioFilePath, result.DurationSeconds);

            await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "TtsWorker message processing cancelled: BroadcastId={BroadcastId}, Segment={Segment}",
                message?.BroadcastId ?? "unknown", message?.SegmentNumber ?? -1);

            // Nack with requeue so another worker can pick it up after restart
            await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "TtsWorker failed to process message: BroadcastId={BroadcastId}, Segment={Segment}, Section={Section}",
                message?.BroadcastId ?? "unknown", message?.SegmentNumber ?? -1, message?.SectionType ?? "unknown");

            // Nack without requeue — message goes to dead letter queue or is discarded
            // This prevents infinite retry loops for poisoned messages
            await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
        }
    }
}
