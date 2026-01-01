using BusinessLayer.Services.Interfaces;
using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace BusinessLayer.Services.Concrete;

public class WebSocketConnectionManager : IWebSocketConnectionManager
{
    // This is a thread-safe dictionary to hold all active connections.
    // Key: agentId (we'll get this from their auth token later)
    // Value: The WebSocket connection itself
    private readonly ConcurrentDictionary<string, WebSocket> _sockets = new();

    public Task AddSocket(string agentId, WebSocket socket) { 
    _sockets.TryAdd(agentId, socket);
        return Task.CompletedTask;
    }

    public Task RemoveSocket(string agentId)
    {
        _sockets.TryRemove(agentId, out _);
        return Task.CompletedTask;
    }

    public WebSocket? GetSocket(string agentId)
    {
        _sockets.TryGetValue(agentId, out var socket);
        return socket;
    }

    public string? GetId(WebSocket socket)
    {
        return _sockets.FirstOrDefault(p => p.Value == socket).Key;
    }
}
