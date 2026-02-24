namespace EDChat.Api.DTOs;

public record MessageDto(int Id, string Content, DateTime SentAt, int UserId, string Username, int RoomId);
