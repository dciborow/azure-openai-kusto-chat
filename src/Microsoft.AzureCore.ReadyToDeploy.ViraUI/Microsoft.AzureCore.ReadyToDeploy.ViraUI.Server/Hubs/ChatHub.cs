namespace Microsoft.AzureCore.ReadyToDeploy.ViraUI.Server.Hubs;

using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;
using Microsoft.AzureCore.ReadyToDeploy.Vira;

public class ChatHub : Hub
{
    private readonly ClearwaterChatService _chatService;

    public ChatHub(ClearwaterChatService chatService)
    {
        _chatService = chatService;
    }

    public async Task SendMessage(string userInput)
    {
        var userId = Context.ConnectionId;

        // Notify the client that the assistant is typing
        await Clients.Caller.SendAsync("AssistantTyping", true);

        // Get assistant's response
        var response = await _chatService.GetChatResponseAsync(userId, userInput);

        // Notify the client that the assistant has stopped typing
        await Clients.Caller.SendAsync("AssistantTyping", false);

        // Send the assistant's response back to the client
        await Clients.Caller.SendAsync("ReceiveMessage", "Assistant", response);
    }
}
