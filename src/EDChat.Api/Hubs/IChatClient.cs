using EDChat.Api.DTOs;

namespace EDChat.Api.Hubs;

public interface IChatClient
{
    Task ReceiveMessage(MessageDto message);
}
