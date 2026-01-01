using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using BusinessLayer.DTOs.Agent;
using BusinessLayer.Services.Infrastructure;
using Microsoft.Extensions.Logging;

namespace BusinessLayer.Services.Interfaces;

/// <summary>
/// Service for sending commands to agents via WebSocket
/// </summary>
public interface IAgentCommandService
{
    /// <summary>
    /// Send a command to an agent and wait for response
    /// </summary>
    Task<TResponse> SendCommandAsync<TRequest, TResponse>(
        int serverId,
        string action,
        TRequest? requestPayload,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default
    ) where TRequest : class
      where TResponse : class;

    /// <summary>
    /// Send a command to an agent without waiting for response (fire-and-forget)
    /// Used for backup triggers and other one-way commands
    /// </summary>
    Task SendCommandToAgentAsync(int serverId, string action, object? payload);
}
