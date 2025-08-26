# Connect4 — Client–Server (.NET)

A complete client–server Connect Four system for a course project.

- **Server** — ASP.NET Core **Web API** (JSON over HTTP, `Newtonsoft.Json`)  
- **Client** — **WinForms** (GDI+ drawing & simple animations)  
- **Gameplay** — Human client sending moves to server (baseline endpoints; extendable to AI/random logic)  
- **Polish** — Clear separation of concerns, handy troubleshooting notes, and multi-project solution setup  

## Table of Contents
- [Tech Stack](#tech-stack)
- [Folder Structure (expanded)](#folder-structure-expanded)
- [Notable Files & Entry Points](#notable-files--entry-points)
- [Prerequisites](#prerequisites)
- [Quick Start](#quick-start)
- [Configuration](#configuration)
- [Running the Projects](#running-the-projects)
- [Features](#features)
- [Client (WinForms)](#client-winforms)
- [Credits](#credits)

## Tech Stack
- **Server:** .NET (ASP.NET Core Web API), JSON via **Newtonsoft.Json**  
- **Client:** .NET (Windows), **WinForms** with GDI+ (`Graphics`, `Bitmap`, `Timer`)  
- **Build/IDE:** Visual Studio 2022 or `dotnet` CLI  
- **Repo Languages:** Mostly C#, with some HTML/CSS/JS

## Folder Structure (expanded)
```
Connect4/
├─ ConnectFourClient/                  # WinForms desktop client
│  ├─ Data/                            # (e.g.) ReplayDbContext (SQLite), entities, migrations
│  ├─ Models/                          # DTOs for API + local replay models
│  ├─ Services/                        # ApiClient (HTTP), serialization, error handling
│  ├─ Properties/                      # Settings, resources, AssemblyInfo
│  ├─ Form1.cs                         # Main UI (board, timers, drawing, input)
│  └─ *.cs                             # UI helpers, animations, utilities
│
├─ ConnectFourWeb/                     # ASP.NET Core Web API
│  ├─ Controllers/                     # (e.g.) GamesController, MovesController
│  ├─ Data/                            # EF Core DbContext, entities, configurations
│  ├─ Services/                        # Game engine / rules, random move generator
│  ├─ Filters/ (optional)              # Exception → ProblemDetails mapping, validation filters
│  ├─ Middleware/ (optional)           # Uniform error handling/log correlation
│  ├─ appsettings*.json                # Connection strings, logging config
│  └─ Program.cs                       # DI, routing, swagger (if present), middleware pipeline
│
├─ packages/
│  └─ Newtonsoft.Json.13.0.3/          # JSON library used by the solution
└─ ConnectFour.sln                     # Solution file
```

## Notable Files & Entry Points
**Server (ASP.NET Core Web API)**
- `Controllers/GamesController.cs` *(or similarly named)*  
  Exposes endpoints to create/get games; shows `[ApiController]`, attribute routing, proper status codes, and DI of a game service.
- `Controllers/MovesController.cs` *(or similar)*  
  Handles move submission, column bounds/full checks, win/draw detection, and returns consistent DTOs. Add `ILogger` breadcrumbs for key actions.
- `Data/GameDbContext.cs` *(or similar)*  
  EF Core configuration for entities like `Player`, `Game`, `Move`; relationships and indices in `OnModelCreating`.
- `Services/GameEngine.cs` *(or similar)*  
  Pure game logic: drop piece, check four-in-a-row, random move generation. Easy to unit test.
- `Filters/ApiExceptionFilter.cs` *(optional)*  
  Converts exceptions to RFC 7807 `ProblemDetails` so API errors are uniform and clean.

**Client (WinForms)**
- `Form1.cs`  
  Main UI: double buffering to prevent flicker, GDI+ drawing (board/discs), timer-driven “falling disc” animation, input handling, temporary input blocking during server turn.
- `Services/ApiClient.cs`  
  Central HTTP layer: base URL config, `HttpClient` reuse, JSON serialization (Newtonsoft), translate `ProblemDetails` into user-friendly messages, optional retry/backoff.
- `Data/ReplayDbContext.cs` *(if present)*  
  Local SQLite store for replays; shows EF Core on the client with `EnsureCreated()` and simple LINQ queries.
- `Models/GameDto.cs`, `Models/MoveDto.cs` *(or similar)*  
  Strongly typed JSON contracts for client–server communication.

## Prerequisites
- **Windows 10/11** (for the WinForms client)  
- **.NET SDK 8.0+** (recommended)  
- **Visual Studio 2022** (or use the `dotnet` CLI)

## Quick Start
```bash
# Clone
git clone https://github.com/NirAvraham1/Connect4.git
cd Connect4

# Restore
dotnet restore

# Run server
cd ConnectFourWeb
dotnet run
# Note the URL (e.g., http://localhost:5221)

# Run client (new terminal)
cd ../ConnectFourClient
dotnet run
```

## Configuration
- **Server URL:** Printed on startup (e.g., `http://localhost:5221`).  
- **Client base URL:** Set in the client’s HTTP layer (e.g., `Services/ApiClient.cs`). Must match the server URL/port.

## Running the Projects
**Visual Studio**
1. Open `ConnectFour.sln`.  
2. **Set Startup Projects… → Multiple**, start both `ConnectFourWeb` and `ConnectFourClient`.  
3. Run (F5). Ensure the client base URL points to the server.

**CLI**
```bash
dotnet run --project ConnectFourWeb
dotnet run --project ConnectFourClient
```

## Features
- **Board & Rules (server):** JSON endpoints to create a game, submit a move, and query state (routes depend on your controllers).  
- **Client UX:** Column hover and smooth disc “drop” animation (timer-based).  
- **Extensibility:** Add replay, last-move highlighting, or server-driven prompts (e.g., MessageBox from API responses).

## Client (WinForms)
- **Play:** Start a game, click a column to drop your disc; the client posts the move to the server.  
- **Animations:** Frame-based GDI+ drawing with a `Timer` for falling discs and subtle highlights.  
- **Blocking:** Optionally disable input while the server is “thinking” (responding).

## Credits
- **Author:** Nir Avraham  

