using EDChat.Api.Mappers;
using EDChat.Data.Repositories;

namespace EDChat.Api.Endpoints;

public static class MessageEndpoints
{
    public static RouteGroupBuilder MapMessageEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/rooms/{roomId:int}/messages").WithTags("Messages");

        group.MapGet("/", async (int roomId, MessageRepository repo) =>
            TypedResults.Ok((await repo.GetByRoomIdAsync(roomId)).Select(m => m.ToDto())))
            .WithName("GetRoomMessages")
            .WithSummary("Obtiene los mensajes de una sala");

        return group;
    }
}
