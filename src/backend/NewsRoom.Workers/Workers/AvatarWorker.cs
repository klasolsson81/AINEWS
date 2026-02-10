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
/// Background worker that consumes avatar generation messages from RabbitMQ.
/// Deserializes AvatarGenerationMessage, resolves IAvatarGenerator from a scoped container,
/// and generates lip-synced avatar video for each broadcast segment section.
/// </summary>
public class AvatarWorker : BackgroundService
{
    private readonly RabbitMqConnection _rabbitMqConnection;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AvatarWorker> _logger;

    private IChannel? _channel;

    public AvatarWorker(
        RabbitMqConnection rabbitMqConnection,
        IServiceProvider serviceProvider,
        ILogger<AvatarWorker> logger)
    {
        _rabbitMqConnection = rabbitMqConnection;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AvatarWorker starting. Listening on queue '{Queue}'", QueueNames.AvatarGeneration);

        try
        {
            _channel = await _rabbitMqConnection.CreateChannelAsync();

            await _channel.QueueDeclareAsync(
                queue: QueueNames.AvatarGeneration,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (sender, ea) =>
            {
                await ProcessMessageAsync(ea, stoppingToken);
            };

            await _channel.BasicConsumeAsync(
                queue: QueueNames.AvatarGeneration,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation("AvatarWorker is now consuming messages from '{Queue}'", QueueNames.AvatarGeneration);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("AvatarWorker shutting down gracefully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AvatarWorker encountered a fatal error");
            throw;
        }
        finally
        {
            if (_channel is not null)
            {
                await _channel.CloseAsync();
                _channel.Dispose();
                _logger.LogInformation("AvatarWorker channel closed");
            }
        }
    }

    private async Task ProcessMessageAsync(BasicDeliverEventArgs ea, CancellationToken stoppingToken)
    {
        var body = ea.Body.ToArray();
        var json = Encoding.UTF8.GetString(body);

        AvatarGenerationMessage? message = null;

        try
        {
            message = JsonSerializer.Deserialize<AvatarGenerationMessage>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (message is null)
            {
                _logger.LogWarning("AvatarWorker received null message, discarding. Raw: {Raw}", json);
                await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
                return;
            }

            _logger.LogInformation(
                "AvatarWorker processing message: BroadcastId={BroadcastId}, Segment={Segment}, Section={Section}, AudioPath={AudioPath}, CorrelationId={CorrelationId}",
                message.BroadcastId, message.SegmentNumber, message.SectionType,
                message.AudioFilePath, message.CorrelationId);

            using var scope = _serviceProvider.CreateScope();
            var avatarGenerator = scope.ServiceProvider.GetRequiredService<IAvatarGenerator>();

            var result = await avatarGenerator.GenerateAvatarVideoAsync(
                message.AudioFilePath,
                message.Tone,
                stoppingToken);

            _logger.LogInformation(
                "AvatarWorker completed: BroadcastId={BroadcastId}, Segment={Segment}, Section={Section}, VideoPath={VideoPath}, Duration={Duration:F1}s",
                message.BroadcastId, message.SegmentNumber, message.SectionType,
                result.VideoFilePath, result.DurationSeconds);

            await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "AvatarWorker message processing cancelled: BroadcastId={BroadcastId}, Segment={Segment}",
                message?.BroadcastId ?? "unknown", message?.SegmentNumber ?? -1);

            await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "AvatarWorker failed to process message: BroadcastId={BroadcastId}, Segment={Segment}, Section={Section}",
                message?.BroadcastId ?? "unknown", message?.SegmentNumber ?? -1, message?.SectionType ?? "unknown");

            await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
        }
    }
}
