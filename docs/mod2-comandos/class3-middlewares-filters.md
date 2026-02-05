# Clase 3: Middlewares y Endpoint Filters
> Duracion estimada: ~12 min

---

## Capitulo 1: Pipeline de middlewares en .NET 10

### Explicacion

Cada middleware puede hacer algo antes de `next()`, pasar el request al siguiente, y hacer algo despues. El orden importa: `UseExceptionHandler` debe ir primero para capturar excepciones de todo el pipeline.

---

## Capitulo 2: Middlewares built-in (CORS, Authentication, etc)

### Explicacion

.NET incluye middlewares listos para usar: `UseCors`, `UseAuthentication`, `UseAuthorization`, `UseStaticFiles`, etc. Configuramos CORS para que Blazor (`localhost:5003`) pueda hacer requests al API (`localhost:5001`). `AllowCredentials` es necesario para SignalR (Modulo 3).

### Prompt para Claude Code

```text
Agrega configuracion de CORS en Program.cs. En los servicios, usa AddCors con una default policy que permita los origenes "https://localhost:5002" y "http://localhost:5003", con AllowAnyHeader, AllowAnyMethod y AllowCredentials. En el pipeline, agrega app.UseCors() antes de los endpoints.
```

### Codigo esperado

```csharp
// Program.cs con CORS
using EDChat.Api.Endpoints;
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

var app = builder.Build();

app.UseCors();

app.MapUserEndpoints();
app.MapRoomEndpoints();
app.MapMessageEndpoints();

app.Run();
```

---

## Capitulo 3: Crear middleware personalizado

### Explicacion

Un **middleware** es un componente que intercepta cada request HTTP antes de llegar a los endpoints — puede modificar el request, hacer logging, o cortar el flujo (short-circuit).

Creamos un middleware que registre el tiempo de cada request usando `IMiddleware` (se integra con DI y requiere registro explicito).

### Comandos

```bash
cd src/EDChat.Api
mkdir Middlewares
```

### Prompt para Claude Code

```text
En la carpeta Middlewares/ de EDChat.Api, crea RequestLoggingMiddleware.cs que implemente IMiddleware:

- Recibe ILogger<RequestLoggingMiddleware> por constructor (primary constructor)
- En InvokeAsync: crea un Stopwatch, llama await next(context), detiene el stopwatch y logea con logger.LogInformation el metodo HTTP, path, status code y tiempo en milisegundos
- Formato del log: "{Method} {Path} -> {StatusCode} en {Elapsed}ms"

Actualiza Program.cs:
- Registra el middleware en DI con builder.Services.AddSingleton<RequestLoggingMiddleware>()
- Agrega app.UseMiddleware<RequestLoggingMiddleware>() en el pipeline (despues de UseCors, antes de los Map)
```

### Codigo esperado

```csharp
// Middlewares/RequestLoggingMiddleware.cs
using System.Diagnostics;

namespace EDChat.Api.Middlewares;

public class RequestLoggingMiddleware(ILogger<RequestLoggingMiddleware> logger) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var stopwatch = Stopwatch.StartNew();

        await next(context);

        stopwatch.Stop();
        logger.LogInformation("{Method} {Path} -> {StatusCode} en {Elapsed}ms",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            stopwatch.ElapsedMilliseconds);
    }
}
```

```csharp
// Program.cs actualizado
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

var app = builder.Build();

app.UseCors();
app.UseMiddleware<RequestLoggingMiddleware>();

app.MapUserEndpoints();
app.MapRoomEndpoints();
app.MapMessageEndpoints();

app.Run();
```

---

## Capitulo 4: Endpoint filters

### Explicacion

A diferencia de middlewares (se ejecutan para todos los requests), los **endpoint filters** se ejecutan solo en los endpoints donde se aplican. Ideales para logica de negocio: verificar si ya existe una sala con el mismo nombre.

### Prompt para Claude Code

```text
Agrega un endpoint filter inline al POST de rooms en RoomEndpoints.cs.

El filter debe:
- Obtener el CreateRoomDto de los argumentos con context.GetArgument<CreateRoomDto>(0)
- Obtener ChatStore del DI con context.HttpContext.RequestServices.GetRequiredService<ChatStore>()
- Verificar si ya existe una sala con el mismo nombre (comparacion case-insensitive)
- Si existe, retornar TypedResults.Conflict(new { error = "Ya existe una sala con el nombre '...'" })
- Si no existe, continuar con await next(context)
```

### Codigo esperado

```csharp
// RoomEndpoints.cs - solo el POST modificado con filter
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
});
```

### Verificacion

```bash
cd src/EDChat.Api
dotnet run &

# Crear una sala
curl -s -X POST http://localhost:5001/api/rooms \
  -H "Content-Type: application/json" \
  -d '{"name":"Deportes","description":"Sala de deportes"}'
# Respuesta: HTTP 201

# Intentar crear sala con el mismo nombre
curl -s -X POST http://localhost:5001/api/rooms \
  -H "Content-Type: application/json" \
  -d '{"name":"Deportes","description":"Otra descripcion"}'
# Respuesta: HTTP 409 Conflict

# Intentar con diferente capitalizacion
curl -s -X POST http://localhost:5001/api/rooms \
  -H "Content-Type: application/json" \
  -d '{"name":"deportes","description":"minusculas"}'
# Respuesta: HTTP 409 Conflict (comparacion case-insensitive)

# Intentar con "General" (sala seed)
curl -s -X POST http://localhost:5001/api/rooms \
  -H "Content-Type: application/json" \
  -d '{"name":"General","description":"Ya existe"}'
# Respuesta: HTTP 409 Conflict

# Ctrl+C para detener
```

### Puntos clave
- `context.GetArgument<T>(index)` obtiene los parametros por posicion. Alternativa: `context.Arguments.OfType<T>().Single()` busca por tipo
- Si el filtro no llama a `next(context)`, el handler nunca se ejecuta (short-circuit)

---

## Capitulo 5: Logging de requests y responses

### Comandos

```bash
cd src/EDChat.Api
dotnet build
dotnet run &

# Hacer algunas requests para ver el logging
curl http://localhost:5001/api/rooms
curl http://localhost:5001/api/users
curl -X POST http://localhost:5001/api/rooms \
  -H "Content-Type: application/json" \
  -d '{"name":"Test","description":"Test room"}'

# En la terminal del servidor deberian aparecer lineas como:
# info: EDChat.Api.Middlewares.RequestLoggingMiddleware
#       GET /api/rooms -> 200 en 5ms
# info: EDChat.Api.Middlewares.RequestLoggingMiddleware
#       GET /api/users -> 200 en 1ms
# info: EDChat.Api.Middlewares.RequestLoggingMiddleware
#       POST /api/rooms -> 201 en 2ms

# Ctrl+C para detener
```

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
│   ├── UserEndpoints.cs
│   ├── RoomEndpoints.cs              ← MODIFICADO (filter)
│   └── MessageEndpoints.cs
├── Mappers/
│   └── DtoMappers.cs
├── Middlewares/
│   └── RequestLoggingMiddleware.cs   ← NUEVO
├── Models/
│   ├── User.cs
│   ├── Room.cs
│   └── Message.cs
├── Services/
│   └── ChatStore.cs
├── Program.cs                        ← MODIFICADO (CORS, middleware)
└── EDChat.Api.csproj
```

### Verificacion

```bash
cd src/EDChat.Api
dotnet build
```

Debe compilar sin errores. CORS esta configurado, el middleware logea todas las requests y el endpoint filter previene salas duplicadas.
