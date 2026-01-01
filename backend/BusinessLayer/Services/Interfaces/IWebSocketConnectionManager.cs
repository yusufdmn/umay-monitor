using System.Net.WebSockets;

namespace BusinessLayer.Services.Interfaces;

public interface IWebSocketConnectionManager
{
    // Adds a socket to the pool. We'll use this *after* the agent is authenticated.
    Task AddSocket(string agentId, WebSocket socket);

    // Removes a socket when the agent disconnects.
    Task RemoveSocket(string agentId);

    // Gets a socket to send a message to a specific agent.
    WebSocket? GetSocket(string agentId);

    // Gets the ID of an agent from its socket object.
    string? GetId(WebSocket socket);
}