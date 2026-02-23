namespace EDChat.Web.Models;

public record MessageModel(int UserId, string Username, string Content, DateTime Timestamp);
