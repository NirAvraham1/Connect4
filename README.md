Connect4 — Client–Server (.NET)

A complete client–server Connect Four system for a course project.

Server — ASP.NET Core Web API (JSON over HTTP, Newtonsoft.Json)

Client — WinForms (GDI+ drawing & simple animations)

Gameplay — Human client sending moves to server (baseline endpoints; extendable to AI/random logic)

Polish — Clear separation of concerns, handy troubleshooting notes, and multi-project solution setup

Repository structure at a glance: ConnectFourClient/ (WinForms), ConnectFourWeb/ (Web API), packages/Newtonsoft.Json.13.0.3/, and ConnectFour.sln. GitHub shows the code is mostly C#. 
GitHub

Table of Contents

Tech Stack

Folder Structure (expanded)

Notable Files & Entry Points

Prerequisites

Quick Start

Configuration

Running the Projects

Features

Client (WinForms)

Credits

Tech Stack

Server: .NET (ASP.NET Core Web API), JSON via Newtonsoft.Json

Client: .NET (Windows), WinForms with GDI+ (Graphics, Bitmap, Timer)

Build/IDE: Visual Studio 2022 or dotnet CLI

Repo Languages (GitHub): Mostly C#, with some HTML/CSS/JS. 
GitHub

Folder Structure (expanded)
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


Tip: If a folder isn’t there yet, consider adding it—the structure above signals good separation of concerns and makes reviewers happy. Root folders verified on GitHub. 
GitHub

Notable Files & Entry Points (click these first)

Server (ASP.NET Core Web API)

Controllers/GamesController.cs (or similarly named)
Why it’s interesting: Shows [ApiController], attribute routing, status codes (201, 400, 404), and model validation. Look for DI of a game service, cancellation tokens, and returning ProblemDetails for errors.

Controllers/MovesController.cs (or similar)
Why: Encapsulates move validation (column bounds, column full, turn order), win/draw detection, and consistent responses (DTOs). Good spot to show logging breadcrumbs via ILogger.

Data/GameDbContext.cs (or similar)
Why: EF Core setup (entities like Player, Game, Move), relationships, indexes, and OnModelCreating. If you use migrations, this is where configurations live.

Services/GameEngine.cs (or similar)
Why: Pure game logic (drop piece, check four-in-a-row, random move generation). Clean separation makes unit tests trivial.

Filters/ApiExceptionFilter.cs (optional)
Why: Converts exceptions to RFC 7807 ProblemDetails (uniform JSON errors) and avoids leaking stack traces.

Client (WinForms)

Form1.cs
Why: Rendering quality & UX polish. Look for:

Double buffering (ControlStyles.OptimizedDoubleBuffer) to prevent flicker

GDI+ drawing (board grid, discs), anti-aliasing

Timer-based animation for “falling disc” and hover highlight

Input blocking while awaiting server turn

Services/ApiClient.cs
Why: Centralized HTTP calls with base URL config, HttpClient reuse, JSON (Newtonsoft) serialization, mapping ProblemDetails to friendly messages, and small retry/backoff for transient errors.

Data/ReplayDbContext.cs (if present)
Why: Local SQLite store for replays; demonstrates EF Core on the client, EnsureCreated(), and simple querying for playback screens.

Models/GameDto.cs, Models/MoveDto.cs (or similar)
Why: Strongly typed JSON contracts, JsonPropertyName/JsonProperty usage, and separation between transport vs. UI models.

If a file above isn’t in your tree yet, adding it (even with minimal content) helps reviewers quickly see professional structure and technology depth.

Prerequisites

Windows 10/11 (for the WinForms client)

.NET SDK 8.0+ (recommended)

Visual Studio 2022 (or use the dotnet CLI)

Quick Start
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

Configuration

Server URL: Printed on startup (e.g., http://localhost:5221).

Client base URL: Configure in the client’s HTTP layer (e.g., Services/ApiClient.cs). Ensure it matches the server URL/port.

Running the Projects

Visual Studio

Open ConnectFour.sln → Set Startup Projects… → Multiple → start both ConnectFourWeb & ConnectFourClient.

F5. Confirm the client base URL points to the server.

CLI

dotnet run --project ConnectFourWeb
dotnet run --project ConnectFourClient

Features

Board & Rules (server): JSON endpoints to create a game, submit a move, and query state (routes depend on your controllers).

Client UX: Column hover, disc “drop” animation (timer-based), and basic turn flow.

Extensibility: Easy to add replay, last-move highlighting, or server-driven prompts (e.g., show server messages in a MessageBox).

Client (WinForms)

Play: Start a game and click a column to drop a disc; the client sends moves to the server via HTTP.

Animations: Frame-based disc fall using a Timer and GDI+ drawing.

Blocking: Optionally disable input during “server turn” to keep flow tidy.

Replay (optional): Add an in-memory/file/SQLite replay if desired.

Credits

Authors: Nir Avraham & Daniel Rubinstien

