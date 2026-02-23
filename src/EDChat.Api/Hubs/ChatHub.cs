using EDChat.Api.DTOs;
using EDChat.Api.Mappers;
using EDChat.Api.Models;
using EDChat.Api.Services;
using Microsoft.AspNetCore.SignalR;

namespace EDChat.Api.Hubs;

public class ChatHub(ChatStore store) : Hub<IChatClient>
{
    public async Task JoinRoom(int roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToString());
    }

    public async Task LeaveRoom(int roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId.ToString());
    }

    public async Task SendMessage(int roomId, int userId, string username, string content)
    {
        var message = new Message
        {
            Content = content,
            UserId = userId,
            Username = username,
            RoomId = roomId
        };

        store.CreateMessage(message);

        var dto = message.ToDto();

        await Clients.Group(roomId.ToString()).ReceiveMessage(dto);
    }
}
