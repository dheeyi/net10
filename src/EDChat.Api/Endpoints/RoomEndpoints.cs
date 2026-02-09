using EDChat.Api.DTOs;
using EDChat.Api.Mappers;
using EDChat.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace EDChat.Api.Endpoints;

public static class RoomEndpoints
{
    public static RouteGroupBuilder MapRoomEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/rooms").WithTags("Rooms");

        group.MapGet("/", (ChatStore store) =>
            TypedResults.Ok(store.GetAllRooms().Select(r => r.ToDto())))
            .WithName("GetAllRooms")
            .WithSummary("Obtiene todas las salas");

        group.MapPost("/", (CreateRoomDto dto, ChatStore store) =>
        {
            var room = dto.ToEntity();
            store.CreateRoom(room);
            return TypedResults.Created($"/api/rooms/{room.Id}", room.ToDto());
        })
            .WithName("CreateRoom")
            .WithSummary("Crea una nueva sala");

        group.MapPut("/{id:int}", Results<Ok<RoomDto>, NotFound> (int id, UpdateRoomDto dto, ChatStore store) =>
        {
            var updated = store.UpdateRoom(id, dto.Name, dto.Description);
            if (updated is null)
                return TypedResults.NotFound();

            return TypedResults.Ok(updated.ToDto());
        })
            .WithName("UpdateRoom")
            .WithSummary("Actualiza una sala existente");

        group.MapDelete("/{id:int}", Results<NoContent, NotFound> (int id, ChatStore store) =>
        {
            if (!store.DeleteRoom(id))
                return TypedResults.NotFound();

            return TypedResults.NoContent();
        })
            .WithName("DeleteRoom")
            .WithSummary("Elimina una sala");

        return group;
    }
}
