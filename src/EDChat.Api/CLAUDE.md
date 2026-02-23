# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands

```bash
# Build the entire solution
dotnet build /Users/williambarra/Projects/edteam/net10/EDChat.slnx

# Run the API project (http://localhost:5001, https://localhost:5002)
dotnet run --project /Users/williambarra/Projects/edteam/net10/src/EDChat.Api

# Run the Blazor Web project
dotnet run --project /Users/williambarra/Projects/edteam/net10/src/EDChat.Web

# Restore packages
dotnet restore /Users/williambarra/Projects/edteam/net10/EDChat.slnx
```

No test projects exist yet. No linter or formatter is configured.

## Architecture

EDChat is an educational .NET 10 chat application with two projects in `EDChat.slnx`:

- **EDChat.Api** — Minimal API backend (this project)
- **EDChat.Web** — Blazor Server frontend with MudBlazor UI and SignalR client

### API Project Structure

The API uses **Minimal APIs** with route groups, organized in layers:

- **Endpoints/** — Route handlers registered as extension methods on `WebApplication` via `MapGroup()`. Three groups: `RoomEndpoints`, `UserEndpoints`, `MessageEndpoints`. All routes are under `/api/`.
- **Services/ChatStore.cs** — Singleton in-memory data store. No database; data is lost on restart. Seeds two default rooms (General, Tecnología).
- **Models/** — Domain entities: `User`, `Room`, `Message`.
- **DTOs/** — Immutable records with `DataAnnotations` validation attributes for API contracts.
- **Mappers/DtoMappers.cs** — Extension methods converting between DTOs and domain models (uses C# 12+ extension syntax).
- **Middlewares/RequestLoggingMiddleware.cs** — Logs request method, path, status code, and elapsed time.

### Wiring (Program.cs)

`ChatStore` is registered as singleton. OpenAPI + Scalar documentation is enabled at `/scalar/v1`. Endpoint groups are mapped via `app.MapRoomEndpoints()`, `app.MapUserEndpoints()`, `app.MapMessageEndpoints()`.

### Key Conventions

- Language: C# 14 / .NET 10 with nullable reference types and implicit usings enabled.
- Endpoint handlers return `TypedResults` for strongly-typed HTTP responses.
- DTO validation uses `System.ComponentModel.DataAnnotations` with a custom `AddValidation()` extension.
- Documentation is in Spanish (the project is an ED Team course).
- API documentation UI: Scalar at `/scalar/v1`.
