# Clase 1: Configuracion inicial de EF Core 10
> Duracion estimada: ~12 min

---

## Capitulo 1: Crear proyecto EDChat.Data

### Explicacion

Hasta ahora todos los datos viven en `ChatStore` — un singleton in-memory que pierde todo al reiniciar. En este modulo migramos a **Entity Framework Core 10** con SQLite para persistencia real.

Separamos el acceso a datos en un proyecto independiente `EDChat.Data` (class library). Esto sigue el principio de separacion de responsabilidades: el API no sabe como se guardan los datos, solo consume interfaces.

### Comandos

```bash
# Desde la raiz de la solucion EDChat/
dotnet new classlib -n EDChat.Data -o src/EDChat.Data
dotnet sln add src/EDChat.Data

# Instalar EF Core en el proyecto de datos
dotnet add src/EDChat.Data package Microsoft.EntityFrameworkCore

# Instalar el provider SQLite en el API (el provider va en el proyecto que configura la conexion)
dotnet add src/EDChat.Api package Microsoft.EntityFrameworkCore.Sqlite

# Agregar referencia de proyecto: el API depende de Data
dotnet add src/EDChat.Api reference src/EDChat.Data

# Eliminar el archivo Class1.cs que viene por defecto
rm src/EDChat.Data/Class1.cs

dotnet build
```

### Puntos clave
- `Microsoft.EntityFrameworkCore` va en EDChat.Data (el ORM base)
- `Microsoft.EntityFrameworkCore.Sqlite` va en EDChat.Api (el provider especifico)
- La referencia de proyecto permite al API usar las clases de Data

---

## Capitulo 2: Crear entidades

### Explicacion

Las **entidades** representan tablas en la base de datos. Son similares a los modelos de `EDChat.Api/Models/`, pero con diferencias importantes:

- `Message` ya no tiene `string Username` — en su lugar tiene una **navigation property** `User User` que EF Core resuelve automaticamente via la relacion
- Cada entidad con relacion tiene una **foreign key** (`UserId`, `RoomId`) y su **navigation property** correspondiente
- `User` y `Room` tienen `List<Message> Messages` — la coleccion inversa de la relacion

### Comandos

```bash
cd src/EDChat.Data
mkdir Entities
```

### Prompt para Claude Code

```text
En la carpeta Entities/ de EDChat.Data, crea tres archivos:

User.cs - propiedades: int Id, string Username (default string.Empty), DateTime CreatedAt (default DateTime.UtcNow), List<Message> Messages (default lista vacia)

Room.cs - propiedades: int Id, string Name (default string.Empty), string Description (default string.Empty), DateTime CreatedAt (default DateTime.UtcNow), List<Message> Messages (default lista vacia)

Message.cs - propiedades: int Id, string Content (default string.Empty), DateTime SentAt (default DateTime.UtcNow), int UserId, User User (default null! para satisfacer nullability), int RoomId, Room Room (default null!). NO incluir string Username.

Namespace: EDChat.Data.Entities. Clases publicas simples con propiedades auto-implementadas.
```

### Codigo esperado

```csharp
// Entities/User.cs
namespace EDChat.Data.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Message> Messages { get; set; } = [];
}
```

```csharp
// Entities/Room.cs
namespace EDChat.Data.Entities;

public class Room
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Message> Messages { get; set; } = [];
}
```

```csharp
// Entities/Message.cs
namespace EDChat.Data.Entities;

public class Message
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;
}
```

### Puntos clave
- `null!` en las navigation properties le dice al compilador "confio en que EF Core las llenara" — evita warnings de nullability
- `List<Message> Messages = []` es la coleccion inversa — permite navegar de User/Room a sus mensajes
- `Message` ya NO tiene `string Username` — el username se obtiene via `message.User.Username`

---

## Capitulo 3: Crear el DbContext

### Explicacion

El **DbContext** es la clase central de EF Core. Representa una sesion con la base de datos y expone `DbSet<T>` para cada tabla. Usamos **primary constructor** para recibir las opciones de configuracion.

### Prompt para Claude Code

```text
En la raiz de EDChat.Data, crea EDChatDb.cs. Es un DbContext con primary constructor que recibe DbContextOptions<EDChatDb>:

- Hereda de DbContext pasando options al constructor base
- Tres propiedades DbSet: Rooms, Users, Messages (usando la sintaxis => Set<T>())
- Importar Microsoft.EntityFrameworkCore y EDChat.Data.Entities
- Namespace: EDChat.Data

No agregar OnModelCreating todavia — eso se hace en la Clase 2.
```

### Codigo esperado

```csharp
// EDChatDb.cs
using EDChat.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EDChat.Data;

public class EDChatDb(DbContextOptions<EDChatDb> options) : DbContext(options)
{
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Message> Messages => Set<Message>();
}
```

### Puntos clave
- `DbContextOptions<EDChatDb>` permite configurar el provider (SQLite, SQL Server, etc.) desde fuera
- `=> Set<T>()` es una expression-bodied property — equivale a un getter que retorna el DbSet
- El primary constructor `EDChatDb(options) : DbContext(options)` es la sintaxis compacta de C# 12

---

## Capitulo 4: Registrar el DbContext en el API

### Explicacion

Configuramos la conexion a SQLite en `appsettings.json` y registramos el DbContext en el contenedor de DI. El ChatStore sigue activo — coexisten temporalmente mientras migramos.

### Prompt para Claude Code

```text
En appsettings.json de EDChat.Api, agrega una seccion ConnectionStrings con "DefaultConnection": "Data Source=edchat.db".

En Program.cs de EDChat.Api, agrega el registro del DbContext despues del builder:
- builder.Services.AddDbContext<EDChatDb>(options => options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=edchat.db"))
- Agrega using EDChat.Data y using Microsoft.EntityFrameworkCore
- Mantener ChatStore y todo lo existente sin cambios
```

### Codigo esperado

```json
// appsettings.json - agregar ConnectionStrings
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=edchat.db"
  }
}
```

```csharp
// Program.cs - agregar despues de var builder = ...
using EDChat.Data;
using Microsoft.EntityFrameworkCore;

// ... (usings existentes)

var builder = WebApplication.CreateBuilder(args);

// EF Core + SQLite (nuevo)
builder.Services.AddDbContext<EDChatDb>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=edchat.db"));

// ChatStore sigue activo (se removera en Clase 4)
builder.Services.AddSingleton<ChatStore>();

// ... (resto sin cambios)
```

---

## Capitulo 5: Providers de EF Core

### Explicacion

EF Core soporta multiples bases de datos a traves de **providers**. El mismo DbContext funciona con cualquiera — solo cambia la linea de configuracion:

| Provider | Paquete NuGet | Uso tipico |
|----------|--------------|------------|
| **SQLite** | `Microsoft.EntityFrameworkCore.Sqlite` | Desarrollo, apps locales |
| **SQL Server** | `Microsoft.EntityFrameworkCore.SqlServer` | Produccion Windows/Azure |
| **PostgreSQL** | `Npgsql.EntityFrameworkCore.PostgreSQL` | Produccion Linux/cloud |
| **In-Memory** | `Microsoft.EntityFrameworkCore.InMemory` | Testing |

Para cambiar de SQLite a SQL Server en produccion, solo cambias:

```csharp
// Desarrollo (SQLite)
options.UseSqlite("Data Source=edchat.db");

// Produccion (SQL Server)
options.UseSqlServer("Server=...;Database=EDChat;...");
```

El codigo de las entidades, DbContext y repositorios no cambia. Esta es una de las ventajas principales de usar un ORM.

---

## Estado del proyecto al finalizar

### Archivos creados/modificados
```
EDChat.Data/                          <-- NUEVO proyecto
├── EDChat.Data.csproj
├── EDChatDb.cs
└── Entities/
    ├── User.cs
    ├── Room.cs
    └── Message.cs

EDChat.Api/
├── appsettings.json                  <-- MODIFICADO (ConnectionStrings)
├── Program.cs                        <-- MODIFICADO (AddDbContext)
└── EDChat.Api.csproj                 <-- MODIFICADO (SQLite package, referencia a Data)

EDChat.slnx                           <-- MODIFICADO (nuevo proyecto)
```

### Verificacion

```bash
dotnet build
```

Debe compilar sin errores en toda la solucion. ChatStore sigue funcionando como antes. El DbContext esta registrado pero sin usar todavia — no hay tablas ni datos.
