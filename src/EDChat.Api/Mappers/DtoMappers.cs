using EDChat.Api.DTOs;
using EDChat.Data.Entities;

namespace EDChat.Api.Mappers;

public static class DtoMappers
{
    extension(User user)
    {
        public UserDto ToDto() => new(user.Id, user.Username, user.CreatedAt);
    }

    extension(CreateUserDto dto)
    {
        public User ToEntity() => new() { Username = dto.Username };
    }

    extension(Room room)
    {
        public RoomDto ToDto() => new(room.Id, room.Name, room.Description, room.CreatedAt);
    }

    extension(CreateRoomDto dto)
    {
        public Room ToEntity() => new() { Name = dto.Name, Description = dto.Description };
    }

    extension(Message message)
    {
        public MessageDto ToDto() => new(message.Id, message.Content, message.SentAt, message.UserId, message.User.Username, message.RoomId);
    }
}
