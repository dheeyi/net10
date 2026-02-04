# Mod 1: .NET 10

Comandos classe: 1

---

### Carpeta del proyecto - EDChat

> Todo lo trabajaremos en la carpeta net10

```bash
# aca estara la solucion mas lo 4 proyectos
mkdir net10

```

## Estructura final del proyecto

```
EDChat/
├── src/
│   ├── EDChat.Console/   # Mod1 - App de consola
│   ├── EDChat.Api/       # Mod2 - Minimal APIs
│   ├── EDChat.Web/       # Mod3 - Blazor Web App
│   └── EDChat.Data/      # Mod4 - EF Core (class library)
├── docs/
│   ├── mod1-comandos.md
│   ├── mod2-comandos.md
│   ├── mod3-comandos.md
│   └── mod4-comandos.md
├── EDChat.slnx
├── CLAUDE.md
└── .gitignore
```

---

## Instalación

### .NET 10 SDK

> Instalador gráfico: [dotnet.microsoft.com/download](https://dotnet.microsoft.com/en-us/download) | Script de terminal: [dotnet-install-script](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-install-script) | Linux Ubuntu: [linux-ubuntu-install](https://learn.microsoft.com/en-us/dotnet/core/install/linux-ubuntu-install)

```bash
# macOS / Linux (script universal)
# Fuente: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-install-script
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 10.0

# Windows (PowerShell)
winget install Microsoft.DotNet.SDK.10

# Linux (Ubuntu/Debian)
# Fuente: https://learn.microsoft.com/en-us/dotnet/core/install/linux-ubuntu-install
sudo apt-get update && sudo apt-get install -y dotnet-sdk-10.0
```

### [Claude Code](https://docs.anthropic.com/en/docs/claude-code/overview)

> CLI oficial de Anthropic para programar con IA desde la terminal.

```bash
# macOS / Linux
curl -fsSL https://claude.ai/install.sh | bash

# macOS (Homebrew)
brew install --cask claude-code

# Windows (WinGet)
winget install Anthropic.ClaudeCode

# Windows (PowerShell)
irm https://claude.ai/install.ps1 | iex
```

### Verificación del entorno

```bash
dotnet --version          # Verificar versión de .NET
claude doctor             # Verificar Claude Code
```

Comandos classe: 2

---

## [dotnet CLI](https://learn.microsoft.com/en-us/dotnet/core/tools/) - Información

> Documentación completa de todos los comandos de la CLI de .NET.

```bash
dotnet --version          # Versión del SDK instalado
dotnet --info             # Información detallada del entorno
dotnet --list-sdks        # Listar todos los SDKs instalados
dotnet --list-runtimes    # Listar todos los runtimes instalados
```

## [dotnet new](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-new) - Templates

> Comando para ver proyectos a partir de templates predefinidos.

```bash
# Listar templates disponibles
dotnet new list
dotnet new list --tag blazor
dotnet new list --tag api
```

## dotnet CLI - Ciclo de desarrollo

```bash
dotnet build              # Compilar el proyecto
dotnet run                # Compilar y ejecutar
dotnet watch              # Ejecutar con hot reload
dotnet clean              # Limpiar archivos compilados
dotnet restore            # Restaurar paquetes NuGet
dotnet publish            # Publicar para producción
dotnet test               # Ejecutar tests
```

# Crear proyectos ejemplo de consola

```bash
dotnet new console -n Bienvenida       # App de consola

# ahora si ejecutar dentro de este proyecto, los comandos build, run y watch
```

## [dotnet tool](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-tool-install) - Herramientas globales

> Gestión de herramientas CLI que se instalan como paquetes NuGet.

```bash
dotnet tool list -g                    # Listar herramientas instaladas
dotnet tool install -g dotnet-ef       # Instalar herramienta
dotnet tool update -g dotnet-ef        # Actualizar herramienta
dotnet tool uninstall -g dotnet-ef     # Desinstalar herramienta
```

Comandos classe: 3

---

## [dotnet new](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-new) - Templates

> Comando para crear proyectos a partir de templates predefinidos.

```bash
# Crear proyectos: Ejemplos
dotnet new console -n EDChat.Console -o src/EDChat.Console       # App de consola
dotnet new webapi -n EDChat.Api -o src/EDChat.Api                # API (Minimal APIs)
dotnet new blazor -n EDChat.Web -o src/EDChat.Web                # Blazor Web App
dotnet new classlib -n EDChat.Data -o src/EDChat.Data            # Librería de clases

# En .NET manejamos soluciones, una solucion puede tener multiples proyectos dentro

# Crear una solucion para EDChat (dentro de la carpeta net10)
dotnet new sln -n EDChat

# Crear proyecto en carpeta específica (dentro de src)
dotnet new console -n EDChat.Console -o src/EDChat.Console
```

---

### [Agregar proyectos a la solución](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-sln#add)

```bash
dotnet sln EDChat.slnx add src/EDChat.Console
dotnet sln EDChat.slnx add src/EDChat.Api
dotnet sln EDChat.slnx add src/EDChat.Web
dotnet sln EDChat.slnx add src/EDChat.Data
```

### Compilar y ejecutar un proyecto específico

```bash
dotnet build
dotnet run --project src/EDChat.Console
dotnet run --project src/EDChat.Api
dotnet run --project src/EDChat.Web
```

Comandos classe: 4

---

## [Claude Code](https://docs.anthropic.com/en/docs/claude-code/overview)

### Comandos básicos

```bash
claude                    # Iniciar sesión
```

### [Uso dentro de la sesión](https://docs.anthropic.com/en/docs/claude-code/cli-usage)

```bash
/init                     # Crear archivo CLAUDE.md
/doctor                   # Verificar instalación
/help                     # Ver ayuda

# otros
/status
/model
/clear
/compact
exit                      # Salir (o Ctrl+C)
```

---



