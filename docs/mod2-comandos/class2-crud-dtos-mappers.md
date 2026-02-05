# Clase 2: CRUD Completo: Endpoints, DTOs y Mappers
> Duracion estimada: ~12 min

---

## Capitulo 1: Endpoint GET - listar

### Explicacion

Empezamos creando endpoints GET usando entidades directas (sin DTOs). El estudiante ve como funciona, y despues en los capitulos 4-5 veremos por que esto es un problema y como se soluciona con DTOs.

Usamos `TypedResults.Ok()` en vez de `Results.Ok()` porque retorna un tipo concreto (`Ok<T>`) que OpenAPI puede introspeccionar para generar schemas exactos.

### Comandos

```bash
cd src/EDChat.Api
mkdir Endpoints
```

### Prompt para Claude Code

```text
En la carpeta Endpoints/ de EDChat.Api, crea RoomEndpoints.cs con un extension method MapRoomEndpoints sobre WebApplication. Usa app.MapGroup("/api/rooms") para crear el grupo. Agrega un endpoint GET / que reciba ChatStore por DI y retorne TypedResults.Ok(store.GetAllRooms()). Retorna el RouteGroupBuilder.

Tambien crea UserEndpoints.cs en la misma carpeta con un extension method MapUserEndpoints. Usa app.MapGroup("/api/users"). Agrega un endpoint GET / que retorne TypedResults.Ok(store.GetAllUsers()).

Usa los namespaces EDChat.Api.Endpoints y EDChat.Api.Services.

Actualiza Program.cs para llamar app.MapRoomEndpoints() y app.MapUserEndpoints() antes de app.Run().
```

### Codigo esperado

```csharp
// Endpoints/RoomEndpoints.cs
using EDChat.Api.Services;

namespace EDChat.Api.Endpoints;

public static class RoomEndpoints
{
    public static RouteGroupBuilder MapRoomEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/rooms");

        group.MapGet("/", (ChatStore store) =>
            TypedResults.Ok(store.GetAllRooms()));

        return group;
    }
}
```

```csharp
// Endpoints/UserEndpoints.cs
using EDChat.Api.Services;

namespace EDChat.Api.Endpoints;

public static class UserEndpoints
{
    public static RouteGroupBuilder MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users");

        group.MapGet("/", (ChatStore store) =>
            TypedResults.Ok(store.GetAllUsers()));

        return group;
    }
}
```

```csharp
// Program.cs actualizado
using EDChat.Api.Endpoints;
using EDChat.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ChatStore>();

var app = builder.Build();

app.MapUserEndpoints();
app.MapRoomEndpoints();

app.Run();
```

### Verificacion

```bash
dotnet run --project src/EDChat.Api &
curl http://localhost:5001/api/rooms
curl http://localhost:5001/api/users
# Ctrl+C para detener
```

---

## Capitulo 2: Endpoint POST - crear

### Explicacion

Para usuarios, si el username ya existe retornamos el usuario existente (`Ok`) en vez de crear uno duplicado (`Created`). Con `TypedResults`, declaramos ambos tipos de retorno usando `Results<Ok<User>, Created<User>>`.

`using Microsoft.AspNetCore.Http.HttpResults` es necesario para `Results<>`, `Ok<T>`, `Created<T>`, etc.

### Prompt para Claude Code

```text
Agrega al endpoint group de RoomEndpoints.cs un POST / que reciba un objeto Room del body y use store.CreateRoom(room). Retorna TypedResults.Created($"/api/rooms/{room.Id}", room). Agrega using EDChat.Api.Models si no existe.

Agrega al endpoint group de UserEndpoints.cs un POST / que reciba un objeto User del body. Primero verifica si ya existe un usuario con ese username usando store.GetUserByUsername(user.Username). Si existe, retorna TypedResults.Ok(existing). Si no, usa store.CreateUser(user) y retorna TypedResults.Created(...). Declara el tipo de retorno como Results<Ok<User>, Created<User>>.

Agrega using Microsoft.AspNetCore.Http.HttpResults en UserEndpoints.cs para el tipo Results<>.
```

### Codigo esperado

```csharp
// Agregar a RoomEndpoints.cs dentro del metodo MapRoomEndpoints, despues del GET
group.MapPost("/", (Room room, ChatStore store) =>
{
    store.CreateRoom(room);
    return TypedResults.Created($"/api/rooms/{room.Id}", room);
});
```

```csharp
// UserEndpoints.cs completo
using EDChat.Api.Models;
using EDChat.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace EDChat.Api.Endpoints;

public static class UserEndpoints
{
    public static RouteGroupBuilder MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users");

        group.MapGet("/", (ChatStore store) =>
            TypedResults.Ok(store.GetAllUsers()));

        group.MapPost("/", Results<Ok<User>, Created<User>> (User user, ChatStore store) =>
        {
            var existing = store.GetUserByUsername(user.Username);
            if (existing is not null)
                return TypedResults.Ok(existing);

            store.CreateUser(user);
            return TypedResults.Created($"/api/users/{user.Id}", user);
        });

        return group;
    }
}
```

### Verificacion

```bash
# Crear una sala
curl -X POST http://localhost:5001/api/rooms \
  -H "Content-Type: application/json" \
  -d '{"name":"Deportes","description":"Sala de deportes"}'

# Crear un usuario
curl -X POST http://localhost:5001/api/users \
  -H "Content-Type: application/json" \
  -d '{"username":"carlos"}'

# Verificar que se crearon
curl http://localhost:5001/api/rooms
curl http://localhost:5001/api/users
```

---

## Capitulo 3: Endpoints PUT y DELETE

### Explicacion

Completamos el CRUD con PUT y DELETE. Ambos usan `Results<>` para declarar que pueden retornar `NotFound`. Tambien creamos `MessageEndpoints.cs`.

### Prompt para Claude Code

```text
Agrega a RoomEndpoints.cs:
- PUT /{id:int} con tipo de retorno Results<Ok<Room>, NotFound>. Recibe int id y Room room del body. Usa store.UpdateRoom(id, room.Name, room.Description). Si retorna null, devuelve TypedResults.NotFound(). Si existe, retorna TypedResults.Ok(updated).
- DELETE /{id:int} con tipo de retorno Results<NoContent, NotFound>. Recibe int id. Usa store.DeleteRoom(id). Si retorna false devuelve TypedResults.NotFound(), si true devuelve TypedResults.NoContent().

Agrega using Microsoft.AspNetCore.Http.HttpResults en RoomEndpoints.cs.

En la carpeta Endpoints/, crea MessageEndpoints.cs con MapMessageEndpoints:
- Grupo: /api/rooms/{roomId:int}/messages
- GET / que reciba int roomId y retorne TypedResults.Ok(store.GetMessagesByRoom(roomId))
- POST / que reciba int roomId, un objeto Message del body, asigne message.RoomId = roomId y use store.CreateMessage(message). Retorna TypedResults.Created(...).

Actualiza Program.cs para incluir app.MapMessageEndpoints().
```

### Codigo esperado

```csharp
// RoomEndpoints.cs completo
using EDChat.Api.Models;
using EDChat.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace EDChat.Api.Endpoints;

public static class RoomEndpoints
{
    public static RouteGroupBuilder MapRoomEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/rooms");

        group.MapGet("/", (ChatStore store) =>
            TypedResults.Ok(store.GetAllRooms()));

        group.MapPost("/", (Room room, ChatStore store) =>
        {
            store.CreateRoom(room);
            return TypedResults.Created($"/api/rooms/{room.Id}", room);
        });

        group.MapPut("/{id:int}", Results<Ok<Room>, NotFound> (int id, Room room, ChatStore store) =>
        {
            var updated = store.UpdateRoom(id, room.Name, room.Description);
            if (updated is null)
                return TypedResults.NotFound();

            return TypedResults.Ok(updated);
        });

        group.MapDelete("/{id:int}", Results<NoContent, NotFound> (int id, ChatStore store) =>
        {
            var deleted = store.DeleteRoom(id);
            if (!deleted)
                return TypedResults.NotFound();

            return TypedResults.NoContent();
        });

        return group;
    }
}
```

```csharp
// Endpoints/MessageEndpoints.cs
using EDChat.Api.Models;
using EDChat.Api.Services;

namespace EDChat.Api.Endpoints;

public static class MessageEndpoints
{
    public static RouteGroupBuilder MapMessageEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/rooms/{roomId:int}/messages");

        group.MapGet("/", (int roomId, ChatStore store) =>
            TypedResults.Ok(store.GetMessagesByRoom(roomId)));

        group.MapPost("/", (int roomId, Message message, ChatStore store) =>
        {
            message.RoomId = roomId;
            store.CreateMessage(message);
            return TypedResults.Created($"/api/rooms/{roomId}/messages/{message.Id}", message);
        });

        return group;
    }
}
```

```csharp
// Program.cs actualizado
using EDChat.Api.Endpoints;
using EDChat.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ChatStore>();

var app = builder.Build();

app.MapUserEndpoints();
app.MapRoomEndpoints();
app.MapMessageEndpoints();

app.Run();
```

### Verificacion

```bash
# Actualizar sala
curl -X PUT http://localhost:5001/api/rooms/1 \
  -H "Content-Type: application/json" \
  -d '{"name":"General Chat","description":"Sala general actualizada"}'

# Eliminar sala
curl -X DELETE http://localhost:5001/api/rooms/3

# Enviar mensaje (primero crear un usuario)
curl -X POST http://localhost:5001/api/users \
  -H "Content-Type: application/json" \
  -d '{"username":"carlos"}'

curl -X POST http://localhost:5001/api/rooms/1/messages \
  -H "Content-Type: application/json" \
  -d '{"content":"Hola a todos!","userId":1,"username":"carlos"}'

# Obtener mensajes de la sala
curl http://localhost:5001/api/rooms/1/messages
```

---

## Capitulo 4: DTOs con record types de C# 14

### Explicacion

Hasta ahora los endpoints retornan entidades directamente (intencional para que el estudiante vea el problema). Los **DTOs** (Data Transfer Objects) separan el contrato de la API del modelo interno — controlan exactamente que datos expones y recibes.

Usamos `record` (inmutable, sintaxis compacta) con **Data Annotations** (`[Required]`, `[MaxLength]`). Las anotaciones todavia **no estan activas** — en la Clase 4 las activaremos con `builder.Services.AddValidation()`.

### Comandos

```bash
mkdir DTOs
```

### Prompt para Claude Code

```text
En la carpeta DTOs/ de EDChat.Api, crea tres archivos:

UserDto.cs - dos records:
- UserDto(int Id, string Username, DateTime CreatedAt)
- CreateUserDto con Username que tenga [Required(ErrorMessage = "El nombre de usuario es requerido")] y [MaxLength(50, ErrorMessage = "El nombre de usuario no puede exceder 50 caracteres")]

RoomDto.cs - tres records:
- RoomDto(int Id, string Name, string Description, DateTime CreatedAt)
- CreateRoomDto con Name que tenga [Required(ErrorMessage = "El nombre de la sala es requerido")] y [MaxLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")], y Description con [MaxLength(500, ErrorMessage = "La descripcion no puede exceder 500 caracteres")] y default ""
- UpdateRoomDto con las mismas anotaciones que CreateRoomDto

MessageDto.cs - dos records:
- MessageDto(int Id, string Content, DateTime SentAt, int UserId, string Username, int RoomId)
- CreateMessageDto con Content que tenga [Required(ErrorMessage = "El contenido del mensaje es requerido")] y [MaxLength(2000, ErrorMessage = "El mensaje no puede exceder 2000 caracteres")], mas int UserId y string Username

Namespace: EDChat.Api.DTOs. Importar System.ComponentModel.DataAnnotations donde se usen anotaciones.
```

### Codigo esperado

```csharp
// DTOs/UserDto.cs
using System.ComponentModel.DataAnnotations;

namespace EDChat.Api.DTOs;

public record UserDto(int Id, string Username, DateTime CreatedAt);

public record CreateUserDto(
    [Required(ErrorMessage = "El nombre de usuario es requerido")]
    [MaxLength(50, ErrorMessage = "El nombre de usuario no puede exceder 50 caracteres")]
    string Username);
```

```csharp
// DTOs/RoomDto.cs
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
```

```csharp
// DTOs/MessageDto.cs
using System.ComponentModel.DataAnnotations;

namespace EDChat.Api.DTOs;

public record MessageDto(int Id, string Content, DateTime SentAt, int UserId, string Username, int RoomId);

public record CreateMessageDto(
    [Required(ErrorMessage = "El contenido del mensaje es requerido")]
    [MaxLength(2000, ErrorMessage = "El mensaje no puede exceder 2000 caracteres")]
    string Content,
    int UserId,
    string Username);
```

### Puntos clave
- `CreateUserDto` solo tiene `Username` - no permitimos que el cliente envie `Id` o `CreatedAt`
- Las anotaciones todavia **no estan activas** - en la Clase 4 las activamos con `AddValidation()`

---

## Capitulo 5: Extension members de C# 14 para mapping

### Explicacion

Los **mappers** convierten entre entidades y DTOs. Creamos mappers con **extension members** de C# 14: bloques `extension(Tipo nombre)` que agrupan lo que extiende un tipo. Se llaman como metodos de instancia: `user.ToDto()`.

### Comandos

```bash
mkdir Mappers
```

### Prompt para Claude Code

```text
En la carpeta Mappers/ de EDChat.Api, crea DtoMappers.cs usando la sintaxis de extension members de C# 14. La clase estatica DtoMappers debe tener bloques extension(Tipo nombre) para cada tipo:

- extension(User user) con metodo UserDto ToDto() - mapea User a UserDto
- extension(CreateUserDto dto) con metodo User ToEntity() - crea User desde CreateUserDto (solo Username)
- extension(Room room) con metodo RoomDto ToDto() - mapea Room a RoomDto
- extension(CreateRoomDto dto) con metodo Room ToEntity() - crea Room desde CreateRoomDto (Name y Description)
- extension(Message message) con metodo MessageDto ToDto() - mapea Message a MessageDto. Usa message.Username directamente para el campo Username.
- extension(CreateMessageDto dto) con metodo Message ToEntity(int roomId) - crea Message desde CreateMessageDto, asignando el RoomId recibido.

Importa EDChat.Api.Models y EDChat.Api.DTOs. Namespace: EDChat.Api.Mappers.

Luego refactoriza todos los endpoints (UserEndpoints, RoomEndpoints, MessageEndpoints) para:
- Los GET retornen DTOs usando .Select(x => x.ToDto())
- Los POST reciban CreateDto en vez de la entidad directa, usen .ToEntity() para crear la entidad
- Los PUT reciban UpdateRoomDto
- Actualizar los tipos de retorno de Results<> para usar DTOs en vez de entidades
- Importar EDChat.Api.DTOs y EDChat.Api.Mappers
```

### Codigo esperado

```csharp
// Mappers/DtoMappers.cs
using EDChat.Api.DTOs;
using EDChat.Api.Models;

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
        public MessageDto ToDto() => new(message.Id, message.Content, message.SentAt, message.UserId, message.Username, message.RoomId);
    }

    extension(CreateMessageDto dto)
    {
        public Message ToEntity(int roomId) => new() { Content = dto.Content, UserId = dto.UserId, Username = dto.Username, RoomId = roomId };
    }
}
```

```csharp
// Endpoints/UserEndpoints.cs - refactorizado con DTOs
using EDChat.Api.DTOs;
using EDChat.Api.Mappers;
using EDChat.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace EDChat.Api.Endpoints;

public static class UserEndpoints
{
    public static RouteGroupBuilder MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users");

        group.MapGet("/", (ChatStore store) =>
            TypedResults.Ok(store.GetAllUsers().Select(u => u.ToDto())));

        group.MapPost("/", Results<Ok<UserDto>, Created<UserDto>> (CreateUserDto dto, ChatStore store) =>
        {
            var existing = store.GetUserByUsername(dto.Username);
            if (existing is not null)
                return TypedResults.Ok(existing.ToDto());

            var user = dto.ToEntity();
            store.CreateUser(user);
            return TypedResults.Created($"/api/users/{user.Id}", user.ToDto());
        });

        return group;
    }
}
```

```csharp
// Endpoints/RoomEndpoints.cs - refactorizado con DTOs
using EDChat.Api.DTOs;
using EDChat.Api.Mappers;
using EDChat.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace EDChat.Api.Endpoints;

public static class RoomEndpoints
{
    public static RouteGroupBuilder MapRoomEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/rooms");

        group.MapGet("/", (ChatStore store) =>
            TypedResults.Ok(store.GetAllRooms().Select(r => r.ToDto())));

        group.MapPost("/", (CreateRoomDto dto, ChatStore store) =>
        {
            var room = dto.ToEntity();
            store.CreateRoom(room);
            return TypedResults.Created($"/api/rooms/{room.Id}", room.ToDto());
        });

        group.MapPut("/{id:int}", Results<Ok<RoomDto>, NotFound> (int id, UpdateRoomDto dto, ChatStore store) =>
        {
            var updated = store.UpdateRoom(id, dto.Name, dto.Description);
            if (updated is null)
                return TypedResults.NotFound();

            return TypedResults.Ok(updated.ToDto());
        });

        group.MapDelete("/{id:int}", Results<NoContent, NotFound> (int id, ChatStore store) =>
        {
            var deleted = store.DeleteRoom(id);
            if (!deleted)
                return TypedResults.NotFound();

            return TypedResults.NoContent();
        });

        return group;
    }
}
```

```csharp
// Endpoints/MessageEndpoints.cs - refactorizado con DTOs
using EDChat.Api.DTOs;
using EDChat.Api.Mappers;
using EDChat.Api.Services;

namespace EDChat.Api.Endpoints;

public static class MessageEndpoints
{
    public static RouteGroupBuilder MapMessageEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/rooms/{roomId:int}/messages");

        group.MapGet("/", (int roomId, ChatStore store) =>
            TypedResults.Ok(store.GetMessagesByRoom(roomId).Select(m => m.ToDto())));

        group.MapPost("/", (int roomId, CreateMessageDto dto, ChatStore store) =>
        {
            var message = dto.ToEntity(roomId);
            store.CreateMessage(message);
            return TypedResults.Created($"/api/rooms/{roomId}/messages/{message.Id}", message.ToDto());
        });

        return group;
    }
}
```

---

## Estado del proyecto al finalizar

### Archivos creados
```
EDChat.Api/
├── DTOs/
│   ├── UserDto.cs
│   ├── RoomDto.cs
│   └── MessageDto.cs
├── Endpoints/
│   ├── UserEndpoints.cs
│   ├── RoomEndpoints.cs
│   └── MessageEndpoints.cs
├── Mappers/
│   └── DtoMappers.cs
├── Models/
│   ├── User.cs
│   ├── Room.cs
│   └── Message.cs
├── Services/
│   └── ChatStore.cs
├── Program.cs
└── EDChat.Api.csproj
```

### Verificacion

```bash
cd src/EDChat.Api
dotnet build
dotnet run &

# Probar todos los endpoints
curl http://localhost:5001/api/rooms
curl http://localhost:5001/api/users

curl -X POST http://localhost:5001/api/users \
  -H "Content-Type: application/json" \
  -d '{"username":"carlos"}'

curl -X POST http://localhost:5001/api/rooms \
  -H "Content-Type: application/json" \
  -d '{"name":"Deportes","description":"Sala de deportes"}'

curl -X POST http://localhost:5001/api/rooms/1/messages \
  -H "Content-Type: application/json" \
  -d '{"content":"Hola!","userId":1,"username":"carlos"}'

curl http://localhost:5001/api/rooms/1/messages

# Ctrl+C para detener
```
