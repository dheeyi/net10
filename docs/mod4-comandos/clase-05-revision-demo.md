# Clase 5: Revision y demo end-to-end
> Duracion estimada: ~12 min

---

## Capitulo 1: Demo CRUD - verificar persistencia

### Explicacion

La diferencia fundamental con `ChatStore` es que ahora los datos **persisten entre reinicios**. Vamos a verificarlo creando datos, deteniendo el servidor, reiniciando y comprobando que todo sigue ahi.

### Verificacion

```bash
# Eliminar BD anterior para empezar limpio
rm src/EDChat.Api/edchat.db 2>/dev/null

# Iniciar el API
dotnet run --project src/EDChat.Api &

# === Crear datos ===

# Crear usuarios
curl -s -X POST http://localhost:5001/api/users \
  -H "Content-Type: application/json" \
  -d '{"username":"carlos"}'

curl -s -X POST http://localhost:5001/api/users \
  -H "Content-Type: application/json" \
  -d '{"username":"maria"}'

# Crear una sala
curl -s -X POST http://localhost:5001/api/rooms \
  -H "Content-Type: application/json" \
  -d '{"name":"Deportes","description":"Sala de deportes"}'

# Verificar que existen
curl -s http://localhost:5001/api/users
curl -s http://localhost:5001/api/rooms

# Actualizar sala
curl -s -X PUT http://localhost:5001/api/rooms/3 \
  -H "Content-Type: application/json" \
  -d '{"name":"Deportes y Fitness","description":"Deportes y actividad fisica"}'

# Verificar actualizacion
curl -s http://localhost:5001/api/rooms

# === Detener y reiniciar ===
# Ctrl+C para detener
dotnet run --project src/EDChat.Api &

# Verificar que los datos persisten
curl -s http://localhost:5001/api/users
# Debe mostrar carlos y maria

curl -s http://localhost:5001/api/rooms
# Debe mostrar General, Tecnologia y Deportes y Fitness

# === Eliminar ===
curl -s -X DELETE http://localhost:5001/api/rooms/3
curl -s http://localhost:5001/api/rooms
# Debe mostrar solo General y Tecnologia

# Ctrl+C para detener
```

### Puntos clave
- Con `ChatStore`, al reiniciar solo quedaban las 2 salas seed. Ahora todo persiste en `edchat.db`
- Las salas seed (General, Tecnologia) se crean con `HasData` solo si la BD no existe

---

## Capitulo 2: Demo relaciones - navigation properties

### Explicacion

Una de las ventajas de EF Core es que las relaciones se resuelven automaticamente. Cuando enviamos un mensaje, no necesitamos incluir el `Username` — EF Core lo obtiene via la navigation property `Message.User.Username`.

El flujo es:
1. El usuario envia un mensaje por SignalR con su `userId`
2. `ChatHub` crea un `Message` con `UserId` (sin Username)
3. `MessageRepository.CreateAsync` guarda el mensaje y carga el `User` con `LoadAsync`
4. El mapper accede a `message.User.Username` para construir el DTO
5. El DTO incluye el `Username` aunque nunca se envio como string suelto

### Verificacion

```bash
# Iniciar ambos proyectos
dotnet run --project src/EDChat.Api &
dotnet run --project src/EDChat.Web &

# 1. Abrir http://localhost:5003 en el navegador
# 2. Login con "carlos"
# 3. Entrar al chat, seleccionar sala General
# 4. Enviar un mensaje: "Hola desde EF Core!"
# 5. El mensaje debe aparecer con formato: [HH:mm] carlos: Hola desde EF Core!

# Verificar via API que el mensaje incluye el username
curl -s http://localhost:5001/api/rooms/1/messages
# Debe mostrar el mensaje con "username": "carlos" (resuelto via navigation property)

# Ctrl+C en ambas terminales para detener
```

---

## Capitulo 3: Estrategias de carga de relaciones

### Explicacion

EF Core ofrece tres estrategias para cargar datos relacionados:

### Eager Loading (Include)

Carga la relacion en la misma query con un JOIN. Es lo que usamos en `MessageRepository`:

```csharp
// Una sola query SQL con LEFT JOIN
db.Messages.Include(m => m.User).Where(m => m.RoomId == roomId).ToListAsync();
```

```sql
-- SQL generado
SELECT m.*, u.* FROM Messages m LEFT JOIN Users u ON m.UserId = u.Id WHERE m.RoomId = @roomId
```

**Cuando usarlo:** Cuando sabes que siempre necesitas la relacion. Es la opcion mas eficiente para nuestro caso.

### Explicit Loading (LoadAsync)

Carga la relacion en una query separada despues de tener la entidad. Lo usamos en `MessageRepository.CreateAsync`:

```csharp
db.Messages.Add(entity);
await db.SaveChangesAsync();
// Query separada para cargar el User
await db.Entry(entity).Reference(m => m.User).LoadAsync();
```

**Cuando usarlo:** Cuando necesitas la relacion solo en ciertos casos, o despues de un insert.

### Lazy Loading (conceptual)

Carga la relacion automaticamente al acceder a la navigation property. Requiere configuracion adicional (`UseLazyLoadingProxies()`) y puede causar el problema **N+1** — una query por cada entidad en un loop.

```csharp
// Sin Include — cada acceso a message.User genera una query separada!
var messages = await db.Messages.ToListAsync();
foreach (var m in messages)
{
    Console.WriteLine(m.User.Username); // N queries adicionales!
}
```

**En EDChat no usamos lazy loading** — preferimos eager loading con `Include` porque siempre necesitamos el User al mostrar mensajes.

### Diagrama de arquitectura final

```
┌─────────────┐     HTTP/SignalR      ┌─────────────┐
│  EDChat.Web  │ ──────────────────→  │  EDChat.Api  │
│  (Blazor)    │                      │  (Minimal    │
│  :5003       │ ←──────────────────  │   APIs)      │
│              │     JSON/WebSocket   │  :5001       │
└─────────────┘                      └──────┬───────┘
                                            │
                                            │ DI (repos)
                                            │
                                     ┌──────┴───────┐
                                     │  EDChat.Data  │
                                     │  (Entities,   │
                                     │   DbContext,   │
                                     │   Repos)       │
                                     └──────┬───────┘
                                            │
                                            │ EF Core
                                            │
                                     ┌──────┴───────┐
                                     │   SQLite      │
                                     │  edchat.db    │
                                     └──────────────┘
```

---

## Capitulo 4: Demo end-to-end completa

### Explicacion

Verificamos todo el flujo: login → salas → mensajes en tiempo real → persistencia.

### Verificacion

```bash
# Iniciar ambos proyectos
dotnet run --project src/EDChat.Api &
dotnet run --project src/EDChat.Web &

# === Flujo completo ===

# 1. Abrir http://localhost:5003 en el navegador
# 2. Login con "carlos" → se crea/obtiene via POST /api/users
# 3. Las salas se cargan desde SQLite via GET /api/rooms
# 4. Seleccionar "General" → se cargan mensajes historicos via GET /api/rooms/1/messages
# 5. Enviar mensaje "Hola!" → va por SignalR al ChatHub → se guarda en SQLite via MessageRepository
# 6. El mensaje aparece en tiempo real para todos los conectados a la sala

# === Verificar persistencia ===
# 7. Detener AMBOS proyectos (Ctrl+C)
# 8. Reiniciar: dotnet run --project src/EDChat.Api & dotnet run --project src/EDChat.Web &
# 9. Login con "carlos" de nuevo
# 10. Los mensajes anteriores deben aparecer al cargar la sala (persisten en SQLite)

# === Verificar con segundo usuario (opcional) ===
# 11. Abrir otra pestania/navegador en http://localhost:5003
# 12. Login con "maria"
# 13. Enviar mensaje desde maria → debe aparecer en la pestania de carlos en tiempo real

# Ctrl+C en ambas terminales para detener
```

---

## Resumen del modulo

### Que cambiamos

| Antes (Modulo 3) | Despues (Modulo 4) |
|---|---|
| `ChatStore` (singleton in-memory) | `IRepository<T>` + EF Core + SQLite |
| `EDChat.Api/Models/` | `EDChat.Data/Entities/` |
| `Message.Username` (string) | `Message.User.Username` (navigation property) |
| Endpoints sincronos | Endpoints async |
| Lambda exception handler | `GlobalExceptionHandler` (IExceptionHandler) |
| `CreateMessageDto` + POST mensajes | Mensajes solo por SignalR |
| Datos se pierden al reiniciar | Datos persisten en `edchat.db` |

### Proyecto: EDChat.Data (nuevo)

| Componente | Archivos |
|---|---|
| Entidades | `Entities/User.cs`, `Room.cs`, `Message.cs` |
| DbContext | `EDChatDb.cs` (Fluent API, relaciones, seed data) |
| Repositorios | `Repositories/IRepository.cs`, `UserRepository.cs`, `RoomRepository.cs`, `MessageRepository.cs` |

### Proyecto: EDChat.Api (modificado)

| Componente | Cambio |
|---|---|
| `Program.cs` | AddDbContext, repos, GlobalExceptionHandler, EnsureCreated |
| Endpoints | Async, usan repos en vez de ChatStore |
| ChatHub | Usa MessageRepository |
| DtoMappers | Usa `EDChat.Data.Entities`, `message.User.Username` |
| MessageDto.cs | Sin CreateMessageDto |
| Handlers/ | Nuevo: `GlobalExceptionHandler.cs` |

### Proyecto: EDChat.Web (sin cambios)

EDChat.Web **no cambio** en este modulo. Se comunica con la API por HTTP y SignalR. Mientras los DTOs mantengan su shape (`UserDto`, `RoomDto`, `MessageDto`), el frontend no se ve afectado por cambios en la capa de datos.

### Verificacion final

```bash
# Compilar toda la solucion
dotnet build

# Ejecutar
dotnet run --project src/EDChat.Api &
dotnet run --project src/EDChat.Web &

# Verificar en http://localhost:5003
```

La aplicacion EDChat ahora tiene persistencia real con Entity Framework Core 10 y SQLite. Los datos sobreviven reinicios del servidor, las relaciones se resuelven automaticamente, y la arquitectura sigue el patron Repository para desacoplamiento.
