# Clase 1: Render modes e interactividad en Blazor con .NET 10
> Duracion estimada: ~12 min

---

## Capitulo 1: Blazor Web App

### Explicacion

Blazor es el framework de .NET para construir interfaces web interactivas usando C# en vez de JavaScript. En .NET 10, Blazor Web App unifica todos los modelos de rendering en un solo template de proyecto.

A diferencia de frameworks como React o Angular, Blazor usa componentes `.razor` que combinan HTML con logica C#. No necesitas aprender un lenguaje nuevo — usas el mismo C# del API.

En el Modulo 2 construimos el backend (API REST con Minimal APIs). Ahora construimos el frontend que se conectara a esa API.

---

## Capitulo 2: Render modes: Static, Interactive Server, WASM, Auto

### Explicacion

Blazor Web App ofrece cuatro render modes que definen **donde** y **como** se ejecuta el componente:

| Render Mode | Donde ejecuta | Interactividad | Conexion |
|-------------|--------------|----------------|----------|
| **Static SSR** | Servidor | No (solo HTML) | HTTP normal |
| **Interactive Server** | Servidor | Si | SignalR (WebSocket) |
| **Interactive WebAssembly** | Navegador | Si | Ninguna (local) |
| **Interactive Auto** | Servidor → Navegador | Si | SignalR → luego local |

- **Static SSR**: El servidor genera HTML y lo envia. Sin botones, sin eventos, sin estado. Ideal para paginas de contenido.
- **Interactive Server**: El servidor mantiene el estado del componente. Cada click viaja por WebSocket al servidor, se procesa, y el diff de UI se envia de vuelta. Latencia baja, pero requiere conexion permanente.
- **Interactive WebAssembly (WASM)**: El runtime de .NET se descarga al navegador. Todo corre local. Primera carga lenta, despues funciona offline.
- **Interactive Auto**: Usa Server mientras WASM se descarga en background. Despues cambia a WASM automaticamente.

---

## Capitulo 3: Cuando usar cada render mode

### Explicacion

La eleccion depende del tipo de aplicacion:

| Caso de uso | Render mode recomendado |
|-------------|----------------------|
| Blog, landing page | Static SSR |
| Dashboard, apps internas | Interactive Server |
| PWA, apps offline | Interactive WASM |
| Apps publicas con muchos usuarios | Interactive Auto |

**Para EDChat usamos Interactive Server** porque:
1. Ya tenemos SignalR para chat en tiempo real — Interactive Server usa la misma conexion WebSocket
2. El estado del chat (mensajes, salas, usuario) se maneja en el servidor
3. No necesitamos funcionalidad offline
4. La carga inicial es rapida (no hay que descargar runtime WASM)

En `.razor`, el render mode se aplica con `@rendermode`:
```razor
@rendermode InteractiveServer
```

O a nivel global en `App.razor`:
```razor
<Routes @rendermode="InteractiveServer" />
```

---

## Capitulo 4: Crear el proyecto Blazor Web App

### Explicacion

Creamos el proyecto Blazor Web App con interactividad Server. Primero lo ejecutamos con el template por defecto para ver como luce, y despues lo configuramos con MudBlazor.

### Comandos

```bash
# Desde la raiz de la solucion EDChat/
dotnet new blazor -n EDChat.Web -o src/EDChat.Web --interactivity Server
dotnet sln add src/EDChat.Web
```

### Configurar el puerto

Reemplaza el contenido de `src/EDChat.Web/Properties/launchSettings.json`:

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "applicationUrl": "http://localhost:5003",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

### Configurar appsettings.json

Reemplaza el contenido de `src/EDChat.Web/appsettings.json`:

```json
{
  "ApiUrl": "http://localhost:5001",
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Build y run para ver el template por defecto

```bash
dotnet build
dotnet run --project src/EDChat.Web
```

Abrir http://localhost:5003 en el navegador. Este es el template por defecto de Blazor con Bootstrap: tiene un sidebar con NavMenu, Counter y Weather. Detener el servidor con `Ctrl+C`.

---

## Capitulo 5: Configurar MudBlazor

### Explicacion

**MudBlazor** es una libreria de componentes UI basada en Material Design que nos da botones, campos de texto, layouts y dialogos listos para usar. Reemplaza Bootstrap del template por defecto.

### Limpiar archivos de ejemplo del template

```bash
# Eliminar paginas de ejemplo del template
rm Components/Pages/Counter.razor 2>/dev/null
rm Components/Pages/Weather.razor 2>/dev/null
rm Components/Pages/Error.razor 2>/dev/null
rm Components/Layout/NavMenu.razor 2>/dev/null
rm Components/Layout/NavMenu.razor.css 2>/dev/null
rm Components/Layout/MainLayout.razor.css 2>/dev/null
```

### Agregar el paquete MudBlazor

```bash
dotnet add src/EDChat.Web package MudBlazor
```

### Configurar Program.cs para MudBlazor

### Prompt para Claude Code

```text
Reemplaza el contenido de Program.cs en EDChat.Web con la configuracion para Blazor Interactive Server con MudBlazor:

- Agrega using MudBlazor.Services
- Registra los servicios: AddRazorComponents() con .AddInteractiveServerComponents() y AddMudServices()
- En el pipeline: MapStaticAssets(), UseAntiforgery()
- Mapea: MapRazorComponents<App>() con .AddInteractiveServerRenderMode()
- Importa el componente App desde EDChat.Web.Components (using EDChat.Web.Components para que App este disponible)
```

### Codigo esperado

```csharp
// Program.cs
using EDChat.Web.Components;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();

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

### Configurar App.razor con MudBlazor

### Prompt para Claude Code

```text
Reemplaza el contenido de Components/App.razor en EDChat.Web. Es el componente raiz HTML del proyecto:

- DOCTYPE html con lang="es"
- En <head>: charset utf-8, viewport, titulo "EDChat", base href "/", <ResourcePreloader /> (preload de assets del framework), font Roboto desde Google Fonts, hojas de estilo con fingerprinting usando @Assets["..."] para MudBlazor y app.css, <ImportMap /> (import maps de JS), y <HeadOutlet @rendermode="InteractiveServer" />
- En <body>: <Routes @rendermode="InteractiveServer" /> para habilitar interactividad global, script de Blazor con @Assets["_framework/blazor.web.js"] y script de MudBlazor (_content/MudBlazor/MudBlazor.min.js)
```

### Codigo esperado

```razor
<!-- Components/App.razor -->
<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>EDChat</title>
    <base href="/" />
    <ResourcePreloader />
    <link href="https://fonts.googleapis.com/css2?family=Roboto:wght@300;400;500;700&display=swap" rel="stylesheet" />
    <link href="@Assets["_content/MudBlazor/MudBlazor.min.css"]" rel="stylesheet" />
    <link href="@Assets["app.css"]" rel="stylesheet" />
    <ImportMap />
    <HeadOutlet @rendermode="InteractiveServer" />
</head>
<body>
    <Routes @rendermode="InteractiveServer" />
    <script src="@Assets["_framework/blazor.web.js"]"></script>
    <script src="_content/MudBlazor/MudBlazor.min.js"></script>
</body>
</html>
```

### Configurar Routes.razor con providers de MudBlazor

### Prompt para Claude Code

```text
Reemplaza el contenido de Components/Routes.razor en EDChat.Web. Debe incluir los providers de MudBlazor (MudThemeProvider, MudPopoverProvider, MudDialogProvider, MudSnackbarProvider) y el Router de Blazor con AppAssembly typeof(Program).Assembly y NotFoundPage typeof(Pages.NotFound), usando RouteView con DefaultLayout typeof(Layout.MainLayout) dentro de <Found>.

Tambien crea Components/Pages/NotFound.razor con ruta "/not-found": un MudContainer centrado con MudText Typo.h4 "Pagina no encontrada" y un MudButton que navegue a "/".
```

### Codigo esperado

```razor
<!-- Components/Routes.razor -->
<MudThemeProvider />
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />

<Router AppAssembly="typeof(Program).Assembly" NotFoundPage="typeof(Pages.NotFound)">
    <Found Context="routeData">
        <RouteView RouteData="routeData" DefaultLayout="typeof(Layout.MainLayout)" />
        <FocusOnNavigate RouteData="routeData" Selector="h1" />
    </Found>
</Router>
```

```razor
<!-- Components/Pages/NotFound.razor -->
@page "/not-found"

<MudContainer MaxWidth="MaxWidth.Small" Class="d-flex align-center justify-center" Style="min-height: 100vh">
    <MudStack AlignItems="AlignItems.Center" Spacing="4">
        <MudText Typo="Typo.h4">Pagina no encontrada</MudText>
        <MudButton Variant="Variant.Filled" Color="Color.Primary" Href="/">Volver al inicio</MudButton>
    </MudStack>
</MudContainer>
```

### Configurar _Imports.razor

### Prompt para Claude Code

```text
Reemplaza el contenido de Components/_Imports.razor en EDChat.Web con los usings necesarios:

@using System.Net.Http
@using System.Net.Http.Json
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using static Microsoft.AspNetCore.Components.Web.RenderMode
@using Microsoft.JSInterop
@using EDChat.Web.Components
@using MudBlazor
```

### Codigo esperado

```razor
<!-- Components/_Imports.razor -->
@using System.Net.Http
@using System.Net.Http.Json
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using static Microsoft.AspNetCore.Components.Web.RenderMode
@using Microsoft.JSInterop
@using EDChat.Web.Components
@using MudBlazor
```

### Build y run para ver con MudBlazor

```bash
dotnet build
dotnet run --project src/EDChat.Web
```

Abrir http://localhost:5003 en el navegador. Ahora la app usa MudBlazor en vez de Bootstrap. Detener el servidor con `Ctrl+C`.

---

## Capitulo 6: Estructura de archivos y carpetas

### Explicacion

Despues de limpiar el template y configurar MudBlazor, la estructura final es:

```
EDChat.Web/
├── Components/
│   ├── Layout/
│   │   └── MainLayout.razor
│   ├── Pages/
│   │   ├── Home.razor
│   │   └── NotFound.razor
│   ├── App.razor
│   ├── Routes.razor
│   └── _Imports.razor
├── wwwroot/
│   └── app.css
├── Properties/
│   └── launchSettings.json
├── appsettings.json
├── Program.cs
└── EDChat.Web.csproj
```

**Archivos clave:**
- `App.razor`: El HTML raiz. Aqui se define el render mode global (`@rendermode="InteractiveServer"`)
- `Routes.razor`: El router de Blazor. Decide que componente renderizar segun la URL
- `_Imports.razor`: Los `@using` que aplican a todos los componentes `.razor`
- `MainLayout.razor`: El layout compartido (header, sidebar, etc.)
- `Program.cs`: Registra servicios y configura el pipeline (igual que en el API)

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
│   │   └── NotFound.razor
│   ├── App.razor
│   ├── Routes.razor
│   └── _Imports.razor
├── Properties/
│   └── launchSettings.json
├── appsettings.json
├── Program.cs
└── EDChat.Web.csproj
```

### Verificacion

```bash
cd src/EDChat.Web
dotnet build
```

Debe compilar sin errores. La app aun no tiene paginas funcionales — eso se hace en la Clase 2.
