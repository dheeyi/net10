# Clase 4: Integracion con la API
> Duracion estimada: ~12 min

---

## Capitulo 1: Refactorizar endpoints y mappers

### Explicacion

Ahora que los repositorios estan listos, reemplazamos `ChatStore` por repositorios en todos los endpoints. Los cambios principales son:

1. **Sync → async**: los endpoints pasan a ser `async` porque los repos usan la BD
2. **ChatStore → IRepository / repos concretos**: inyeccion por DI
3. **DtoMappers**: cambia el namespace de `EDChat.Api.Models` a `EDChat.Data.Entities`, y `message.Username` se convierte en `message.User.Username` (navigation property)
4. **MessageEndpoints**: pierde el POST — los mensajes se envian por SignalR, no por REST
5. **Endpoint filter**: usa `Arguments.OfType<>()` en vez de `GetArgument<>()` y obtiene `IRepository<Room>` del DI

> **Nota clave**: DtoMappers DEBE refactorizarse junto con los endpoints porque ahora reciben `EDChat.Data.Entities.User` de los repos en vez de `EDChat.Api.Models.User`.

### Prompt para Claude Code

```text
Refactoriza los siguientes archivos de EDChat.Api para usar repositorios en vez de ChatStore:

1. Mappers/DtoMappers.cs:
- Cambiar using EDChat.Api.Models por using EDChat.Data.Entities
- En el extension de Message: cambiar message.Username por message.User.Username
- Eliminar el extension de CreateMessageDto (ya no se usa, los mensajes van por SignalR)

2. Endpoints/UserEndpoints.cs:
- Cambiar using EDChat.Api.Services por using EDChat.Data.Entities y using EDChat.Data.Repositories
- GET: async, recibe IRepository<User> repo, usa await repo.GetAllAsync()
- POST: async Task<Results<Ok<UserDto>, Created<UserDto>>>, recibe CreateUserDto dto y UserRepository repo, usa await repo.GetByUsernameAsync() y await repo.CreateAsync()

3. Endpoints/RoomEndpoints.cs:
- Cambiar using EDChat.Api.Services por using EDChat.Data.Entities y using EDChat.Data.Repositories
- GET: async, recibe IRepository<Room> repo, usa await repo.GetAllAsync()
- POST: async, recibe CreateRoomDto dto y IRepository<Room> repo
- POST filter: usar context.Arguments.OfType<CreateRoomDto>().Single() y obtener IRepository<Room> del DI, usar await repo.GetAllAsync()
- PUT: async Task<Results<Ok<RoomDto>, NotFound>>, recibe id, UpdateRoomDto dto, IRepository<Room> repo
- DELETE: async Task<Results<NoContent, NotFound>>, recibe id, IRepository<Room> repo

4. Endpoints/MessageEndpoints.cs:
- Solo queda el GET: async, recibe int roomId y MessageRepository repo, usa await repo.GetByRoomIdAsync(roomId)
- Eliminar el POST completo (los mensajes se envian por SignalR)
- Cambiar usings: eliminar EDChat.Api.Services y EDChat.Api.DTOs, agregar EDChat.Data.Repositories
```

### Codigo esperado

```csharp
// Mappers/DtoMappers.cs
using EDChat.Api.DTOs;
using EDChat.Data.Entities;

namespace EDChat.Api.Mappers;

public static class DtoMappers
{
    extension(User user)
    {
        public UserDto ToDto() => new(user.Id, user.Username, user.CreatedAt);
    }

    extension(CreateUserDto dto)
    {
        public User ToEntity() => new() { Username = dto.Username };
    }

    extension(Room room)
    {
        public RoomDto ToDto() => new(room.Id, room.Name, room.Description, room.CreatedAt);
    }

    extension(CreateRoomDto dto)
    {
        public Room ToEntity() => new() { Name = dto.Name, Description = dto.Description };
    }

    extension(Message message)
    {
        public MessageDto ToDto() => new(message.Id, message.Content, message.SentAt, message.UserId, message.User.Username, message.RoomId);
    }
}
```

```csharp
// Endpoints/UserEndpoints.cs
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
        {
            var users = await repo.GetAllAsync();
            return TypedResults.Ok(users.Select(u => u.ToDto()));
        })
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
```

```csharp
// Endpoints/RoomEndpoints.cs
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
        {
            var rooms = await repo.GetAllAsync();
            return TypedResults.Ok(rooms.Select(r => r.ToDto()));
        })
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
                return TypedResults.Conflict(new { error = $"Ya existe una sala con el nombre '{dto.Name}'" });
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
```

```csharp
// Endpoints/MessageEndpoints.cs
using EDChat.Api.Mappers;
using EDChat.Data.Repositories;

namespace EDChat.Api.Endpoints;

public static class MessageEndpoints
{
    public static RouteGroupBuilder MapMessageEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/rooms/{roomId:int}/messages").WithTags("Messages");

        group.MapGet("/", async (int roomId, MessageRepository repo) =>
        {
            var messages = await repo.GetByRoomIdAsync(roomId);
            return TypedResults.Ok(messages.Select(m => m.ToDto()));
        })
        .WithName("GetRoomMessages")
        .WithSummary("Obtiene los mensajes de una sala");

        return group;
    }
}
```

### Puntos clave
- `message.User.Username` en vez de `message.Username` — la navigation property resuelve el JOIN
- El POST de mensajes se elimina — SignalR es el canal de envio de mensajes. El GET se mantiene para cargar historico
- El endpoint filter ahora usa `Arguments.OfType<CreateRoomDto>().Single()` en vez de `GetArgument<>(0)` porque la posicion del argumento cambia al reemplazar ChatStore por repos
- PUT ahora primero busca, modifica las propiedades, y llama `UpdateAsync` — diferente a la version ChatStore que tenia un metodo `UpdateRoom(id, name, desc)`

---

## Capitulo 2: Refactorizar ChatHub

### Explicacion

El Hub de SignalR tambien usaba `ChatStore`. Ahora usa `MessageRepository` para persistir los mensajes en la BD.

Un detalle importante: despues de `messageRepo.CreateAsync(message)`, el `message.User` ya esta cargado (gracias al `LoadAsync` en el repositorio). Sin embargo, para el DTO que enviamos por SignalR, asignamos `message.User` manualmente con el username que recibimos como parametro. Esto evita una query adicional y es seguro porque el username ya fue validado al hacer login.

### Prompt para Claude Code

```text
Refactoriza Hubs/ChatHub.cs en EDChat.Api:

- Cambiar el constructor de ChatStore store a MessageRepository messageRepo
- Cambiar los usings: eliminar EDChat.Api.DTOs, EDChat.Api.Models y EDChat.Api.Services. Agregar EDChat.Data.Entities y EDChat.Data.Repositories
- En JoinRoom y LeaveRoom: cambiar roomId.ToString() por $"room-{roomId}" (formato mas descriptivo)
- En SendMessage:
  - Crear Message sin Username (la entidad ya no tiene esa propiedad): solo Content, RoomId, UserId
  - Usar await messageRepo.CreateAsync(message) en vez de store.CreateMessage(message)
  - Despues del create, asignar message.User = new User { Id = userId, Username = username }
  - Enviar al grupo $"room-{roomId}" (en vez de roomId.ToString())
  - Usar message.ToDto() directamente en el ReceiveMessage (sin variable intermedia)
```

### Codigo esperado

```csharp
// Hubs/ChatHub.cs
using EDChat.Api.Mappers;
using EDChat.Data.Entities;
using EDChat.Data.Repositories;
using Microsoft.AspNetCore.SignalR;

namespace EDChat.Api.Hubs;

public class ChatHub(MessageRepository messageRepo) : Hub<IChatClient>
{
    public async Task JoinRoom(int roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"room-{roomId}");
    }

    public async Task LeaveRoom(int roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"room-{roomId}");
    }

    public async Task SendMessage(int roomId, int userId, string username, string content)
    {
        var message = new Message
        {
            Content = content,
            RoomId = roomId,
            UserId = userId
        };

        await messageRepo.CreateAsync(message);

        message.User = new User { Id = userId, Username = username };
        await Clients.Group($"room-{roomId}").ReceiveMessage(message.ToDto());
    }
}
```

### Puntos clave
- `Message` ya no tiene `Username` — la entidad usa navigation property
- `message.User = new User { ... }` asigna el User manualmente para el mapeo a DTO, evitando una query extra
- `$"room-{roomId}"` es un formato mas descriptivo que `roomId.ToString()` para los nombres de grupo SignalR

---

## Capitulo 3: Crear GlobalExceptionHandler

### Explicacion

En el Modulo 2, el manejo de excepciones usaba un lambda inline en `Program.cs`. Ahora lo extraemos a una clase dedicada que implementa `IExceptionHandler` — una interfaz de .NET 8+ que permite manejar excepciones de forma mas estructurada.

Ademas de retornar una respuesta JSON generica, el handler logea la excepcion completa para debugging.

### Comandos

```bash
cd src/EDChat.Api
mkdir Handlers
```

### Prompt para Claude Code

```text
En la carpeta Handlers/ de EDChat.Api, crea GlobalExceptionHandler.cs:

- Implementa IExceptionHandler (namespace Microsoft.AspNetCore.Diagnostics)
- Primary constructor con ILogger<GlobalExceptionHandler>
- En TryHandleAsync: logear la excepcion con logger.LogError(exception, "Error no controlado: {Message}", exception.Message), establecer status code 500, retornar JSON con error "Ocurrio un error interno en el servidor" usando WriteAsJsonAsync, retornar true (excepcion manejada)

Namespace: EDChat.Api.Handlers.
```

### Codigo esperado

```csharp
// Handlers/GlobalExceptionHandler.cs
using Microsoft.AspNetCore.Diagnostics;

namespace EDChat.Api.Handlers;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Error no controlado: {Message}", exception.Message);

        httpContext.Response.StatusCode = 500;
        await httpContext.Response.WriteAsJsonAsync(
            new { error = "Ocurrio un error interno en el servidor" }, cancellationToken);

        return true;
    }
}
```

### Actualizar Program.cs

### Prompt para Claude Code

```text
En Program.cs de EDChat.Api:
- Agregar using EDChat.Api.Handlers
- Reemplazar el lambda inline de UseExceptionHandler por el patron de servicio:
  - En servicios: builder.Services.AddExceptionHandler<GlobalExceptionHandler>() y builder.Services.AddProblemDetails()
  - En pipeline: reemplazar el bloque app.UseExceptionHandler(error => ...) por app.UseExceptionHandler()
```

### Codigo esperado

```csharp
// Program.cs - servicios
using EDChat.Api.Handlers;

// ... (otros usings)

// Manejo global de excepciones (reemplaza el lambda inline)
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// ... (otros servicios)
```

```csharp
// Program.cs - pipeline (reemplazar el bloque lambda)
app.UseExceptionHandler();  // usa el GlobalExceptionHandler registrado en DI
```

### Puntos clave
- `IExceptionHandler` permite inyectar dependencias (como `ILogger`) — el lambda inline no podia
- `AddProblemDetails()` es requerido por `UseExceptionHandler()` cuando se usa sin lambda — configura el formato de respuesta
- `TryHandleAsync` retorna `true` para indicar que la excepcion fue manejada; si retorna `false`, pasa al siguiente handler

---

## Capitulo 4: Limpieza de codigo obsoleto

### Explicacion

Con todo migrado a repositorios, eliminamos el codigo que ya no se usa:

1. **`Models/`** — las entidades ahora viven en `EDChat.Data/Entities/`
2. **`Services/ChatStore.cs`** — reemplazado por los repositorios
3. **`CreateMessageDto`** de `MessageDto.cs` — ya no hay POST de mensajes por REST
4. **Usings obsoletos** en Program.cs — `EDChat.Api.Services` ya no se usa

### Comandos

```bash
cd src/EDChat.Api

# Eliminar Models/ (reemplazado por EDChat.Data/Entities/)
rm -rf Models/

# Eliminar ChatStore (reemplazado por repositorios)
rm Services/ChatStore.cs

# Si la carpeta Services queda vacia, eliminarla
rmdir Services/ 2>/dev/null
```

### Prompt para Claude Code

```text
En DTOs/MessageDto.cs de EDChat.Api, elimina el record CreateMessageDto completo (incluido el using System.ComponentModel.DataAnnotations si queda sin uso). Solo debe quedar MessageDto.

En Program.cs de EDChat.Api, elimina:
- La linea builder.Services.AddSingleton<ChatStore>()
- El using EDChat.Api.Services (ya no existe)
- Mantener Scalar.AspNetCore y MapScalarApiReference (sigue siendo util para explorar la API)
```

### Codigo esperado

```csharp
// DTOs/MessageDto.cs - limpio
namespace EDChat.Api.DTOs;

public record MessageDto(int Id, string Content, DateTime SentAt, int UserId, string Username, int RoomId);
```

### Program.cs final

Despues de todos los cambios de las clases 1-4, el Program.cs queda asi:

```csharp
// Program.cs - version final
using EDChat.Api.Endpoints;
using EDChat.Api.Handlers;
using EDChat.Api.Hubs;
using EDChat.Api.Middlewares;
using EDChat.Data;
using EDChat.Data.Entities;
using EDChat.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// EF Core + SQLite
builder.Services.AddDbContext<EDChatDb>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=edchat.db"));

// Repositories
builder.Services.AddScoped<IRepository<User>, UserRepository>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<IRepository<Room>, RoomRepository>();
builder.Services.AddScoped<IRepository<Message>, MessageRepository>();
builder.Services.AddScoped<MessageRepository>();

// Validacion nativa de .NET 10
builder.Services.AddValidation();

// Manejo global de excepciones
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// SignalR
builder.Services.AddSignalR();

// OpenAPI
builder.Services.AddOpenApi();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://localhost:5002", "http://localhost:5003")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Middleware como servicio (IMiddleware requiere registro en DI)
builder.Services.AddSingleton<RequestLoggingMiddleware>();

var app = builder.Build();

// Aplicar migraciones y crear BD automaticamente
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EDChatDb>();
    db.Database.EnsureCreated();
}

app.UseExceptionHandler();

app.UseCors();
app.UseMiddleware<RequestLoggingMiddleware>();

// OpenAPI endpoint
app.MapOpenApi();
app.MapScalarApiReference();

// Endpoints
app.MapUserEndpoints();
app.MapRoomEndpoints();
app.MapMessageEndpoints();

// SignalR Hub
app.MapHub<ChatHub>("/chat");

app.Run();
```

### Verificacion

```bash
# Eliminar la BD anterior para que se recree limpia
rm src/EDChat.Api/edchat.db 2>/dev/null

dotnet build

dotnet run --project src/EDChat.Api &

# Probar endpoints
curl http://localhost:5001/api/rooms
# Debe retornar las 2 salas seed

curl -X POST http://localhost:5001/api/users \
  -H "Content-Type: application/json" \
  -d '{"username":"carlos"}'
# Debe crear el usuario

curl http://localhost:5001/api/rooms/1/messages
# Debe retornar lista vacia (aun no hay mensajes)

# Ctrl+C para detener
```

---

## Estado del proyecto al finalizar

### Archivos creados/modificados/eliminados
```
EDChat.Api/
├── DTOs/
│   ├── UserDto.cs
│   ├── RoomDto.cs
│   └── MessageDto.cs                <-- MODIFICADO (sin CreateMessageDto)
├── Endpoints/
│   ├── UserEndpoints.cs             <-- MODIFICADO (repos async)
│   ├── RoomEndpoints.cs             <-- MODIFICADO (repos async)
│   └── MessageEndpoints.cs          <-- MODIFICADO (solo GET, repos async)
├── Handlers/
│   └── GlobalExceptionHandler.cs    <-- NUEVO
├── Hubs/
│   ├── IChatClient.cs
│   └── ChatHub.cs                   <-- MODIFICADO (MessageRepository)
├── Mappers/
│   └── DtoMappers.cs                <-- MODIFICADO (Data.Entities, User.Username)
├── Middlewares/
│   └── RequestLoggingMiddleware.cs
├── Program.cs                        <-- MODIFICADO (sin ChatStore, con GlobalExceptionHandler)
├── Models/                           <-- ELIMINADO
└── Services/ChatStore.cs             <-- ELIMINADO
```

### Verificacion

```bash
dotnet build
```

Debe compilar sin errores. No mas ChatStore — toda la persistencia pasa por EF Core y repositorios. La app funciona igual que antes pero con datos persistentes.
