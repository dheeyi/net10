using EDChat.Api.Models;

namespace EDChat.Api.Services;

public class ChatStore
{
    private readonly List<User> _users = [];
    private readonly List<Room> _rooms =
    [
        new Room { Id = 1, Name = "General", Description = "Sala de chat general", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
        new Room { Id = 2, Name = "Tecnología", Description = "Discusiones sobre tecnología", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
    ];
    private readonly List<Message> _messages = [];

    private int _nextUserId = 1;
    private int _nextRoomId = 3;
    private int _nextMessageId = 1;

    // Users
    public List<User> GetAllUsers() => _users;

    public User? GetUserById(int id) => _users.Find(u => u.Id == id);

    public User? GetUserByUsername(string username) =>
        _users.Find(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

    public User CreateUser(User user)
    {
        user.Id = _nextUserId++;
        _users.Add(user);
        return user;
    }

    // Rooms
    public List<Room> GetAllRooms() => _rooms;

    public Room? GetRoomById(int id) => _rooms.Find(r => r.Id == id);

    public Room CreateRoom(Room room)
    {
        room.Id = _nextRoomId++;
        _rooms.Add(room);
        return room;
    }

    public Room? UpdateRoom(int id, string name, string description)
    {
        var room = GetRoomById(id);
        if (room is null) return null;

        room.Name = name;
        room.Description = description;
        return room;
    }

    public bool DeleteRoom(int id)
    {
        var room = GetRoomById(id);
        if (room is null) return false;

        _rooms.Remove(room);
        return true;
    }

    // Messages
    public List<Message> GetMessagesByRoom(int roomId) =>
        _messages.FindAll(m => m.RoomId == roomId);

    public Message CreateMessage(Message message)
    {
        message.Id = _nextMessageId++;
        _messages.Add(message);
        return message;
    }
}
