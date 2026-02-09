using System.ComponentModel.DataAnnotations;

namespace EDChat.Api.DTOs;

public record MessageDto(int Id, string Content, DateTime SentAt, int UserId, string Username, int RoomId);

public record CreateMessageDto(
    [Required(ErrorMessage = "El contenido del mensaje es requerido")]
    [MaxLength(2000, ErrorMessage = "El mensaje no puede exceder 2000 caracteres")]
    string Content,
    int UserId,
    string Username);
