using System.ComponentModel.DataAnnotations;

namespace EDChat.Api.DTOs;

public record UserDto(int Id, string Username, DateTime CreatedAt);

public record CreateUserDto(
    [Required(ErrorMessage = "El nombre de usuario es requerido")]
    [MaxLength(50, ErrorMessage = "El nombre de usuario no puede exceder 50 caracteres")]
    string Username);
