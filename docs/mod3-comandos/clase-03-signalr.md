# Clase 3: SignalR y comunicacion en tiempo real
> Duracion estimada: ~12 min

---

## Capitulo 1: Configurar SignalR Hub

### Explicacion

**SignalR** es la libreria de .NET para comunicacion en tiempo real. Usa WebSockets (con fallback a Server-Sent Events o Long Polling) para enviar datos del servidor al cliente **sin que el cliente haga polling**.

Un **Hub** es una clase del lado del servidor que actua como punto central de comunicacion. Los clientes se conectan al Hub, y el Hub puede enviar mensajes a clientes individuales, a grupos, o a todos los conectados.

Para EDChat, el Hub va en el proyecto **EDChat.Api** (servidor) porque es el backend el que recibe y redistribuye los mensajes. El Hub usa `ChatStore` para guardar los mensajes en memoria.

### Comandos

```bash
cd src/EDChat.Api
mkdir Hubs
```

### Prompt para Claude Code

```text
En la carpeta Hubs/ de EDChat.Api, crea dos archivos:

IChatClient.cs - interfaz que define los metodos que el servidor puede invocar en el cliente:
- Task ReceiveMessage(MessageDto message) — el parametro usa MessageDto del namespace EDChat.Api.DTOs

ChatHub.cs - Hub tipado que hereda de Hub<IChatClient>:
- Recibe ChatStore por constructor (primary constructor)
- Metodo async Task JoinRoom(int roomId): agrega la conexion al grupo usando Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToString())
- Metodo async Task LeaveRoom(int roomId): remueve la conexion del grupo usando Groups.RemoveFromGroupAsync
- Metodo async Task SendMessage(int roomId, int userId, string username, string content): Crea un Message con Content=content, UserId=userId, Username=username, y RoomId=roomId. Usa store.CreateMessage(message). Convierte a MessageDto con message.ToDto() (importar EDChat.Api.Mappers). Envia al grupo con Clients.Group(roomId.ToString()).ReceiveMessage(dto)

Namespaces: EDChat.Api.Hubs. Importar EDChat.Api.Models, EDChat.Api.DTOs, EDChat.Api.Mappers, EDChat.Api.Services, Microsoft.AspNetCore.SignalR.
```

### Codigo esperado

```csharp
// Hubs/IChatClient.cs
using EDChat.Api.DTOs;

namespace EDChat.Api.Hubs;

public interface IChatClient
{
    Task ReceiveMessage(MessageDto message);
}
```

```csharp
// Hubs/ChatHub.cs
using EDChat.Api.DTOs;
using EDChat.Api.Mappers;
using EDChat.Api.Models;
using EDChat.Api.Services;
using Microsoft.AspNetCore.SignalR;

namespace EDChat.Api.Hubs;

public class ChatHub(ChatStore store) : Hub<IChatClient>
{
    public async Task JoinRoom(int roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToString());
    }

    public async Task LeaveRoom(int roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId.ToString());
    }

    public async Task SendMessage(int roomId, int userId, string username, string content)
    {
        var message = new Message
        {
            Content = content,
            UserId = userId,
            Username = username,
            RoomId = roomId
        };

        store.CreateMessage(message);
        var dto = message.ToDto();

        await Clients.Group(roomId.ToString()).ReceiveMessage(dto);
    }
}
```

### Registrar SignalR en el API

### Prompt para Claude Code

```text
Actualiza Program.cs de EDChat.Api para agregar SignalR:
- En servicios: agrega builder.Services.AddSignalR() (despues de AddValidation)
- En el pipeline: agrega app.MapHub<ChatHub>("/chat") (despues de MapMessageEndpoints, antes de app.Run)
- Importa EDChat.Api.Hubs
```

### Codigo esperado

```csharp
// Program.cs de EDChat.Api - lineas nuevas
using EDChat.Api.Hubs;

// ... (servicios existentes)
builder.Services.AddSignalR();

// ... (pipeline existente, despues de los Map endpoints)
app.MapHub<ChatHub>("/chat");

app.Run();
```

### Verificacion

```bash
cd src/EDChat.Api
dotnet build
```

### Puntos clave
- `Hub<IChatClient>` es un Hub tipado — el compilador verifica que los metodos del cliente existan en la interfaz
- `Groups` agrupa conexiones por sala. `Clients.Group("1")` envia solo a los conectados a esa sala
- `Context.ConnectionId` identifica cada conexion unica de un cliente

---

## Capitulo 2: Conectar Blazor con SignalR

### Explicacion

Del lado del cliente (Blazor), usamos `HubConnection` del paquete `Microsoft.AspNetCore.SignalR.Client`. Creamos un servicio `ChatService` que encapsula la conexion y expone eventos C# para cuando llegan mensajes.

### Comandos

```bash
cd src/EDChat.Web
dotnet add package Microsoft.AspNetCore.SignalR.Client
mkdir Services
```

### Prompt para Claude Code

```text
En la carpeta Services/ de EDChat.Web, crea ChatService.cs:

- Clase publica con una propiedad privada HubConnection? _connection
- Propiedad publica de solo lectura HubConnectionState State que retorna _connection?.State ?? HubConnectionState.Disconnected
- Evento Action<MessageModel>? OnMessageReceived — se dispara cuando el servidor envia un mensaje
- Evento Action? OnReconnected — se dispara al reconectarse
- Evento Action<string>? OnClosed — se dispara al perder la conexion

- Metodo async Task ConnectAsync(string hubUrl):
  - Si _connection no es null, retornar inmediatamente (guard para evitar conexiones duplicadas)
  - Crea el HubConnection con HubConnectionBuilderExtensions: new HubConnectionBuilder().WithUrl(hubUrl).WithAutomaticReconnect().Build()
  - Registra handler para "ReceiveMessage" que recibe un MessageModel y dispara OnMessageReceived
  - Registra evento Reconnected que dispara OnReconnected
  - Registra evento Closed que dispara OnClosed con el mensaje de la excepcion o string vacio
  - Llama await _connection.StartAsync()

- Metodo async Task JoinRoomAsync(int roomId): invoca "JoinRoom" en el hub
- Metodo async Task LeaveRoomAsync(int roomId): invoca "LeaveRoom" en el hub
- Metodo async Task SendMessageAsync(int roomId, int userId, string username, string content): invoca "SendMessage" en el hub con los cuatro parametros

- Todos los metodos que usan _connection deben verificar que no sea null antes de invocar
- Implementar IAsyncDisposable: en DisposeAsync, si _connection no es null, llamar await _connection.DisposeAsync() y asignar null. Esto libera la conexion WebSocket cuando el servicio scoped se destruye (al cerrar el circuito)

Namespace: EDChat.Web.Services. Importar Microsoft.AspNetCore.SignalR.Client y EDChat.Web.Models.
```

### Codigo esperado

```csharp
// Services/ChatService.cs
using EDChat.Web.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace EDChat.Web.Services;

public class ChatService : IAsyncDisposable
{
    private HubConnection? _connection;

    public HubConnectionState State =>
        _connection?.State ?? HubConnectionState.Disconnected;

    public event Action<MessageModel>? OnMessageReceived;
    public event Action? OnReconnected;
    public event Action<string>? OnClosed;

    public async Task ConnectAsync(string hubUrl)
    {
        if (_connection is not null) return;

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _connection.On<MessageModel>("ReceiveMessage", message =>
        {
            OnMessageReceived?.Invoke(message);
        });

        _connection.Reconnected += connectionId =>
        {
            OnReconnected?.Invoke();
            return Task.CompletedTask;
        };

        _connection.Closed += exception =>
        {
            OnClosed?.Invoke(exception?.Message ?? string.Empty);
            return Task.CompletedTask;
        };

        await _connection.StartAsync();
    }

    public async Task JoinRoomAsync(int roomId)
    {
        if (_connection is not null)
            await _connection.InvokeAsync("JoinRoom", roomId);
    }

    public async Task LeaveRoomAsync(int roomId)
    {
        if (_connection is not null)
            await _connection.InvokeAsync("LeaveRoom", roomId);
    }

    public async Task SendMessageAsync(int roomId, int userId, string username, string content)
    {
        if (_connection is not null)
            await _connection.InvokeAsync("SendMessage", roomId, userId, username, content);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}
```

### Registrar ChatService en DI

### Prompt para Claude Code

```text
Actualiza Program.cs de EDChat.Web para registrar ChatService como Scoped (una instancia por circuito/sesion de usuario):
- Agrega using EDChat.Web.Services
- Agrega builder.Services.AddScoped<ChatService>() despues de AddMudServices()
```

### Codigo esperado

```csharp
// Program.cs de EDChat.Web - actualizado
using EDChat.Web.Components;
using EDChat.Web.Services;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddScoped<ChatService>();

var app = builder.Build();

// ... (resto del pipeline sin cambios)
```

### Actualizar _Imports.razor

### Prompt para Claude Code

```text
Agrega @using EDChat.Web.Services al final de Components/_Imports.razor en EDChat.Web (despues del @using EDChat.Web.Models que se agrego en la clase anterior). Esto evita tener que agregar @using en cada componente que use los servicios.
```

### Codigo esperado

```razor
@* Agregar al final de Components/_Imports.razor *@
@using EDChat.Web.Services
```

### Puntos clave
- `WithAutomaticReconnect()` intenta reconectar automaticamente si se pierde la conexion
- `_connection.On<T>("Metodo", handler)` registra un handler para cuando el servidor invoca ese metodo
- `Scoped` en Blazor Server significa una instancia **por circuito** (por sesion de usuario)

---

## Capitulo 3: Enviar y recibir mensajes en tiempo real

### Explicacion

Ahora conectamos `ChatService` con la pagina `Chat.razor`. El flujo es:

1. Al entrar a `/chat/{Username}`, el componente se conecta al Hub via `ChatService`
2. Se une a la sala "General" (Id=1) por defecto
3. Cuando el usuario escribe un mensaje, se envia al Hub
4. El Hub guarda el mensaje y lo redistribuye a todos los conectados a esa sala
5. Cada cliente recibe el mensaje via `OnMessageReceived` y actualiza la UI

### Prompt para Claude Code

```text
Reemplaza el contenido de Components/Pages/Chat.razor en EDChat.Web. La pagina implementa IAsyncDisposable y tiene ruta "/chat/{Username}":

Inyectar: NavigationManager, ChatService, IConfiguration, ISnackbar

Estado:
- List<MessageModel> messages = []
- string? messageText
- int currentRoomId = 1 (sala General por defecto)
- int currentUserId = 0 (se simulara por ahora)
- bool isConnected = false

Layout con MudBlazor:
- MudAppBar con Elevation 2: icono de chat (Icons.Material.Filled.Chat), titulo "EDChat", Spacer, MudChip que muestre el estado de conexion (verde "Conectado" o rojo "Desconectado" segun isConnected), y MudButton para cerrar sesion
- MudMainContent con Class "pa-4" Style "margin-top: 64px":
  - MudPaper con Class "pa-4" Style "height: calc(100vh - 140px)" con display flex y flex-direction column:
    - MudText Typo.h6 Class "mb-2": "Sala: General"
    - Div con style "flex: 1; overflow-y: auto" para la lista de mensajes:
      - Foreach message in messages: MudText con el formato "[HH:mm] username: content". Si message.UserId == currentUserId, agregar Style "font-weight: bold"
    - Div con display flex y gap para el input:
      - MudTextField @bind-Value messageText, Placeholder "Escribe un mensaje...", Variant Outlined, con OnKeyDown que llame SendMessage si es Enter
      - MudIconButton con Icons.Material.Filled.Send, Color Primary, que llame SendMessage

En @code:
- OnInitializedAsync: obtener la URL del hub desde IConfiguration["ApiUrl"] + "/chat". Registrar ChatService.OnMessageReceived para agregar el mensaje a la lista y llamar InvokeAsync(StateHasChanged). Registrar OnReconnected y OnClosed para actualizar isConnected y mostrar snackbar. Llamar ChatService.ConnectAsync(hubUrl), actualizar isConnected=true, y llamar ChatService.JoinRoomAsync(currentRoomId). Envolver en try-catch.
- SendMessage: si messageText no esta vacio, llamar ChatService.SendMessageAsync(currentRoomId, currentUserId, Username, messageText) y limpiar messageText.
- Los handlers async void (HandleMessageReceived, HandleReconnected, HandleClosed) deben envolver su contenido en try-catch porque las excepciones en async void crashean el proceso.
- DisposeAsync: implementar IAsyncDisposable (no IDisposable). Desuscribir los eventos y llamar ChatService.LeaveRoomAsync(currentRoomId) envuelto en try-catch (la conexion puede estar cerrada).
```

### Codigo esperado

```razor
<!-- Components/Pages/Chat.razor -->
@page "/chat/{Username}"
@inject NavigationManager NavigationManager
@inject ChatService ChatService
@inject IConfiguration Configuration
@inject ISnackbar Snackbar
@implements IAsyncDisposable
@using EDChat.Web.Services
@using EDChat.Web.Models

<MudLayout>
    <MudAppBar Elevation="2">
        <MudIcon Icon="@Icons.Material.Filled.Chat" Class="mr-2" />
        <MudText Typo="Typo.h6">EDChat</MudText>
        <MudSpacer />
        @if (isConnected)
        {
            <MudChip T="string" Color="Color.Success" Size="Size.Small">Conectado</MudChip>
        }
        else
        {
            <MudChip T="string" Color="Color.Error" Size="Size.Small">Desconectado</MudChip>
        }
        <MudButton Color="Color.Inherit" OnClick="Logout">Cerrar sesion</MudButton>
    </MudAppBar>

    <MudMainContent Class="pa-4" Style="margin-top: 64px">
        <MudPaper Class="pa-4" Style="height: calc(100vh - 140px); display: flex; flex-direction: column">
            <MudText Typo="Typo.h6" Class="mb-2">Sala: General</MudText>

            <div style="flex: 1; overflow-y: auto" id="message-list">
                @foreach (var message in messages)
                {
                    <MudText Style="@(message.UserId == currentUserId ? "font-weight: bold" : "")">
                        [@message.SentAt.ToString("HH:mm")] @message.Username: @message.Content
                    </MudText>
                }
            </div>

            <div style="display: flex; gap: 8px; margin-top: 8px">
                <MudTextField @bind-Value="messageText"
                              Placeholder="Escribe un mensaje..."
                              Variant="Variant.Outlined"
                              OnKeyDown="HandleKeyDown"
                              Style="flex: 1" />
                <MudIconButton Icon="@Icons.Material.Filled.Send"
                               Color="Color.Primary"
                               OnClick="SendMessage" />
            </div>
        </MudPaper>
    </MudMainContent>
</MudLayout>

@code {
    [Parameter]
    public string Username { get; set; } = string.Empty;

    private List<MessageModel> messages = [];
    private string? messageText;
    private int currentRoomId = 1;
    private int currentUserId = 0;
    private bool isConnected;

    protected override async Task OnInitializedAsync()
    {
        var apiUrl = Configuration["ApiUrl"] ?? "http://localhost:5001";
        var hubUrl = $"{apiUrl}/chat";

        ChatService.OnMessageReceived += HandleMessageReceived;
        ChatService.OnReconnected += HandleReconnected;
        ChatService.OnClosed += HandleClosed;

        try
        {
            await ChatService.ConnectAsync(hubUrl);
            isConnected = true;
            await ChatService.JoinRoomAsync(currentRoomId);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error al conectar: {ex.Message}", Severity.Error);
        }
    }

    private async void HandleMessageReceived(MessageModel message)
    {
        try
        {
            messages.Add(message);
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception)
        {
            // El componente puede estar disposed; ignorar
        }
    }

    private async void HandleReconnected()
    {
        try
        {
            isConnected = true;
            Snackbar.Add("Reconectado al servidor", Severity.Success);
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception)
        {
            // El componente puede estar disposed; ignorar
        }
    }

    private async void HandleClosed(string error)
    {
        try
        {
            isConnected = false;
            Snackbar.Add("Se perdio la conexion con el servidor", Severity.Warning);
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception)
        {
            // El componente puede estar disposed; ignorar
        }
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await SendMessage();
        }
    }

    private async Task SendMessage()
    {
        if (!string.IsNullOrWhiteSpace(messageText))
        {
            await ChatService.SendMessageAsync(currentRoomId, currentUserId, Username, messageText);
            messageText = string.Empty;
        }
    }

    private void Logout()
    {
        NavigationManager.NavigateTo("/");
    }

    public async ValueTask DisposeAsync()
    {
        ChatService.OnMessageReceived -= HandleMessageReceived;
        ChatService.OnReconnected -= HandleReconnected;
        ChatService.OnClosed -= HandleClosed;

        try
        {
            await ChatService.LeaveRoomAsync(currentRoomId);
        }
        catch (Exception)
        {
            // La conexion puede estar cerrada; ignorar
        }
    }
}
```

### Verificacion

```bash
# Terminal 1: iniciar el API
cd src/EDChat.Api
dotnet run &

# Terminal 2: iniciar la Web
cd src/EDChat.Web
dotnet run &

# 1. Abrir http://localhost:5003 en el navegador
# 2. Escribir un nombre y entrar al chat
# 3. El chip deberia mostrar "Conectado" (verde)
# 4. Escribir un mensaje y presionar Enter o click en enviar
# 5. El mensaje deberia aparecer en la lista (con userId 0 porque aun no hay login real)

# Ctrl+C en ambas terminales para detener
```

### Puntos clave
- `InvokeAsync(StateHasChanged)` es necesario porque los eventos de SignalR llegan desde un hilo diferente al de renderizado de Blazor
- El `currentUserId = 0` es temporal — en la Clase 4 se obtendra del API al hacer login
- Los mensajes solo aparecen cuando llegan via SignalR, no se cargan historicos todavia

---

## Capitulo 4: Manejo de conexion y reconexion

### Explicacion

`WithAutomaticReconnect()` intenta reconectar automaticamente con intervalos crecientes: 0s, 2s, 10s, 30s. Si falla despues de 4 intentos, se cierra permanentemente.

Para personalizar los intervalos o la cantidad de reintentos:

```csharp
// Ejemplo: reintentos cada 1s, 5s, 10s, 30s, 60s
.WithAutomaticReconnect(new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5),
    TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60) })
```

En `Chat.razor` ya manejamos los tres estados:
- **Conectado**: chip verde, chat funcional
- **Reconectando**: automatico via `WithAutomaticReconnect()`
- **Desconectado**: chip rojo, snackbar de advertencia

El evento `Reconnected` se dispara cuando la reconexion es exitosa. Despues de reconectarse, hay que **volver a unirse a los grupos** porque SignalR no persiste la membresia de grupos:

### Prompt para Claude Code

```text
Actualiza el metodo HandleReconnected en Chat.razor para que despues de reconectarse se una nuevamente a la sala actual con ChatService.JoinRoomAsync(currentRoomId). Hazlo async void y agrega el await.
```

### Codigo esperado

```csharp
// Actualizar en Chat.razor
private async void HandleReconnected()
{
    try
    {
        isConnected = true;
        await ChatService.JoinRoomAsync(currentRoomId);
        Snackbar.Add("Reconectado al servidor", Severity.Success);
        await InvokeAsync(StateHasChanged);
    }
    catch (Exception)
    {
        // El componente puede estar disposed; ignorar
    }
}
```

---

## Capitulo 5: ReconnectModal y circuit resilience (.NET 10)

### Explicacion

Blazor Interactive Server usa un **circuito** SignalR para mantener el estado de la UI. Si la conexion se pierde (wifi inestable, sleep del laptop), Blazor muestra automaticamente un modal de reconexion.

En .NET 10, el comportamiento de reconexion del circuito se mejoro significativamente:

1. **Reconnect modal automatico**: Blazor muestra un overlay cuando detecta desconexion del circuito
2. **Circuit resilience mejorado**: .NET 10 mantiene el estado del circuito en el servidor por mas tiempo, permitiendo reconexiones exitosas incluso despues de pausas largas
3. **Blazor.web.js** maneja la reconexion automaticamente — no necesitas codigo adicional

El script `_framework/blazor.web.js` (que ya incluimos en `App.razor`) se encarga de:
- Detectar desconexion del circuito
- Mostrar un modal de reconexion al usuario
- Intentar reconectar automaticamente
- Recargar la pagina si la reconexion falla permanentemente

> **Nota:** Este es el circuito de **Blazor** (la UI interactiva), diferente de la conexion SignalR de nuestro **ChatService** (los mensajes del chat). Blazor usa su propio canal SignalR internamente para el render mode Interactive Server.

### Verificacion

```bash
# Iniciar ambos proyectos
cd src/EDChat.Api && dotnet run &
cd src/EDChat.Web && dotnet run &

# 1. Abrir http://localhost:5003
# 2. Hacer login y entrar al chat
# 3. Detener el API (Ctrl+C en terminal 1)
# 4. Intentar enviar un mensaje — deberia mostrar error
# 5. Reiniciar el API: cd src/EDChat.Api && dotnet run &
# 6. Esperar unos segundos — el ChatService deberia reconectarse automaticamente
# 7. Verificar que el chip vuelve a verde

# Ctrl+C en ambas terminales para detener
```

---

## Estado del proyecto al finalizar

### Archivos creados/modificados
```
EDChat.Api/
├── Hubs/
│   ├── IChatClient.cs
│   └── ChatHub.cs
└── Program.cs

EDChat.Web/
├── Components/
│   └── Pages/
│       └── Chat.razor
├── Services/
│   └── ChatService.cs
├── Program.cs
└── EDChat.Web.csproj
```

### Verificacion

```bash
cd src/EDChat.Api && dotnet build
cd src/EDChat.Web && dotnet build
```

El chat ya envia y recibe mensajes en tiempo real via SignalR. En la Clase 4 agregaremos consumo del API REST para login, salas y mensajes historicos.
