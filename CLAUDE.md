# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Educational .NET 10 project for the "curso de .NET 10, 2026" (EDteam). Contains multiple console application projects targeting `net10.0`.

## Build and Run Commands

```bash
# Build the entire solution
dotnet build

# Run individual projects
dotnet run --project Bienvenida
dotnet run --project src/EDChat.Console

# Restore NuGet dependencies
dotnet restore

# Clean build artifacts
dotnet clean
```

## Project Structure

- **EDChat.slnx** — Solution file
- **Bienvenida/** — Console app project
- **src/EDChat.Console/** — Console app project (EDChat application)
- **helloWorld.cs** — Standalone C# script file

## C# Conventions

All projects use:
- **Target framework**: `net10.0`
- **Implicit usings**: enabled (no need for explicit `using System;` etc.)
- **Nullable reference types**: enabled
- Top-level statements (no `Main` method boilerplate)
