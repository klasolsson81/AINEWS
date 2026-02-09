using Microsoft.AspNetCore.SignalR;

namespace NewsRoom.Api.Hubs;

/// <summary>
/// SignalR hub for real-time broadcast job status updates.
/// Clients join a group identified by the jobId to receive
/// progress updates, status changes, and completion/error notifications.
/// </summary>
public class BroadcastHub : Hub
{
    private readonly ILogger<BroadcastHub> _logger;

    public BroadcastHub(ILogger<BroadcastHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Subscribe to status updates for a specific broadcast job.
    /// </summary>
    public async Task JoinBroadcastGroup(string jobId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, jobId);
        _logger.LogInformation("Client {ConnectionId} joined broadcast group {JobId}",
            Context.ConnectionId, jobId);
    }

    /// <summary>
    /// Unsubscribe from status updates for a specific broadcast job.
    /// </summary>
    public async Task LeaveBroadcastGroup(string jobId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, jobId);
        _logger.LogInformation("Client {ConnectionId} left broadcast group {JobId}",
            Context.ConnectionId, jobId);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogWarning(exception, "Client disconnected with error: {ConnectionId}",
                Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
