namespace BusinessLayer.Services.Interfaces;

public interface IAgentMessageHandler
{
    Task HandleMessageAsync(string message, int serverId);
}