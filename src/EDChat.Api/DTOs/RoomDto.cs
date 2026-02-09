using System.ComponentModel.DataAnnotations;

namespace EDChat.Api.DTOs;

public record RoomDto(int Id, string Name, string Description, DateTime CreatedAt);

public record CreateRoomDto(
    [Required(ErrorMessage = "El nombre de la sala es requerido")]
    [MaxLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
    string Name,
    [MaxLength(500, ErrorMessage = "La descripcion no puede exceder 500 caracteres")]
    string Description = "");

public record UpdateRoomDto(
    [Required(ErrorMessage = "El nombre de la sala es requerido")]
    [MaxLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
    string Name,
    [MaxLength(500, ErrorMessage = "La descripcion no puede exceder 500 caracteres")]
    string Description = "");
