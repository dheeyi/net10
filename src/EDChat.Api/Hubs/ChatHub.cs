using EDChat.Api.Mappers;
using EDChat.Data.Entities;
using EDChat.Data.Repositories;
using Microsoft.AspNetCore.SignalR;

namespace EDChat.Api.Hubs;

public class ChatHub(MessageRepository messageRepo) : Hub<IChatClient>
{
    public async Task JoinRoom(int roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"room-{roomId}");
    }

    public async Task LeaveRoom(int roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"room-{roomId}");
    }

    public async Task SendMessage(int roomId, int userId, string username, string content)
    {
        var message = new Message
        {
            Content = content,
            RoomId = roomId,
            UserId = userId
        };

        await messageRepo.CreateAsync(message);
        message.User = new User { Id = userId, Username = username };

        await Clients.Group($"room-{roomId}").ReceiveMessage(message.ToDto());
    }
}
