using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace NewsRoom.Workers.Infrastructure;

/// <summary>
/// Singleton service that manages the RabbitMQ connection lifecycle.
/// Provides lazy connection creation and channel factory methods.
/// Uses RabbitMQ.Client 7.x async API.
/// </summary>
public sealed class RabbitMqConnection : IAsyncDisposable
{
    private readonly ILogger<RabbitMqConnection> _logger;
    private readonly ConnectionFactory _connectionFactory;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private IConnection? _connection;
    private bool _disposed;

    public RabbitMqConnection(IConfiguration configuration, ILogger<RabbitMqConnection> logger)
    {
        _logger = logger;

        var host = configuration["RABBITMQ_HOST"]
            ?? Environment.GetEnvironmentVariable("RABBITMQ_HOST")
            ?? "localhost";

        var user = configuration["RABBITMQ_USER"]
            ?? Environment.GetEnvironmentVariable("RABBITMQ_USER")
            ?? "newsroom";

        var password = configuration["RABBITMQ_PASSWORD"]
            ?? Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD")
            ?? "newsroom_dev_2026";

        _connectionFactory = new ConnectionFactory
        {
            HostName = host,
            UserName = user,
            Password = password,
            // Enable automatic recovery for resilience
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        _logger.LogInformation(
            "RabbitMQ connection configured for host '{Host}' with user '{User}'",
            host, user);
    }

    /// <summary>
    /// Gets or creates the shared RabbitMQ connection using lazy initialization.
    /// Thread-safe via semaphore.
    /// </summary>
    public async Task<IConnection> GetConnectionAsync()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RabbitMqConnection));

        if (_connection is { IsOpen: true })
            return _connection;

        await _connectionLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (_connection is { IsOpen: true })
                return _connection;

            _logger.LogInformation("Creating new RabbitMQ connection...");
            _connection = await _connectionFactory.CreateConnectionAsync();
            _logger.LogInformation("RabbitMQ connection established successfully");

            return _connection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create RabbitMQ connection");
            throw;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// Creates a new RabbitMQ channel from the shared connection.
    /// Each worker should create its own channel.
    /// </summary>
    public async Task<IChannel> CreateChannelAsync()
    {
        var connection = await GetConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        _logger.LogDebug("Created new RabbitMQ channel");
        return channel;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_connection is not null)
        {
            try
            {
                await _connection.CloseAsync();
                _connection.Dispose();
                _logger.LogInformation("RabbitMQ connection closed and disposed");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while disposing RabbitMQ connection");
            }
        }

        _connectionLock.Dispose();
    }
}
