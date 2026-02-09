using Microsoft.Extensions.Logging;
using NewsRoom.Core.Interfaces;
using System.Text.Json;

namespace NewsRoom.Infrastructure.Mocks;

public class MockMessagePublisher : IMessagePublisher
{
    private readonly ILogger<MockMessagePublisher> _logger;

    public MockMessagePublisher(ILogger<MockMessagePublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync<T>(string queueName, T message, CancellationToken cancellationToken = default) where T : class
    {
        _logger.LogInformation("MockMessagePublisher: Published to queue '{Queue}': {Type}",
            queueName, typeof(T).Name);
        _logger.LogDebug("MockMessagePublisher: Message content: {Content}", JsonSerializer.Serialize(message));
        return Task.CompletedTask;
    }
}
