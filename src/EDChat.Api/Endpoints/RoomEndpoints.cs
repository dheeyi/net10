using EDChat.Api.DTOs;
using EDChat.Api.Mappers;
using EDChat.Data.Entities;
using EDChat.Data.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;

namespace EDChat.Api.Endpoints;

public static class RoomEndpoints
{
    public static RouteGroupBuilder MapRoomEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/rooms").WithTags("Rooms");

        group.MapGet("/", async (IRepository<Room> repo) =>
            TypedResults.Ok((await repo.GetAllAsync()).Select(r => r.ToDto())))
            .WithName("GetAllRooms")
            .WithSummary("Obtiene todas las salas");

        group.MapPost("/", async (CreateRoomDto dto, IRepository<Room> repo) =>
        {
            var room = dto.ToEntity();
            await repo.CreateAsync(room);
            return TypedResults.Created($"/api/rooms/{room.Id}", room.ToDto());
        })
            .AddEndpointFilter(async (context, next) =>
            {
                var dto = context.Arguments.OfType<CreateRoomDto>().Single();
                var repo = context.HttpContext.RequestServices.GetRequiredService<IRepository<Room>>();
                var rooms = await repo.GetAllAsync();
                if (rooms.Any(r => r.Name.Equals(dto.Name, StringComparison.OrdinalIgnoreCase)))
                    return TypedResults.Conflict("Ya existe una sala con ese nombre");
                return await next(context);
            })
            .WithName("CreateRoom")
            .WithSummary("Crea una nueva sala");

        group.MapPut("/{id:int}", async Task<Results<Ok<RoomDto>, NotFound>> (int id, UpdateRoomDto dto, IRepository<Room> repo) =>
        {
            var room = await repo.GetByIdAsync(id);
            if (room is null)
                return TypedResults.NotFound();

            room.Name = dto.Name;
            room.Description = dto.Description;
            await repo.UpdateAsync(room);
            return TypedResults.Ok(room.ToDto());
        })
            .WithName("UpdateRoom")
            .WithSummary("Actualiza una sala existente");

        group.MapDelete("/{id:int}", async Task<Results<NoContent, NotFound>> (int id, IRepository<Room> repo) =>
        {
            var room = await repo.GetByIdAsync(id);
            if (room is null)
                return TypedResults.NotFound();

            await repo.DeleteAsync(id);
            return TypedResults.NoContent();
        })
            .WithName("DeleteRoom")
            .WithSummary("Elimina una sala");

        return group;
    }
}
