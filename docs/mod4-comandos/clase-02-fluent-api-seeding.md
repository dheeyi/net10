# Clase 2: Fluent API y datos iniciales
> Duracion estimada: ~12 min

---

## Capitulo 1: Fluent API - configurar propiedades

### Explicacion

EF Core necesita saber las reglas de cada columna: cual es la primary key, que campos son obligatorios, longitudes maximas, indices unicos, etc. La **Fluent API** configura esto en `OnModelCreating` del DbContext.

Empezamos configurando las propiedades basicas de cada entidad.

### Prompt para Claude Code

```text
En EDChatDb.cs de EDChat.Data, agrega el metodo override OnModelCreating(ModelBuilder modelBuilder). Configura las propiedades:

Para User:
- HasKey(u => u.Id)
- Property Username: IsRequired(), HasMaxLength(50)
- HasIndex(u => u.Username).IsUnique() — para que no haya dos usuarios con el mismo nombre

Para Room:
- HasKey(r => r.Id)
- Property Name: IsRequired(), HasMaxLength(100)
- Property Description: HasMaxLength(500)

Para Message:
- HasKey(m => m.Id)
- Property Content: IsRequired(), HasMaxLength(2000)

NO configurar relaciones todavia — eso se hace en el siguiente capitulo.
```

### Codigo esperado

```csharp
// EDChatDb.cs - agregar dentro de la clase
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<User>(entity =>
    {
        entity.HasKey(u => u.Id);
        entity.Property(u => u.Username).IsRequired().HasMaxLength(50);
        entity.HasIndex(u => u.Username).IsUnique();
    });

    modelBuilder.Entity<Room>(entity =>
    {
        entity.HasKey(r => r.Id);
        entity.Property(r => r.Name).IsRequired().HasMaxLength(100);
        entity.Property(r => r.Description).HasMaxLength(500);
    });

    modelBuilder.Entity<Message>(entity =>
    {
        entity.HasKey(m => m.Id);
        entity.Property(m => m.Content).IsRequired().HasMaxLength(2000);
    });
}
```

### Puntos clave
- `HasIndex().IsUnique()` en Username crea un indice unico en la BD — la BD misma rechaza duplicados
- `IsRequired()` marca la columna como NOT NULL
- `HasMaxLength()` define el tamanio maximo de la columna

---

## Capitulo 2: Fluent API - configurar relaciones

### Explicacion

Las relaciones se definen con `HasOne().WithMany().HasForeignKey()`. EDChat tiene dos relaciones **One-to-Many**:

- **User → Messages**: Un usuario tiene muchos mensajes. Cada mensaje pertenece a un usuario.
- **Room → Messages**: Una sala tiene muchos mensajes. Cada mensaje pertenece a una sala.

```
User (1) ──── (*) Message (*) ──── (1) Room
```

### Prompt para Claude Code

```text
En el metodo OnModelCreating de EDChatDb.cs, dentro del bloque de configuracion de Message, agrega las relaciones:

- entity.HasOne(m => m.User).WithMany(u => u.Messages).HasForeignKey(m => m.UserId)
- entity.HasOne(m => m.Room).WithMany(r => r.Messages).HasForeignKey(m => m.RoomId)
```

### Codigo esperado

```csharp
// Agregar dentro del bloque modelBuilder.Entity<Message>(...) de EDChatDb.cs
entity.HasOne(m => m.User).WithMany(u => u.Messages).HasForeignKey(m => m.UserId);
entity.HasOne(m => m.Room).WithMany(r => r.Messages).HasForeignKey(m => m.RoomId);
```

### Puntos clave
- `HasOne(m => m.User)` — cada Message tiene UN User
- `WithMany(u => u.Messages)` — cada User tiene MUCHOS Messages
- `HasForeignKey(m => m.UserId)` — la FK en la tabla Messages es UserId
- EF Core crea las foreign keys y restricciones automaticamente en la BD

---

## Capitulo 3: Data Annotations vs Fluent API

### Explicacion

EF Core soporta dos formas de configurar el modelo:

| Aspecto | Data Annotations | Fluent API |
|---------|-----------------|------------|
| Donde se usa | Sobre las propiedades (`[Required]`, `[MaxLength]`) | En `OnModelCreating` |
| Proposito principal | Validacion de input (DTOs) | Configuracion de persistencia (entidades) |
| Flexibilidad | Limitada | Completa (relaciones, indices, seed data) |

**En EDChat usamos ambas:**
- **Data Annotations en DTOs** (Modulo 2): validacion del input del usuario antes de llegar al handler
- **Fluent API en entidades** (este modulo): configuracion de la base de datos

Esta separacion es intencional — los DTOs son el contrato de la API, las entidades son el modelo de datos. Pueden evolucionar independientemente.

**Relaciones Many-to-Many**: EF Core tambien soporta relaciones muchos-a-muchos (por ejemplo, usuarios que pertenecen a multiples salas). Se configuran con `HasMany().WithMany()` y EF Core crea la tabla intermedia automaticamente. En EDChat no tenemos este caso — las salas son publicas y no tienen membresia.

---

## Capitulo 4: Seed data con HasData

### Explicacion

`HasData()` define datos iniciales que EF Core inserta al crear la base de datos. Agregamos las dos salas por defecto que antes estaban hardcodeadas en `ChatStore`.

> **Nota:** Los `Id` deben especificarse explicitamente en seed data porque EF Core necesita saber las primary keys por adelantado. `CreatedAt` usa una fecha fija para que los datos seed sean deterministas.

### Prompt para Claude Code

```text
En el metodo OnModelCreating de EDChatDb.cs, despues de la configuracion de Message, agrega seed data para Room usando modelBuilder.Entity<Room>().HasData():

- Room con Id=1, Name="General", Description="Sala de chat general", CreatedAt=new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
- Room con Id=2, Name="Tecnologia", Description="Discusiones sobre tecnologia", CreatedAt=new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
```

### Codigo esperado

```csharp
// Agregar al final de OnModelCreating en EDChatDb.cs
modelBuilder.Entity<Room>().HasData(
    new Room { Id = 1, Name = "General", Description = "Sala de chat general", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
    new Room { Id = 2, Name = "Tecnología", Description = "Discusiones sobre tecnología", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
);
```

### Puntos clave
- Los mismos datos que tenia `ChatStore` ahora viven en la BD
- `DateTimeKind.Utc` asegura que las fechas seed sean consistentes independientemente del timezone del servidor

---

## Capitulo 5: Crear la base de datos con EnsureCreated

### Explicacion

`EnsureCreated()` crea la base de datos y todas las tablas si no existen. Es ideal para desarrollo y prototipos. Para produccion se usan **migrations** — que generan scripts SQL incrementales para actualizar el schema sin perder datos.

Agregamos el bloque de creacion de BD al inicio del pipeline en `Program.cs`.

> **Migrations en produccion:** En lugar de `EnsureCreated`, se usa `dotnet ef migrations add NombreMigracion` para generar archivos de migracion que representan cada cambio al schema. Luego `dotnet ef database update` aplica los cambios. Esto permite control de versiones del schema y rollback. Para este curso, `EnsureCreated` es suficiente.

### Prompt para Claude Code

```text
En Program.cs de EDChat.Api, despues de var app = builder.Build() y antes del UseExceptionHandler, agrega un bloque que cree la BD:

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EDChatDb>();
    db.Database.EnsureCreated();
}

Esto crea un scope de DI temporal para obtener el DbContext y crear la BD.
```

### Codigo esperado

```csharp
// Program.cs - despues de var app = builder.Build()
var app = builder.Build();

// Crear BD automaticamente
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EDChatDb>();
    db.Database.EnsureCreated();
}

// ... (resto del pipeline sin cambios)
```

### Verificacion

```bash
# Ejecutar el API para que cree la BD
dotnet run --project src/EDChat.Api &

# Verificar que se creo el archivo de la BD
ls src/EDChat.Api/edchat.db

# Ctrl+C para detener
```

### Puntos clave
- `CreateScope()` es necesario porque `EDChatDb` esta registrado como Scoped (un scope por request)
- `EnsureCreated()` no hace nada si la BD ya existe — es seguro ejecutarlo multiples veces
- El archivo `edchat.db` se crea en el directorio de trabajo del API

---

## Estado del proyecto al finalizar

### Archivos modificados
```
EDChat.Data/
└── EDChatDb.cs                       <-- MODIFICADO (OnModelCreating completo)

EDChat.Api/
└── Program.cs                        <-- MODIFICADO (EnsureCreated)
```

### EDChatDb.cs completo

```csharp
using EDChat.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EDChat.Data;

public class EDChatDb(DbContextOptions<EDChatDb> options) : DbContext(options)
{
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Username).IsRequired().HasMaxLength(50);
            entity.HasIndex(u => u.Username).IsUnique();
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Name).IsRequired().HasMaxLength(100);
            entity.Property(r => r.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Content).IsRequired().HasMaxLength(2000);
            entity.HasOne(m => m.User).WithMany(u => u.Messages).HasForeignKey(m => m.UserId);
            entity.HasOne(m => m.Room).WithMany(r => r.Messages).HasForeignKey(m => m.RoomId);
        });

        modelBuilder.Entity<Room>().HasData(
            new Room { Id = 1, Name = "General", Description = "Sala de chat general", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Room { Id = 2, Name = "Tecnología", Description = "Discusiones sobre tecnología", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}
```

### Verificacion

```bash
dotnet build
dotnet run --project src/EDChat.Api &

# Verificar que la BD se creo con las tablas y seed data
# (el API deberia iniciar sin errores y edchat.db deberia existir)

# Ctrl+C para detener
```

Debe compilar y ejecutar sin errores. Al iniciar, se crea `edchat.db` con las tablas Users, Rooms, Messages y las 2 salas seed. ChatStore sigue activo — la app funciona igual que antes.
