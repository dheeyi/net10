# Clase 4: Consumir APIs REST desde Blazor
> Duracion estimada: ~12 min

---

## Capitulo 1: Configurar HttpClient en Blazor

### Explicacion

`HttpClient` es la clase de .NET para hacer requests HTTP. En Blazor Server, usamos `IHttpClientFactory` para crear instancias configuradas — esto maneja el pool de conexiones y evita socket exhaustion.

Registramos un **named HttpClient** con la URL base del API (desde `appsettings.json`). Asi todos los requests van a `http://localhost:5001` sin repetir la URL.

### Prompt para Claude Code

```text
Actualiza Program.cs de EDChat.Web para configurar HttpClient:

- Lee la URL del API desde la configuracion: builder.Configuration["ApiUrl"] ?? "http://localhost:5001"
- Registra un named HttpClient "Api" con builder.Services.AddHttpClient("Api", client => { client.BaseAddress = new Uri(apiUrl); })
- Esto debe ir despues de AddScoped<ChatService>()
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

var apiUrl = builder.Configuration["ApiUrl"] ?? "http://localhost:5001";
builder.Services.AddHttpClient("Api", client =>
{
    client.BaseAddress = new Uri(apiUrl);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.MapStaticAssets();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
```

### Puntos clave
- `AddHttpClient("Api", ...)` registra un HttpClient con nombre "Api" y URL base configurada
- `IHttpClientFactory` gestiona la vida de las conexiones HTTP automaticamente
- La URL base viene de `appsettings.json` — facil de cambiar sin modificar codigo

---

## Capitulo 2: Inyeccion de dependencias para servicios

### Explicacion

Creamos `ApiClient` — un servicio que encapsula todas las llamadas HTTP al API. En vez de inyectar `HttpClient` directamente en los componentes, centralizamos la logica en un servicio. Esto permite:
- Reusar la logica de llamadas en multiples componentes
- Mantener el estado del usuario logueado (`CurrentUser`)
- Manejar errores en un solo lugar

### Prompt para Claude Code

```text
En la carpeta Services/ de EDChat.Web, crea ApiClient.cs:

- Recibe IHttpClientFactory por constructor (primary constructor)
- Propiedad privada HttpClient Http que se inicializa con factory.CreateClient("Api") (lazy con field backing o en constructor)
- Propiedad publica UserModel? CurrentUser { get; private set; }
- Propiedad publica string BaseAddress que retorna Http.BaseAddress?.ToString() ?? ""

- Metodo async Task<UserModel?> LoginAsync(string username):
  - Crea un objeto anonimo new { username }
  - Hace POST a "/api/users" con PostAsJsonAsync
  - Si response.IsSuccessStatusCode, lee el body como UserModel con response.Content.ReadFromJsonAsync<UserModel>()
  - Asigna el resultado a CurrentUser
  - Retorna CurrentUser
  - Si falla, retorna null

- Metodo async Task<List<RoomModel>> GetRoomsAsync():
  - Hace GET a "/api/rooms" con GetFromJsonAsync<List<RoomModel>>()
  - Retorna la lista o una lista vacia si es null

- Metodo async Task<RoomModel?> CreateRoomAsync(string name, string description):
  - Hace POST a "/api/rooms" con PostAsJsonAsync enviando new { name, description }
  - Si response.IsSuccessStatusCode, lee como RoomModel
  - Retorna el room o null

- Metodo async Task<List<MessageModel>> GetMessagesAsync(int roomId):
  - Hace GET a $"/api/rooms/{roomId}/messages" con GetFromJsonAsync
  - Retorna la lista o una lista vacia si es null

Namespace: EDChat.Web.Services. Importar EDChat.Web.Models y System.Net.Http.Json.
```

### Codigo esperado

```csharp
// Services/ApiClient.cs
using System.Net.Http.Json;
using EDChat.Web.Models;

namespace EDChat.Web.Services;

public class ApiClient(IHttpClientFactory factory)
{
    private readonly HttpClient _http = factory.CreateClient("Api");

    public UserModel? CurrentUser { get; private set; }
    public string BaseAddress => _http.BaseAddress?.ToString() ?? "";

    public async Task<UserModel?> LoginAsync(string username)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/users", new { username });

            if (response.IsSuccessStatusCode)
            {
                CurrentUser = await response.Content.ReadFromJsonAsync<UserModel>();
                return CurrentUser;
            }
        }
        catch (Exception)
        {
            // Se maneja en el componente
        }

        return null;
    }

    public async Task<List<RoomModel>> GetRoomsAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<RoomModel>>("/api/rooms") ?? [];
        }
        catch (Exception)
        {
            return [];
        }
    }

    public async Task<RoomModel?> CreateRoomAsync(string name, string description)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/rooms", new { name, description });

            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<RoomModel>();
        }
        catch (Exception)
        {
            // Se maneja en el componente
        }

        return null;
    }

    public async Task<List<MessageModel>> GetMessagesAsync(int roomId)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<MessageModel>>($"/api/rooms/{roomId}/messages") ?? [];
        }
        catch (Exception)
        {
            return [];
        }
    }
}
```

### Registrar ApiClient en DI

### Prompt para Claude Code

```text
Actualiza Program.cs de EDChat.Web para registrar ApiClient como Scoped:
- Agrega builder.Services.AddScoped<ApiClient>() despues de AddHttpClient
```

### Codigo esperado

```csharp
// Program.cs - agregar despues de AddHttpClient
builder.Services.AddScoped<ApiClient>();
```

---

## Capitulo 3: GET - obtener datos de la API

### Explicacion

Ahora conectamos `Chat.razor` con el API real. Al entrar al chat:
1. Cargamos las salas disponibles con `ApiClient.GetRoomsAsync()`
2. Seleccionamos la primera sala
3. Cargamos los mensajes historicos de esa sala con `ApiClient.GetMessagesAsync(roomId)`

### Prompt para Claude Code

```text
Reemplaza completamente Components/Pages/Chat.razor en EDChat.Web. Cambios principales:

1. Inyectar ApiClient ademas de los servicios existentes (@inject ApiClient ApiClient, agregar @using EDChat.Web.Services)

2. Agregar estado:
   - List<RoomModel> rooms = []
   - RoomModel? selectedRoom

3. Actualizar OnInitializedAsync:
   - Antes de conectar SignalR, cargar las salas: rooms = await ApiClient.GetRoomsAsync()
   - Si hay salas, selectedRoom = rooms.First() y currentRoomId = selectedRoom.Id
   - Despues de JoinRoomAsync, cargar mensajes historicos: messages = await ApiClient.GetMessagesAsync(currentRoomId)
   - Obtener currentUserId de ApiClient.CurrentUser?.Id ?? 0

4. Agregar metodo async Task SelectRoom(RoomModel room):
   - Si hay sala seleccionada, llamar ChatService.LeaveRoomAsync del room anterior
   - Actualizar selectedRoom = room y currentRoomId = room.Id
   - Llamar ChatService.JoinRoomAsync(currentRoomId)
   - Cargar mensajes: messages = await ApiClient.GetMessagesAsync(currentRoomId)

5. Actualizar el layout para mostrar salas en un drawer lateral:
   - Agregar bool drawerOpen = true al estado
   - MudIconButton con icono Menu en el AppBar para toggle del drawer (drawerOpen = !drawerOpen)
   - MudDrawer con @bind-Open="drawerOpen" Variant Persistent, con lista de salas
   - Cada sala es un MudNavLink que llama SelectRoom
   - El titulo de la sala seleccionada cambia dinamicamente
   - Mostrar "Sin salas disponibles" si la lista esta vacia
```

### Codigo esperado

```razor
<!-- Components/Pages/Chat.razor - version completa -->
@page "/chat/{Username}"
@inject NavigationManager NavigationManager
@inject ChatService ChatService
@inject ApiClient ApiClient
@inject IConfiguration Configuration
@inject ISnackbar Snackbar
@implements IAsyncDisposable
@using EDChat.Web.Services
@using EDChat.Web.Models

<MudLayout>
    <MudAppBar Elevation="2">
        <MudIconButton Icon="@Icons.Material.Filled.Menu" Color="Color.Inherit" OnClick="() => drawerOpen = !drawerOpen" />
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

    <MudDrawer @bind-Open="drawerOpen" Variant="DrawerVariant.Persistent" Elevation="1">
        <MudDrawerHeader>
            <MudText Typo="Typo.h6">Salas</MudText>
        </MudDrawerHeader>
        <MudNavMenu>
            @foreach (var room in rooms)
            {
                <MudNavLink OnClick="() => SelectRoom(room)"
                            Style="@(selectedRoom?.Id == room.Id ? "background-color: var(--mud-palette-action-default-hover)" : "")">
                    @room.Name
                </MudNavLink>
            }
            @if (!rooms.Any())
            {
                <MudText Class="pa-4" Typo="Typo.body2">Sin salas disponibles</MudText>
            }
        </MudNavMenu>
    </MudDrawer>

    <MudMainContent Class="pa-4" Style="margin-top: 64px">
        <MudPaper Class="pa-4" Style="height: calc(100vh - 140px); display: flex; flex-direction: column">
            <MudText Typo="Typo.h6" Class="mb-2">
                @(selectedRoom?.Name ?? "Selecciona una sala")
            </MudText>

            <div style="flex: 1; overflow-y: auto">
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

    private List<RoomModel> rooms = [];
    private List<MessageModel> messages = [];
    private RoomModel? selectedRoom;
    private string? messageText;
    private int currentRoomId = 1;
    private int currentUserId;
    private bool drawerOpen = true;
    private bool isConnected;

    protected override async Task OnInitializedAsync()
    {
        currentUserId = ApiClient.CurrentUser?.Id ?? 0;

        // Cargar salas del API
        rooms = await ApiClient.GetRoomsAsync();
        if (rooms.Any())
        {
            selectedRoom = rooms.First();
            currentRoomId = selectedRoom.Id;
        }

        // Conectar SignalR
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

            // Cargar mensajes historicos
            messages = await ApiClient.GetMessagesAsync(currentRoomId);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error al conectar: {ex.Message}", Severity.Error);
        }
    }

    private async Task SelectRoom(RoomModel room)
    {
        if (selectedRoom is not null)
            await ChatService.LeaveRoomAsync(selectedRoom.Id);

        selectedRoom = room;
        currentRoomId = room.Id;

        await ChatService.JoinRoomAsync(currentRoomId);
        messages = await ApiClient.GetMessagesAsync(currentRoomId);
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
            await ChatService.JoinRoomAsync(currentRoomId);
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
            await SendMessage();
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

        if (selectedRoom is not null)
        {
            try
            {
                await ChatService.LeaveRoomAsync(selectedRoom.Id);
            }
            catch (Exception)
            {
                // La conexion puede estar cerrada; ignorar
            }
        }
    }
}
```

---

## Capitulo 4: POST - enviar datos a la API

### Explicacion

Ahora conectamos el login real. `Home.razor` usara `ApiClient.LoginAsync()` para crear o recuperar el usuario via POST al API. Esto reemplaza la navegacion directa por una llamada HTTP real.

### Prompt para Claude Code

```text
Actualiza Components/Pages/Home.razor en EDChat.Web para usar ApiClient:

- Inyectar ApiClient (@inject ApiClient ApiClient, agregar @using EDChat.Web.Services)
- Inyectar ISnackbar para notificaciones
- Agregar campo bool isLoading = false
- Cambiar el metodo Login a async Task:
  - Activar isLoading = true
  - Llamar await ApiClient.LoginAsync(username)
  - Si retorna un usuario (no null), navegar a "/chat"
  - Si retorna null, mostrar Snackbar.Add("Error al conectar con el servidor", Severity.Error)
  - En finally, isLoading = false
- El boton debe mostrar un MudProgressCircular cuando isLoading es true, y deshabilitarse
- Actualizar la ruta de Chat.razor: cambiar de /chat/{Username} a /chat (sin parametro, ya que el username viene de ApiClient.CurrentUser)
```

### Codigo esperado

```razor
<!-- Components/Pages/Home.razor - con API real -->
@page "/"
@inject NavigationManager NavigationManager
@inject ApiClient ApiClient
@inject ISnackbar Snackbar
@using EDChat.Web.Services

<MudContainer MaxWidth="MaxWidth.Small" Class="d-flex align-center" Style="min-height: 100vh">
    <MudPaper Elevation="3" Class="pa-8" Style="width: 100%">
        <MudText Typo="Typo.h3" Align="Align.Center">EDChat</MudText>
        <MudText Typo="Typo.subtitle1" Align="Align.Center" Class="mb-6">
            Ingresa tu nombre para comenzar
        </MudText>

        <MudTextField @bind-Value="username"
                      Label="Nombre de usuario"
                      Variant="Variant.Outlined"
                      Class="mb-4"
                      Disabled="isLoading" />

        <MudButton Variant="Variant.Filled"
                   Color="Color.Primary"
                   FullWidth="true"
                   Disabled="@(string.IsNullOrWhiteSpace(username) || isLoading)"
                   OnClick="Login">
            @if (isLoading)
            {
                <MudProgressCircular Size="Size.Small" Indeterminate="true" Class="mr-2" />
                <span>Conectando...</span>
            }
            else
            {
                <span>Entrar al chat</span>
            }
        </MudButton>
    </MudPaper>
</MudContainer>

@code {
    private string? username;
    private bool isLoading;

    private async Task Login()
    {
        if (string.IsNullOrWhiteSpace(username)) return;

        isLoading = true;

        try
        {
            var user = await ApiClient.LoginAsync(username);

            if (user is not null)
            {
                NavigationManager.NavigateTo("/chat");
            }
            else
            {
                Snackbar.Add("Error al conectar con el servidor", Severity.Error);
            }
        }
        catch (Exception)
        {
            Snackbar.Add("Error al conectar con el servidor", Severity.Error);
        }
        finally
        {
            isLoading = false;
        }
    }
}
```

### Actualizar Chat.razor - ruta sin parametro

### Prompt para Claude Code

```text
Actualiza Chat.razor: cambia @page "/chat/{Username}" por @page "/chat". Elimina el [Parameter] public string Username. En su lugar, obtiene el username de ApiClient.CurrentUser?.Username ?? "". Si ApiClient.CurrentUser es null, redirigir a "/" en OnInitializedAsync (el usuario no hizo login). Actualizar currentUserId para usar ApiClient.CurrentUser.Id.
```

### Codigo esperado

```csharp
// Cambios en Chat.razor @code
// Cambiar la directiva @page:
// @page "/chat"  (en vez de "/chat/{Username}")

// Eliminar [Parameter] y agregar:
private string Username => ApiClient.CurrentUser?.Username ?? "";

// En OnInitializedAsync, al inicio:
if (ApiClient.CurrentUser is null)
{
    NavigationManager.NavigateTo("/");
    return;
}
currentUserId = ApiClient.CurrentUser.Id;
```

---

## Capitulo 5: Manejo de errores

### Explicacion

En una aplicacion real, las llamadas HTTP pueden fallar por muchas razones: el servidor esta caido, timeout, datos invalidos, etc. Nuestro `ApiClient` ya captura excepciones y retorna `null` o listas vacias. Los componentes muestran feedback al usuario con `ISnackbar`.

Patron de manejo de errores en EDChat:

| Capa | Responsabilidad | Ejemplo |
|------|----------------|---------|
| `ApiClient` | Captura excepciones HTTP, retorna null/empty | `try/catch` en cada metodo |
| `ChatService` | Eventos de desconexion/reconexion | `OnClosed`, `OnReconnected` |
| Componentes `.razor` | Muestra feedback al usuario | `Snackbar.Add(...)` |
| `Program.cs` (API) | Error handler global | `UseExceptionHandler` (Modulo 2) |

### Verificacion

```bash
# Probar manejo de errores: iniciar solo la Web (sin el API)
cd src/EDChat.Web
dotnet run &

# 1. Abrir http://localhost:5003
# 2. Intentar hacer login
# 3. Deberia mostrar "Error al conectar con el servidor" en el snackbar
# 4. Ctrl+C para detener

# Ahora probar con el API activo
cd src/EDChat.Api && dotnet run &
cd src/EDChat.Web && dotnet run &

# 1. Login deberia funcionar
# 2. El chat deberia mostrar salas y permitir enviar mensajes
# 3. Ctrl+C en ambas terminales
```

---

## Estado del proyecto al finalizar

### Archivos creados/modificados
```
EDChat.Web/
├── Components/
│   ├── Pages/
│   │   ├── Home.razor
│   │   └── Chat.razor
│   └── ...
├── Services/
│   ├── ApiClient.cs
│   └── ChatService.cs
├── Program.cs
└── EDChat.Web.csproj
```

### Verificacion

```bash
cd src/EDChat.Web
dotnet build
```

La app ahora consume el API REST: login real, lista de salas, mensajes historicos, y todo con manejo de errores.
