# Clase 3: Repository pattern
> Duracion estimada: ~12 min

---

## Capitulo 1: Crear la interfaz generica IRepository

### Explicacion

El **patron Repository** encapsula el acceso a datos detras de una interfaz. Los endpoints no saben si los datos vienen de SQLite, SQL Server, o memoria — solo llaman metodos del repositorio.

Creamos una interfaz generica `IRepository<T>` con las operaciones CRUD basicas. Cada entidad tendra su implementacion concreta.

```
Endpoint  →  IRepository<Room>  →  RoomRepository  →  EDChatDb  →  SQLite
```

### Comandos

```bash
cd src/EDChat.Data
mkdir Repositories
```

### Prompt para Claude Code

```text
En la carpeta Repositories/ de EDChat.Data, crea IRepository.cs con una interfaz generica:

- IRepository<T> donde T : class
- Task<List<T>> GetAllAsync()
- Task<T?> GetByIdAsync(int id)
- Task<T> CreateAsync(T entity)
- Task<T> UpdateAsync(T entity)
- Task DeleteAsync(int id)

Namespace: EDChat.Data.Repositories.
```

### Codigo esperado

```csharp
// Repositories/IRepository.cs
namespace EDChat.Data.Repositories;

public interface IRepository<T> where T : class
{
    Task<List<T>> GetAllAsync();
    Task<T?> GetByIdAsync(int id);
    Task<T> CreateAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task DeleteAsync(int id);
}
```

### Puntos clave
- `where T : class` restringe T a tipos por referencia (las entidades)
- Todos los metodos son async (`Task<>`) porque las operaciones de BD son I/O
- La interfaz define el contrato — la implementacion decide como hablar con la BD

---

## Capitulo 2: Implementar UserRepository y RoomRepository

### Explicacion

Cada repositorio recibe `EDChatDb` por constructor (primary constructor) y usa los metodos de EF Core:
- `ToListAsync()` — ejecuta la query y retorna una lista
- `FindAsync(id)` — busca por primary key (muy eficiente, usa cache del contexto)
- `Add()` + `SaveChangesAsync()` — inserta y guarda
- `Update()` + `SaveChangesAsync()` — marca como modificado y guarda
- `Remove()` + `SaveChangesAsync()` — marca para eliminar y guarda

`UserRepository` agrega un metodo extra `GetByUsernameAsync` que el endpoint de login necesita.

### Prompt para Claude Code

```text
En la carpeta Repositories/ de EDChat.Data, crea dos archivos:

UserRepository.cs - implementa IRepository<User>:
- Primary constructor con EDChatDb
- GetAllAsync: db.Users.OrderBy(u => u.Username).ToListAsync()
- GetByIdAsync: db.Users.FindAsync(id).AsTask() (FindAsync retorna ValueTask, AsTask lo convierte)
- GetByUsernameAsync(string username): metodo extra, db.Users.FirstOrDefaultAsync(u => u.Username == username)
- CreateAsync: db.Users.Add(entity), SaveChangesAsync, retornar entity
- UpdateAsync: db.Users.Update(entity), SaveChangesAsync, retornar entity
- DeleteAsync: buscar por id con FindAsync, si existe Remove + SaveChangesAsync

RoomRepository.cs - implementa IRepository<Room>:
- Misma estructura que UserRepository pero con db.Rooms
- GetAllAsync ordenado por Name

Namespaces: EDChat.Data.Repositories. Importar EDChat.Data.Entities y Microsoft.EntityFrameworkCore.
```

### Codigo esperado

```csharp
// Repositories/UserRepository.cs
using EDChat.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EDChat.Data.Repositories;

public class UserRepository(EDChatDb db) : IRepository<User>
{
    public Task<List<User>> GetAllAsync() =>
        db.Users.OrderBy(u => u.Username).ToListAsync();

    public Task<User?> GetByIdAsync(int id) =>
        db.Users.FindAsync(id).AsTask();

    public Task<User?> GetByUsernameAsync(string username) =>
        db.Users.FirstOrDefaultAsync(u => u.Username == username);

    public async Task<User> CreateAsync(User entity)
    {
        db.Users.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task<User> UpdateAsync(User entity)
    {
        db.Users.Update(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task DeleteAsync(int id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is not null)
        {
            db.Users.Remove(user);
            await db.SaveChangesAsync();
        }
    }
}
```

```csharp
// Repositories/RoomRepository.cs
using EDChat.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EDChat.Data.Repositories;

public class RoomRepository(EDChatDb db) : IRepository<Room>
{
    public Task<List<Room>> GetAllAsync() =>
        db.Rooms.OrderBy(r => r.Name).ToListAsync();

    public Task<Room?> GetByIdAsync(int id) =>
        db.Rooms.FindAsync(id).AsTask();

    public async Task<Room> CreateAsync(Room entity)
    {
        db.Rooms.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task<Room> UpdateAsync(Room entity)
    {
        db.Rooms.Update(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task DeleteAsync(int id)
    {
        var room = await db.Rooms.FindAsync(id);
        if (room is not null)
        {
            db.Rooms.Remove(room);
            await db.SaveChangesAsync();
        }
    }
}
```

### Puntos clave
- `FindAsync(id).AsTask()` — `FindAsync` retorna `ValueTask`, pero la interfaz usa `Task`. `.AsTask()` hace la conversion
- `OrderBy` se ejecuta en la BD (SQL `ORDER BY`), no en memoria
- Despues de `Add` + `SaveChangesAsync`, EF Core asigna el `Id` generado por la BD al entity

---

## Capitulo 3: Implementar MessageRepository

### Explicacion

`MessageRepository` es mas complejo porque necesita **cargar la relacion con User**. Cuando consultamos mensajes, queremos incluir el `Username` del autor.

EF Core ofrece dos estrategias para cargar relaciones:

- **Eager loading** (`Include`): carga la relacion en la misma query SQL con un JOIN. Lo usamos en `GetAllAsync` y `GetByRoomIdAsync`.
- **Explicit loading** (`LoadAsync`): carga la relacion en una query separada *despues* de tener la entidad. Lo usamos en `CreateAsync` — despues de insertar un mensaje, cargamos su User para poder retornar el DTO completo.

### Prompt para Claude Code

```text
En la carpeta Repositories/ de EDChat.Data, crea MessageRepository.cs que implementa IRepository<Message>:

- Primary constructor con EDChatDb
- GetAllAsync: db.Messages.Include(m => m.User).OrderBy(m => m.SentAt).ToListAsync()
- GetByIdAsync: db.Messages.Include(m => m.User).FirstOrDefaultAsync(m => m.Id == id)
- GetByRoomIdAsync(int roomId): metodo extra, db.Messages.Include(m => m.User).Where(m => m.RoomId == roomId).OrderBy(m => m.SentAt).ToListAsync()
- CreateAsync: db.Messages.Add(entity), SaveChangesAsync, luego cargar el User con db.Entry(entity).Reference(m => m.User).LoadAsync(), retornar entity
- UpdateAsync: db.Messages.Update(entity), SaveChangesAsync, retornar entity
- DeleteAsync: buscar con FindAsync, si existe Remove + SaveChangesAsync

Namespace: EDChat.Data.Repositories. Importar EDChat.Data.Entities y Microsoft.EntityFrameworkCore.
```

### Codigo esperado

```csharp
// Repositories/MessageRepository.cs
using EDChat.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EDChat.Data.Repositories;

public class MessageRepository(EDChatDb db) : IRepository<Message>
{
    public Task<List<Message>> GetAllAsync() =>
        db.Messages.Include(m => m.User).OrderBy(m => m.SentAt).ToListAsync();

    public Task<Message?> GetByIdAsync(int id) =>
        db.Messages.Include(m => m.User).FirstOrDefaultAsync(m => m.Id == id);

    public Task<List<Message>> GetByRoomIdAsync(int roomId) =>
        db.Messages.Include(m => m.User).Where(m => m.RoomId == roomId).OrderBy(m => m.SentAt).ToListAsync();

    public async Task<Message> CreateAsync(Message entity)
    {
        db.Messages.Add(entity);
        await db.SaveChangesAsync();
        await db.Entry(entity).Reference(m => m.User).LoadAsync();
        return entity;
    }

    public async Task<Message> UpdateAsync(Message entity)
    {
        db.Messages.Update(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task DeleteAsync(int id)
    {
        var message = await db.Messages.FindAsync(id);
        if (message is not null)
        {
            db.Messages.Remove(message);
            await db.SaveChangesAsync();
        }
    }
}
```

### Puntos clave
- `.Include(m => m.User)` genera un `LEFT JOIN` en SQL — trae el User en la misma consulta
- Sin `Include`, `message.User` seria `null` y `message.User.Username` lanzaria una excepcion
- `db.Entry(entity).Reference(m => m.User).LoadAsync()` es **explicit loading** — carga el User despues del insert para poder mapear a DTO
- `GetByRoomIdAsync` filtra por sala y ordena por fecha — reemplaza a `ChatStore.GetMessagesByRoom()`

---

## Capitulo 4: Registrar repositorios en DI

### Explicacion

Registramos cada repositorio como **Scoped** — una instancia por request HTTP. Esto coincide con el lifetime del DbContext (tambien Scoped).

Para `UserRepository` y `MessageRepository` registramos tanto la interfaz como el tipo concreto, porque algunos endpoints necesitan metodos especificos que no estan en `IRepository<T>` (como `GetByUsernameAsync` y `GetByRoomIdAsync`).

### Prompt para Claude Code

```text
En Program.cs de EDChat.Api, agrega el registro de los repositorios despues del AddDbContext:

builder.Services.AddScoped<IRepository<User>, UserRepository>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<IRepository<Room>, RoomRepository>();
builder.Services.AddScoped<IRepository<Message>, MessageRepository>();
builder.Services.AddScoped<MessageRepository>();

Agrega using EDChat.Data.Entities y using EDChat.Data.Repositories.
Mantener ChatStore — se removera en la Clase 4.
```

### Codigo esperado

```csharp
// Program.cs - agregar despues del AddDbContext
using EDChat.Data.Entities;
using EDChat.Data.Repositories;

// ... (AddDbContext existente)

// Repositories (nuevo)
builder.Services.AddScoped<IRepository<User>, UserRepository>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<IRepository<Room>, RoomRepository>();
builder.Services.AddScoped<IRepository<Message>, MessageRepository>();
builder.Services.AddScoped<MessageRepository>();

// ChatStore sigue activo (se removera en Clase 4)
builder.Services.AddSingleton<ChatStore>();

// ... (resto sin cambios)
```

### Puntos clave
- `AddScoped<IRepository<User>, UserRepository>()` — cuando alguien pida `IRepository<User>`, recibe `UserRepository`
- `AddScoped<UserRepository>()` — registro adicional para inyectar `UserRepository` directamente (para acceder a `GetByUsernameAsync`)
- **Scoped** significa una instancia por request — el repositorio y el DbContext comparten el mismo scope

---

## Estado del proyecto al finalizar

### Archivos creados
```
EDChat.Data/
└── Repositories/
    ├── IRepository.cs                <-- NUEVO
    ├── UserRepository.cs             <-- NUEVO
    ├── RoomRepository.cs             <-- NUEVO
    └── MessageRepository.cs          <-- NUEVO

EDChat.Api/
└── Program.cs                        <-- MODIFICADO (registro de repos)
```

### Verificacion

```bash
dotnet build
```

Debe compilar sin errores. Los repositorios estan registrados en DI pero no se usan todavia — los endpoints siguen usando `ChatStore`. En la Clase 4 haremos el cambio completo.
