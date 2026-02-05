# Clase 1: Minimal APIs y su arquitectura
> Duracion estimada: ~12 min

---

## Capitulo 1: ¿Que son Minimal APIs? Ventajas vs Controllers

### Explicacion

Minimal APIs es el modelo recomendado por Microsoft para proyectos nuevos. Los endpoints se definen con `app.MapGet()`, `app.MapPost()`, etc. directamente en el pipeline, sin Controllers.

En .NET 10, Minimal APIs incluyen validacion nativa, Server-Sent Events, y OpenAPI 3.1 completo. Controllers solo se justifican para features especificas: model binding extensible, JsonPatch, OData o application parts.

---

## Capitulo 2: Crear proyecto WebAPI con Minimal APIs

### Explicacion

`dotnet new web` crea un proyecto Minimal API vacio (solo `Program.cs` con un `app.MapGet`).

> **Nota:** Si ya se creo el proyecto en el Modulo 1, se puede reutilizar. Si es asi, hay que limpiar el contenido de `Program.cs` y eliminar archivos innecesarios.

### Comandos

```bash
# Desde la raiz de la solucion EDChat/
dotnet new list web
dotnet new web -n EDChat.Api -o src/EDChat.Api
dotnet sln add src/EDChat.Api
dotnet build
```

### Configurar el puerto

Reemplaza el contenido de `Properties/launchSettings.json`:

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5001",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "https://localhost:5002;http://localhost:5001",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

### Modelos del dominio

Los **modelos** representan las entidades del negocio — la estructura de datos que nuestra aplicacion maneja.

EDChat tiene tres modelos: `User`, `Room` y `Message`. `Message.Username` es un string directo — en el Modulo 4 se cambia a navigation property. Los defaults (`string.Empty`, `DateTime.UtcNow`) evitan warnings de nullability.

### Comandos

```bash
cd src/EDChat.Api
mkdir Models
```

### Prompt para Claude Code

```text
En la carpeta Models/ de EDChat.Api, crea tres archivos:

User.cs - propiedades: int Id, string Username, DateTime CreatedAt (default UtcNow)

Room.cs - propiedades: int Id, string Name, string Description, DateTime CreatedAt (default UtcNow)

Message.cs - propiedades: int Id, string Content, DateTime SentAt (default UtcNow), int UserId, string Username, int RoomId

Usa el namespace EDChat.Api.Models. Clases publicas simples con propiedades auto-implementadas.
```

### Codigo esperado

```csharp
// Models/User.cs
namespace EDChat.Api.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

```csharp
// Models/Room.cs
namespace EDChat.Api.Models;

public class Room
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

```csharp
// Models/Message.cs
namespace EDChat.Api.Models;

public class Message
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int RoomId { get; set; }
}
```

---

## Capitulo 3: Route groups y organizacion de endpoints

### Explicacion

`app.MapGroup("/api/rooms")` crea un grupo de endpoints con prefijo compartido. Organizamos cada grupo en su propio archivo con un extension method sobre `WebApplication`:

```csharp
// Patron: un archivo por entidad
public static class RoomEndpoints
{
    public static RouteGroupBuilder MapRoomEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/rooms");
        // endpoints del grupo
        return group;
    }
}
```

En `Program.cs` se registran como `app.MapRoomEndpoints()`. En la Clase 2 crearemos los archivos de endpoints con los handlers CRUD.

---

## Capitulo 4: Dependency Injection en Minimal APIs

### Explicacion

Los servicios registrados en DI se inyectan como parametros de los endpoint handlers. `AddSingleton<ChatStore>()` registra una instancia unica compartida en todos los requests.

```csharp
group.MapGet("/", (ChatStore store) => ...);
//                  ↑ inyectado automaticamente por DI
```

Un **servicio** es una clase que encapsula logica reutilizable. `ChatStore` es nuestro almacen de datos in-memory.

### Comandos

```bash
mkdir Services
```

### Prompt para Claude Code

```text
En la carpeta Services/ de EDChat.Api, crea ChatStore.cs. Es un almacen in-memory singleton con estas caracteristicas:

- Listas privadas: _users, _rooms, _messages (todas List<T> del namespace EDChat.Api.Models)
- Contadores privados para IDs auto-incrementales: _nextUserId = 1, _nextRoomId = 3, _nextMessageId = 1
- Datos seed en _rooms: Room con Id=1, Name="General", Description="Sala de chat general", CreatedAt=2025-01-01 UTC y Room con Id=2, Name="Tecnología", Description="Discusiones sobre tecnología", CreatedAt=2025-01-01 UTC (fechas fijas para que los datos seed sean predecibles)
- Metodos publicos para Users: List<User> GetAllUsers(), User? GetUserById(int id), User? GetUserByUsername(string username), User CreateUser(User user) (asigna Id auto-incremental)
- Metodos publicos para Rooms: List<Room> GetAllRooms(), Room? GetRoomById(int id), Room CreateRoom(Room room) (asigna Id), Room? UpdateRoom(int id, string name, string description), bool DeleteRoom(int id)
- Metodos publicos para Messages: List<Message> GetMessagesByRoom(int roomId), Message CreateMessage(Message message) (asigna Id)
```

### Codigo esperado

```csharp
// Services/ChatStore.cs
using EDChat.Api.Models;

namespace EDChat.Api.Services;

public class ChatStore
{
    private readonly List<User> _users = [];
    private readonly List<Room> _rooms =
    [
        new Room { Id = 1, Name = "General", Description = "Sala de chat general", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
        new Room { Id = 2, Name = "Tecnología", Description = "Discusiones sobre tecnología", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
    ];
    private readonly List<Message> _messages = [];

    private int _nextUserId = 1;
    private int _nextRoomId = 3;
    private int _nextMessageId = 1;

    // Users
    public List<User> GetAllUsers() => [.. _users];

    public User? GetUserById(int id) => _users.FirstOrDefault(u => u.Id == id);

    public User? GetUserByUsername(string username) =>
        _users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

    public User CreateUser(User user)
    {
        user.Id = _nextUserId++;
        _users.Add(user);
        return user;
    }

    // Rooms
    public List<Room> GetAllRooms() => [.. _rooms];

    public Room? GetRoomById(int id) => _rooms.FirstOrDefault(r => r.Id == id);

    public Room CreateRoom(Room room)
    {
        room.Id = _nextRoomId++;
        _rooms.Add(room);
        return room;
    }

    public Room? UpdateRoom(int id, string name, string description)
    {
        var room = _rooms.FirstOrDefault(r => r.Id == id);
        if (room is null) return null;

        room.Name = name;
        room.Description = description;
        return room;
    }

    public bool DeleteRoom(int id)
    {
        var room = _rooms.FirstOrDefault(r => r.Id == id);
        if (room is null) return false;

        _rooms.Remove(room);
        return true;
    }

    // Messages
    public List<Message> GetMessagesByRoom(int roomId) =>
        [.. _messages.Where(m => m.RoomId == roomId).OrderBy(m => m.SentAt)];

    public Message CreateMessage(Message message)
    {
        message.Id = _nextMessageId++;
        _messages.Add(message);
        return message;
    }
}
```

### Puntos clave
- `[.. list]` es la spread syntax de C# 12 - crea una copia de la lista (evita modificacion externa)
- `_nextRoomId = 3` porque ya hay 2 rooms seed

---

## Capitulo 5: Program.cs - estructura y configuracion

### Explicacion

`Program.cs` es el punto de entrada. Registramos `ChatStore` como singleton. En el Modulo 4, se reemplazara por `AddDbContext<EDChatDb>()` y repositorios scoped.

### Prompt para Claude Code

```text
Reemplaza el contenido de Program.cs en EDChat.Api con la configuracion minima: crear el builder, registrar ChatStore como singleton usando builder.Services.AddSingleton<ChatStore>(), construir la app y ejecutar con app.Run(). Solo importar EDChat.Api.Services. No agregar endpoints todavia.
```

### Codigo esperado

```csharp
// Program.cs
using EDChat.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ChatStore>();

var app = builder.Build();

app.Run();
```

---

## Estado del proyecto al finalizar

### Archivos creados
```
EDChat.Api/
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
```

Debe compilar sin errores. La app aun no tiene endpoints - eso se hace en la Clase 2.
