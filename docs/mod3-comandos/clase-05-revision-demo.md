# Clase 5: Revision y demo del flujo completo
> Duracion estimada: ~12 min

---

## Capitulo 1: Web App completamente funcional

### Explicacion

Verificamos que toda la aplicacion web esta funcionando correctamente. El flujo completo incluye: login de usuario, visualizacion de salas, seleccion de salas y envio de mensajes. Todos los componentes que construimos en las clases anteriores trabajan juntos.

### Comandos

```bash
# Iniciar ambos proyectos
cd src/EDChat.Api
dotnet run &
sleep 2

cd src/EDChat.Web
dotnet run &
sleep 2

echo "API: http://localhost:5001"
echo "Web: http://localhost:5003"
echo "Scalar: http://localhost:5001/scalar/v1"
```

---

## Capitulo 2: SignalR funcionando en tiempo real

### Explicacion

Para probar el tiempo real, necesitamos dos sesiones de navegador (o una ventana normal y una de incognito) conectadas a la misma sala.

### Pasos de la demo

1. **Abrir el navegador:** Ir a `http://localhost:5003`

2. **Login usuario 1:** Escribir "carlos" y click en "Entrar al chat"
   - Verificar: chip verde "Conectado"
   - Verificar: lista de salas en el drawer ("General", "Tecnologia")

3. **Abrir segunda ventana** (incognito o diferente navegador): Ir a `http://localhost:5003`

4. **Login usuario 2:** Escribir "maria" y click en "Entrar al chat"

5. **Enviar mensaje desde carlos:** Escribir "Hola a todos!" y Enter
   - Verificar: el mensaje aparece en **ambas** ventanas instantaneamente
   - Verificar: en la ventana de carlos, el mensaje aparece en **bold** (es su propio mensaje)
   - Verificar: en la ventana de maria, aparece normal

6. **Responder desde maria:** Escribir "Hola Carlos!" y Enter
   - Verificar: aparece en ambas ventanas

7. **Cambiar de sala:** En la ventana de carlos, click en "Tecnologia"
   - Verificar: la lista de mensajes se limpia (sala diferente)

8. **Enviar mensaje en sala Tecnologia desde carlos:** "Alguien sabe de .NET 10?"
   - Verificar: el mensaje aparece en la ventana de carlos
   - Verificar: el mensaje **NO** aparece en la ventana de maria (esta en otra sala)

---

## Capitulo 3: Conexion con API verificada

### Verificacion con curl

Mientras la demo esta corriendo, podemos verificar que los datos se guardaron en el API:

```bash
# Verificar usuarios creados
curl -s http://localhost:5001/api/users
```

Respuesta esperada:
```json
[
  { "id": 1, "username": "carlos", "createdAt": "2026-02-11T..." },
  { "id": 2, "username": "maria", "createdAt": "2026-02-11T..." }
]
```

```bash
# Verificar salas
curl -s http://localhost:5001/api/rooms
```

Respuesta esperada:
```json
[
  { "id": 1, "name": "General", "description": "Sala de chat general", "createdAt": "2025-01-01T00:00:00Z" },
  { "id": 2, "name": "Tecnología", "description": "Discusiones sobre tecnología", "createdAt": "2025-01-01T00:00:00Z" }
]
```

```bash
# Verificar mensajes de la sala General
curl -s http://localhost:5001/api/rooms/1/messages
```

Respuesta esperada:
```json
[
  { "id": 1, "content": "Hola a todos!", "sentAt": "2026-02-11T...", "userId": 1, "username": "carlos", "roomId": 1 },
  { "id": 2, "content": "Hola Carlos!", "sentAt": "2026-02-11T...", "userId": 2, "username": "maria", "roomId": 1 }
]
```

```bash
# Verificar mensajes de la sala Tecnologia
curl -s http://localhost:5001/api/rooms/2/messages
```

Respuesta esperada:
```json
[
  { "id": 3, "content": "Alguien sabe de .NET 10?", "sentAt": "2026-02-11T...", "userId": 1, "username": "carlos", "roomId": 2 }
]
```

```bash
# Detener los servidores
kill %1 %2 2>/dev/null
```

---

## Resumen de la arquitectura

```
                    ┌─────────────────────┐
                    │   Navegador (UI)    │
                    │   Blazor Server     │
                    │   MudBlazor         │
                    └────────┬────────────┘
                             │
              ┌──────────────┼──────────────┐
              │              │              │
         HTTP REST      SignalR          Blazor
         (ApiClient)   (ChatService)    Circuit
              │              │              │
              ▼              ▼              ▼
    ┌─────────────────────────────────────────┐
    │           EDChat.Api                     │
    │   ┌──────────┐  ┌──────────┐            │
    │   │ Endpoints │  │ ChatHub  │            │
    │   └────┬─────┘  └────┬─────┘            │
    │        │              │                  │
    │        ▼              ▼                  │
    │   ┌──────────────────────┐              │
    │   │     ChatStore        │              │
    │   │   (in-memory)        │              │
    │   └──────────────────────┘              │
    └─────────────────────────────────────────┘
```

**Tres canales de comunicacion:**
1. **HTTP REST** (`ApiClient`): Login, listar salas, cargar mensajes historicos, crear salas
2. **SignalR** (`ChatService`): Enviar/recibir mensajes en tiempo real, unirse/salir de salas
3. **Blazor Circuit**: La UI interactiva (render mode Interactive Server) — esto lo maneja Blazor automaticamente

---

## Resumen de archivos del proyecto

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
├── Hubs/
│   ├── IChatClient.cs
│   └── ChatHub.cs
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
├── Program.cs
└── EDChat.Api.csproj

EDChat.Web/
├── Components/
│   ├── Layout/
│   │   ├── MainLayout.razor
│   │   └── ReconnectModal.razor
│   ├── Pages/
│   │   ├── Home.razor
│   │   ├── Chat.razor
│   │   └── NotFound.razor
│   ├── App.razor
│   ├── Routes.razor
│   └── _Imports.razor
├── Models/
│   ├── UserModel.cs
│   ├── RoomModel.cs
│   └── MessageModel.cs
├── Services/
│   ├── ApiClient.cs
│   └── ChatService.cs
├── Properties/
│   └── launchSettings.json
├── appsettings.json
├── Program.cs
└── EDChat.Web.csproj
```
