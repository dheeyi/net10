# Mod 1: .NET 10

Comandos del Mod 1

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

> Comando para crear proyectos a partir de templates predefinidos.

```bash
# Listar templates disponibles
dotnet new list
dotnet new list --tag Web
dotnet new list --tag Common

# Crear proyectos
dotnet new console -n MiApp                  # App de consola
dotnet new webapi -n MiApi                   # API (Minimal APIs)
dotnet new blazor -n MiWeb                   # Blazor Web App
dotnet new classlib -n MiLibreria            # Librería de clases

# Crear proyecto en carpeta específica
dotnet new console -n MiApp -o src/MiApp
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

## [dotnet tool](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-tool-install) - Herramientas globales

> Gestión de herramientas CLI que se instalan como paquetes NuGet.

```bash
dotnet tool list -g                    # Listar herramientas instaladas
dotnet tool install -g dotnet-ef       # Instalar herramienta
dotnet tool update -g dotnet-ef        # Actualizar herramienta
dotnet tool uninstall -g dotnet-ef     # Desinstalar herramienta
```

---

## Solución y proyectos

### [Crear la solución](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-new#create-a-solution)

```bash
dotnet new sln -n EDChat
```

### Crear los proyectos del curso

```bash
dotnet new console -n EDChat.Console -o src/EDChat.Console
dotnet new webapi -n EDChat.Api -o src/EDChat.Api
dotnet new blazor -n EDChat.Web -o src/EDChat.Web
dotnet new classlib -n EDChat.Data -o src/EDChat.Data
```

### [Agregar proyectos a la solución](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-sln#add)

```bash
dotnet sln add src/EDChat.Console
dotnet sln add src/EDChat.Api
dotnet sln add src/EDChat.Web
dotnet sln add src/EDChat.Data
```

### Compilar y ejecutar un proyecto específico

```bash
dotnet build
dotnet run --project src/EDChat.Console
dotnet run --project src/EDChat.Api
dotnet run --project src/EDChat.Web
```

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
exit                      # Salir (o Ctrl+C)
```

---



