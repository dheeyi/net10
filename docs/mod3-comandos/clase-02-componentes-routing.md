# Clase 2: Componentes y Routing
> Duracion estimada: ~12 min

---

## Capitulo 1: Crear componentes reutilizables .razor

### Explicacion

Un componente `.razor` combina HTML y C# en un solo archivo. La seccion `@code { }` contiene la logica C#. Los componentes son la unidad fundamental de Blazor — todo es un componente: paginas, layouts, botones, formularios.

Antes de crear las paginas, necesitamos **modelos** que representen los datos que viene del API. Son clases simples que reflejan los DTOs del backend.

### Comandos

```bash
cd src/EDChat.Web
mkdir Models
```

### Prompt para Claude Code

```text
En la carpeta Models/ de EDChat.Web, crea tres archivos con clases simples (no records, clases normales con propiedades auto-implementadas):

UserModel.cs - propiedades: int Id, string Username (default string.Empty), DateTime CreatedAt

RoomModel.cs - propiedades: int Id, string Name (default string.Empty), string Description (default string.Empty), DateTime CreatedAt

MessageModel.cs - propiedades: int Id, string Content (default string.Empty), DateTime SentAt, int UserId, string Username (default string.Empty), int RoomId

Namespace: EDChat.Web.Models.
```

### Codigo esperado

```csharp
// Models/UserModel.cs
namespace EDChat.Web.Models;

public class UserModel
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
```

```csharp
// Models/RoomModel.cs
namespace EDChat.Web.Models;

public class RoomModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
```

```csharp
// Models/MessageModel.cs
namespace EDChat.Web.Models;

public class MessageModel
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int RoomId { get; set; }
}
```

### Actualizar _Imports.razor

### Prompt para Claude Code

```text
Agrega @using EDChat.Web.Models al final de Components/_Imports.razor en EDChat.Web. Esto evita tener que agregar @using en cada componente que use los modelos.
```

### Codigo esperado

```razor
@* Agregar al final de Components/_Imports.razor *@
@using EDChat.Web.Models
```

---

## Capitulo 2: @page directive y routing

### Explicacion

`@page "/ruta"` convierte un componente en una **pagina navegable**. El router de Blazor (en `Routes.razor`) busca componentes con `@page` y los renderiza cuando la URL coincide.

Las rutas pueden tener parametros: `@page "/chat/{Username}"` captura el segmento de la URL en una propiedad `Username` del componente.

Creamos la pagina de inicio (`Home.razor`) — un formulario de login simple donde el usuario escribe su nombre y navega al chat.

### Prompt para Claude Code

```text
Reemplaza el contenido de Components/Pages/Home.razor en EDChat.Web. Es la pagina de login con ruta "/":

- Usa MudContainer con MaxWidth="MaxWidth.Small" y Class="d-flex align-center" con Style="min-height: 100vh"
- Dentro, un MudPaper con Elevation="3", Class="pa-8" y Style="width: 100%"
- Titulo con MudText Typo.h3 Align Center: "EDChat"
- Subtitulo con MudText Typo.subtitle1 Align Center Class "mb-6": "Ingresa tu nombre para comenzar"
- MudTextField @bind-Value="username" Label="Nombre de usuario" Variant="Variant.Outlined" Class="mb-4"
- MudButton Variant="Variant.Filled" Color="Color.Primary" FullWidth="true" que llame al metodo Login, con texto "Entrar al chat" y Disabled cuando username es null o whitespace
- En @code: campo privado string username, metodo privado void Login() que use NavigationManager.NavigateTo($"/chat/{username}") si username no es vacio
- Inyectar NavigationManager con @inject
```

### Codigo esperado

```razor
<!-- Components/Pages/Home.razor -->
@page "/"
@inject NavigationManager NavigationManager

<MudContainer MaxWidth="MaxWidth.Small" Class="d-flex align-center" Style="min-height: 100vh">
    <MudPaper Elevation="3" Class="pa-8" Style="width: 100%">
        <MudText Typo="Typo.h3" Align="Align.Center">EDChat</MudText>
        <MudText Typo="Typo.subtitle1" Align="Align.Center" Class="mb-6">
            Ingresa tu nombre para comenzar
        </MudText>

        <MudTextField @bind-Value="username"
                      Label="Nombre de usuario"
                      Variant="Variant.Outlined"
                      Class="mb-4" />

        <MudButton Variant="Variant.Filled"
                   Color="Color.Primary"
                   FullWidth="true"
                   Disabled="@(string.IsNullOrWhiteSpace(username))"
                   OnClick="Login">
            Entrar al chat
        </MudButton>
    </MudPaper>
</MudContainer>

@code {
    private string? username;

    private void Login()
    {
        if (!string.IsNullOrWhiteSpace(username))
        {
            NavigationManager.NavigateTo($"/chat/{username}");
        }
    }
}
```

### Puntos clave
- `@page "/"` define la ruta raiz
- `@inject` inyecta servicios del DI (como en los endpoints del API)
- `@bind-Value` crea two-way binding entre el campo y la UI
- `NavigationManager.NavigateTo()` navega programaticamente a otra ruta

---

## Capitulo 3: Layout components y NavMenu

### Explicacion

Un **Layout** es un componente que envuelve a las paginas. Define la estructura comun: header, sidebar, footer. Usa `@Body` para indicar donde se renderiza el contenido de la pagina.

`MainLayout.razor` debe heredar de `LayoutComponentBase` y usar `@Body` en su markup. En `Routes.razor` lo referenciamos como `DefaultLayout`.

### Prompt para Claude Code

```text
Reemplaza el contenido de Components/Layout/MainLayout.razor en EDChat.Web. Debe heredar de LayoutComponentBase con @inherits. El layout es minimo: un MudLayout con un MudMainContent que contiene @Body. No agregar AppBar ni Drawer todavia — se agregarán en la pagina de Chat directamente.
```

### Codigo esperado

```razor
<!-- Components/Layout/MainLayout.razor -->
@inherits LayoutComponentBase

<MudLayout>
    <MudMainContent>
        @Body
    </MudMainContent>
</MudLayout>
```

### Puntos clave
- `@inherits LayoutComponentBase` es obligatorio para layouts
- `@Body` es donde Blazor renderiza la pagina actual
- El layout se aplica a todas las paginas via `DefaultLayout` en `Routes.razor`

---

## Capitulo 4: Pasar datos entre componentes (parameters)

### Explicacion

Los componentes reciben datos via **parameters** — propiedades marcadas con `[Parameter]`. En la URL, los segmentos `{Nombre}` se mapean automaticamente a parametros del componente.

Creamos `Chat.razor` como pagina con un parametro de ruta. Por ahora es una version basica que solo muestra el nombre del usuario — en las siguientes clases le agregaremos SignalR y consumo de API.

### Prompt para Claude Code

```text
En Components/Pages/ de EDChat.Web, crea Chat.razor con ruta "/chat/{Username}":

- Parametro [Parameter] public string Username (default string.Empty)
- Un MudLayout con:
  - MudAppBar con Elevation 2: MudText con "EDChat - Bienvenido, {Username}"
  - MudMainContent con Class "pa-4":
    - MudText Typo.h5: "Sala de chat"
    - MudText Typo.body1 Class "mb-4": "Conectado como {Username}. El chat se construira en las siguientes clases."
    - MudButton que navegue a "/" con texto "Cerrar sesion" usando NavigationManager

Inyectar NavigationManager.
```

### Codigo esperado

```razor
<!-- Components/Pages/Chat.razor -->
@page "/chat/{Username}"
@inject NavigationManager NavigationManager

<MudLayout>
    <MudAppBar Elevation="2">
        <MudText Typo="Typo.h6">EDChat - Bienvenido, @Username</MudText>
    </MudAppBar>

    <MudMainContent Class="pa-4">
        <MudText Typo="Typo.h5">Sala de chat</MudText>
        <MudText Typo="Typo.body1" Class="mb-4">
            Conectado como @Username. El chat se construira en las siguientes clases.
        </MudText>
        <MudButton Variant="Variant.Outlined" OnClick="Logout">Cerrar sesion</MudButton>
    </MudMainContent>
</MudLayout>

@code {
    [Parameter]
    public string Username { get; set; } = string.Empty;

    private void Logout()
    {
        NavigationManager.NavigateTo("/");
    }
}
```

### Puntos clave
- `[Parameter]` marca una propiedad para recibir datos externos
- Los parametros de ruta (`{Username}`) se mapean automaticamente a `[Parameter]` con el mismo nombre
- Los parametros tambien se pueden pasar entre componentes padre-hijo: `<MiComponente Titulo="Hola" />`

---

## Capitulo 5: Component lifecycle methods (ejecutar /init en minimal APIS)

### Explicacion

Cada componente tiene un ciclo de vida con metodos que podemos sobreescribir:

| Metodo | Cuando se ejecuta |
|--------|-------------------|
| `OnInitialized` / `OnInitializedAsync` | Una vez al crear el componente |
| `OnParametersSet` / `OnParametersSetAsync` | Cada vez que cambian los parametros |
| `OnAfterRender` / `OnAfterRenderAsync` | Despues de renderizar (tiene `firstRender` bool) |
| `Dispose` | Al destruir el componente (implementar `IDisposable`) |

**Para EDChat usaremos:**
- `OnInitializedAsync`: para conectar SignalR y cargar datos del API (Clases 3-4)
- `Dispose`: para desconectar SignalR al salir de la pagina (Clase 3)

```csharp
// Ejemplo de lifecycle en un componente
@implements IDisposable

@code {
    protected override async Task OnInitializedAsync()
    {
        // Se ejecuta una vez al cargar el componente
        // Aqui conectaremos SignalR y cargaremos salas
    }

    public void Dispose()
    {
        // Se ejecuta al destruir el componente
        // Aqui desconectaremos SignalR
    }
}
```

No necesitamos agregar codigo todavia — estos metodos se implementaran en las Clases 3 y 4 cuando tengamos SignalR y el ApiClient.

### Verificacion

```bash
cd src/EDChat.Web
dotnet build
dotnet run &

# Abrir en el navegador: http://localhost:5003
# 1. Deberia mostrar la pagina de login con campo de texto y boton
# 2. Escribir un nombre y hacer click en "Entrar al chat"
# 3. Deberia navegar a /chat/{nombre} y mostrar el saludo

# Ctrl+C para detener
```

---

## Estado del proyecto al finalizar

### Archivos creados/modificados
```
EDChat.Web/
├── Components/
│   ├── Layout/
│   │   └── MainLayout.razor
│   ├── Pages/
│   │   ├── Home.razor
│   │   └── Chat.razor
│   ├── App.razor
│   ├── Routes.razor
│   └── _Imports.razor
├── Models/
│   ├── UserModel.cs
│   ├── RoomModel.cs
│   └── MessageModel.cs
├── Program.cs
└── EDChat.Web.csproj
```

### Verificacion

```bash
cd src/EDChat.Web
dotnet build
```

La app tiene login funcional con navegacion al chat. En la Clase 3 agregaremos SignalR para mensajes en tiempo real.
