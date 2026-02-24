using EDChat.Api.DTOs;
using EDChat.Api.Mappers;
using EDChat.Data.Entities;
using EDChat.Data.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;

namespace EDChat.Api.Endpoints;

public static class UserEndpoints
{
    public static RouteGroupBuilder MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users").WithTags("Users");

        group.MapGet("/", async (IRepository<User> repo) =>
            TypedResults.Ok((await repo.GetAllAsync()).Select(u => u.ToDto())))
            .WithName("GetAllUsers")
            .WithSummary("Obtiene todos los usuarios");

        group.MapPost("/", async Task<Results<Ok<UserDto>, Created<UserDto>>> (CreateUserDto dto, UserRepository repo) =>
        {
            var existing = await repo.GetByUsernameAsync(dto.Username);
            if (existing is not null)
                return TypedResults.Ok(existing.ToDto());

            var user = dto.ToEntity();
            await repo.CreateAsync(user);
            return TypedResults.Created($"/api/users/{user.Id}", user.ToDto());
        })
            .WithName("CreateUser")
            .WithSummary("Crea un nuevo usuario o retorna el existente");

        return group;
    }
}
