# Clase 4: Validacion, manejo de errores y OpenAPI 3.1
> Duracion estimada: ~12 min

---

## Capitulo 1: Validacion nativa de .NET 10 con Data Annotations

### Explicacion

En la Clase 2 creamos los DTOs con Data Annotations, pero todavia no hacen nada. Ahora las activamos con una sola linea.

**Data Annotations + Endpoint Filters** trabajan juntos:
- **Data Annotations** (`AddValidation()`): validan formato y estructura automaticamente → HTTP 400
- **Endpoint Filters** (Clase 3): validan logica de negocio (duplicados) → HTTP 409

### Prompt para Claude Code

```text
Agrega builder.Services.AddValidation() en Program.cs (despues de AddSingleton<RequestLoggingMiddleware>).
```

### Codigo esperado

```csharp
// Program.cs - agregar validacion
using EDChat.Api.Endpoints;
using EDChat.Api.Middlewares;
using EDChat.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ChatStore>();
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
builder.Services.AddSingleton<RequestLoggingMiddleware>();
builder.Services.AddValidation();

var app = builder.Build();

app.UseCors();
app.UseMiddleware<RequestLoggingMiddleware>();

app.MapUserEndpoints();
app.MapRoomEndpoints();
app.MapMessageEndpoints();

app.Run();
```

### Verificacion

```bash
cd src/EDChat.Api
dotnet run &

# Request invalido - nombre vacio
curl -s -X POST http://localhost:5001/api/rooms \
  -H "Content-Type: application/json" \
  -d '{"name":"","description":"test"}'
# Respuesta esperada: HTTP 400

# Request invalido - nombre demasiado largo (mas de 100 caracteres)
curl -s -X POST http://localhost:5001/api/rooms \
  -H "Content-Type: application/json" \
  -d '{"name":"Este es un nombre extremadamente largo que supera el limite de cien caracteres establecido en el validador de CreateRoomDto","description":"test"}'
# Respuesta esperada: HTTP 400

# Request valido
curl -s -X POST http://localhost:5001/api/rooms \
  -H "Content-Type: application/json" \
  -d '{"name":"Deportes","description":"Sala de deportes"}'
# Respuesta esperada: HTTP 201

# Ctrl+C para detener
```

### Puntos clave
- **Una linea** activa toda la validacion: `builder.Services.AddValidation()`
- Si la validacion falla, el handler del endpoint **nunca se ejecuta**
- La respuesta de error incluye `title` y `errors` agrupados por propiedad

---

## Capitulo 2: Manejo global de excepciones

### Explicacion

`UseExceptionHandler` captura todas las excepciones no manejadas y retorna JSON consistente en vez de stack traces.

### Prompt para Claude Code

```text
Agrega manejo global de excepciones en Program.cs usando app.UseExceptionHandler(...) con un handler inline. Debe ser el primer middleware del pipeline (antes de UseCors). El handler establece status code 500, content type "application/json" y retorna un JSON con la propiedad error: "Ocurrio un error interno en el servidor". Usa WriteAsJsonAsync.
```

### Codigo esperado

```csharp
// Program.cs - seccion del pipeline
var app = builder.Build();

app.UseExceptionHandler(error =>
{
    error.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = "Ocurrio un error interno en el servidor" });
    });
});

app.UseCors();
app.UseMiddleware<RequestLoggingMiddleware>();

app.MapUserEndpoints();
app.MapRoomEndpoints();
app.MapMessageEndpoints();

app.Run();
```

### Puntos clave
- `UseExceptionHandler` debe ir **primero** en el pipeline
- La respuesta es siempre JSON con un mensaje generico - nunca exponemos el stack trace

---

## Capitulo 3: OpenAPI 3.1 nativo

### Explicacion

.NET 10 incluye soporte nativo de OpenAPI 3.1 con `Microsoft.AspNetCore.OpenApi`. Se integra con `TypedResults` para generar schemas exactos. **Scalar** es una UI moderna para visualizar la documentacion.

### Comandos

```bash
cd src/EDChat.Api
dotnet add package Microsoft.AspNetCore.OpenApi
dotnet add package Scalar.AspNetCore
```

### Prompt para Claude Code

```text
Actualiza Program.cs para agregar soporte de OpenAPI con UI visual:

- Agrega using Scalar.AspNetCore
- En la seccion de servicios: builder.Services.AddOpenApi()
- Despues del middleware de logging: app.MapOpenApi() y app.MapScalarApiReference()
```

### Codigo esperado

```csharp
// Program.cs con OpenAPI
using EDChat.Api.Endpoints;
using EDChat.Api.Middlewares;
using EDChat.Api.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ChatStore>();
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
builder.Services.AddSingleton<RequestLoggingMiddleware>();
builder.Services.AddValidation();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler(error =>
{
    error.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = "Ocurrio un error interno en el servidor" });
    });
});

app.UseCors();
app.UseMiddleware<RequestLoggingMiddleware>();

app.MapOpenApi();
app.MapScalarApiReference();

app.MapUserEndpoints();
app.MapRoomEndpoints();
app.MapMessageEndpoints();

app.Run();
```

### Verificacion

```bash
cd src/EDChat.Api
dotnet build
```

---

## Capitulo 4: Documentar endpoints

### Explicacion

`WithTags` agrupa endpoints, `WithName` da nombre unico, `WithSummary` agrega descripcion.

### Prompt para Claude Code

```text
Actualiza los tres archivos de endpoints para agregar metadata de OpenAPI:

En RoomEndpoints.cs:
- Al grupo: .WithTags("Rooms")
- GET: .WithName("GetAllRooms").WithSummary("Obtiene todas las salas")
- POST: .WithName("CreateRoom").WithSummary("Crea una nueva sala")
- PUT: .WithName("UpdateRoom").WithSummary("Actualiza una sala existente")
- DELETE: .WithName("DeleteRoom").WithSummary("Elimina una sala")

En UserEndpoints.cs:
- Al grupo: .WithTags("Users")
- GET: .WithName("GetAllUsers").WithSummary("Obtiene todos los usuarios")
- POST: .WithName("CreateUser").WithSummary("Crea un nuevo usuario o retorna el existente")

En MessageEndpoints.cs:
- Al grupo: .WithTags("Messages")
- GET: .WithName("GetRoomMessages").WithSummary("Obtiene los mensajes de una sala")
- POST: .WithName("CreateMessage").WithSummary("Envia un mensaje a una sala")
```

### Codigo esperado

```csharp
// RoomEndpoints.cs con metadata
public static RouteGroupBuilder MapRoomEndpoints(this WebApplication app)
{
    var group = app.MapGroup("/api/rooms").WithTags("Rooms");

    group.MapGet("/", (ChatStore store) =>
        TypedResults.Ok(store.GetAllRooms().Select(r => r.ToDto())))
    .WithName("GetAllRooms")
    .WithSummary("Obtiene todas las salas");

    group.MapPost("/", (CreateRoomDto dto, ChatStore store) =>
    {
        var room = dto.ToEntity();
        store.CreateRoom(room);
        return TypedResults.Created($"/api/rooms/{room.Id}", room.ToDto());
    })
    .AddEndpointFilter(async (context, next) =>
    {
        var dto = context.GetArgument<CreateRoomDto>(0);
        var store = context.HttpContext.RequestServices.GetRequiredService<ChatStore>();

        if (store.GetAllRooms().Any(r => r.Name.Equals(dto.Name, StringComparison.OrdinalIgnoreCase)))
            return TypedResults.Conflict(new { error = $"Ya existe una sala con el nombre '{dto.Name}'" });

        return await next(context);
    })
    .WithName("CreateRoom")
    .WithSummary("Crea una nueva sala");

    group.MapPut("/{id:int}", Results<Ok<RoomDto>, NotFound> (int id, UpdateRoomDto dto, ChatStore store) =>
    {
        var updated = store.UpdateRoom(id, dto.Name, dto.Description);
        if (updated is null)
            return TypedResults.NotFound();

        return TypedResults.Ok(updated.ToDto());
    })
    .WithName("UpdateRoom")
    .WithSummary("Actualiza una sala existente");

    group.MapDelete("/{id:int}", Results<NoContent, NotFound> (int id, ChatStore store) =>
    {
        var deleted = store.DeleteRoom(id);
        if (!deleted)
            return TypedResults.NotFound();

        return TypedResults.NoContent();
    })
    .WithName("DeleteRoom")
    .WithSummary("Elimina una sala");

    return group;
}
```

---

## Capitulo 5: Ejecutar el proyecto

### Verificacion

```bash
cd src/EDChat.Api
dotnet build
dotnet run &

# Verificar endpoints
curl http://localhost:5001/api/rooms
curl http://localhost:5001/api/users

# Verificar validacion (debe retornar 400)
curl -X POST http://localhost:5001/api/rooms \
  -H "Content-Type: application/json" \
  -d '{"name":"","description":"test"}'

# Ctrl+C para detener
```

**URLs de documentacion:**
- JSON: http://localhost:5001/openapi/v1.json
- UI visual: http://localhost:5001/scalar/v1

Abre http://localhost:5001/scalar/v1 en el navegador para explorar y probar los endpoints interactivamente.

---

## Estado del proyecto al finalizar

### Archivos creados/modificados
```
EDChat.Api/
├── DTOs/
│   ├── UserDto.cs
│   ├── RoomDto.cs
│   └── MessageDto.cs
├── Endpoints/
│   ├── UserEndpoints.cs              ← MODIFICADO (metadata)
│   ├── RoomEndpoints.cs              ← MODIFICADO (metadata)
│   └── MessageEndpoints.cs           ← MODIFICADO (metadata)
├── Mappers/
│   └── DtoMappers.cs
├── Middlewares/
│   └── RequestLoggingMiddleware.cs
├── Models/
│   ├── User.cs
│   ├── Room.cs
│   └── Message.cs
├── Services/
│   └── ChatStore.cs
├── Program.cs                        ← MODIFICADO (validacion, exception handler, OpenAPI)
└── EDChat.Api.csproj                 ← MODIFICADO (OpenAPI package)
```

### Verificacion

```bash
cd src/EDChat.Api
dotnet build
```

La API esta completa con: CRUD, DTOs, Mappers, Validacion nativa, Endpoint Filter, Error Handling, OpenAPI y CORS.
