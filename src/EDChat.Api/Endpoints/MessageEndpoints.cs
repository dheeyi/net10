using EDChat.Api.DTOs;
using EDChat.Api.Mappers;
using EDChat.Api.Services;

namespace EDChat.Api.Endpoints;

public static class MessageEndpoints
{
    public static RouteGroupBuilder MapMessageEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/rooms/{roomId:int}/messages").WithTags("Messages");

        group.MapGet("/", (int roomId, ChatStore store) =>
            TypedResults.Ok(store.GetMessagesByRoom(roomId).Select(m => m.ToDto())))
            .WithName("GetRoomMessages")
            .WithSummary("Obtiene los mensajes de una sala");

        group.MapPost("/", (int roomId, CreateMessageDto dto, ChatStore store) =>
        {
            var message = dto.ToEntity(roomId);
            store.CreateMessage(message);
            return TypedResults.Created($"/api/rooms/{roomId}/messages/{message.Id}", message.ToDto());
        })
            .WithName("CreateMessage")
            .WithSummary("Envia un mensaje a una sala");

        return group;
    }
}
