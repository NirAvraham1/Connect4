Connect4 — WinForms Client + ASP.NET Core Web API

Connect Four with a Desktop client in WinForms and a .NET Web API server.
The client draws the board, “falling discs” animations, highlights and hover; the server manages game state, logical validations, and optional replay/statistics (as needed).

Repository structure:
ConnectFourClient/ (WinForms), ConnectFourWeb/ (Web API), and the solution file ConnectFour.sln. GitHub

✨ Features

7×6 board with drop animation and per-column hover.

Full separation between UI (client) and logic/state (server).

HTTP (JSON) communication between the client and server (Newtonsoft.Json).

Easily extendable: replay, last-move highlight, server-to-client messages (MessageBox), and more.

🧱 Technologies

C# / .NET — WinForms (client), ASP.NET Core (server).

JSON with Newtonsoft.Json.

The repo shows mostly C#, HTML, CSS, and JS (per GitHub “Languages”). GitHub

📁 Project Structure
Connect4/
├─ ConnectFourClient/      # WinForms application (.NET)
│  ├─ (Forms, Services, Api helpers, etc.)
│  └─ app.config / server base-URL file (as in your code)
├─ ConnectFourWeb/         # Web API in ASP.NET Core
│  ├─ Controllers/
│  ├─ Models/
│  ├─ Services/
│  └─ appsettings*.json
└─ ConnectFour.sln         # Visual Studio solution

✅ Prerequisites

Windows 10/11 (for WinForms).

.NET SDK 8.0+ (recommended) and Visual Studio 2022 (or dotnet CLI).

Permissions for local HTTP (Firewall may prompt).

🚀 Quick Start (CLI)
# 1) Clone
git clone https://github.com/NirAvraham1/Connect4.git
cd Connect4

# 2) Restore
dotnet restore

# 3) Run the server (local by default)
cd ConnectFourWeb
dotnet run
# Note: In the client code there was an example Base URL: http://localhost:5221
# If the port/URL differs, update it in the client before running.

# 4) Run the client
cd ../ConnectFourClient
dotnet run

🧩 Configuration — Client connection

In the client there is an ApiClient/BaseUrl class (for example, your code showed "http://localhost:5221"). Make sure the URL matches the port where the server is running. If needed — expose/move the Base URL to a config file (app.config / Settings).

🖥️ Running in Visual Studio

Open ConnectFour.sln.

Set Startup Projects… → choose Multiple and set both ConnectFourWeb and ConnectFourClient to Start.

Ensure in the server configuration the URL is stable (e.g., http://localhost:5221) and that the client is configured with the same base URL.

🔌 API (General description)

The server exposes endpoints for game management: create a game, perform a move, get board state/history.
Tip: if Swagger is enabled in the project, you can browse to /swagger while running to get automatic documentation.

🧱 Architecture — high level
[WinForms Client]  <----HTTP JSON---->  [ASP.NET Core Web API]
Board drawing, UI                        Rules/logic/state
Animations, MessageBox                   Move validation, (optional) replay/DB

🧰 Development & extensions

Falling animations: triggered by dedicated timer(s) based on a “target row” per column.

Highlight: store the “last move” (e.g., (col,row)), draw a halo around the last disc.

Server MessageBox: client listens/checks the API response and shows messages from the server.

Replay: save the sequence of moves on server/client and play it back with a timer.

🛠️ Common troubleshooting

MSB3577: Two output file names resolved to the same output path — usually caused by a duplicate RESX/resource or the same logical path in two projects.
Fix:

Delete bin/ and obj/ for all projects.

Ensure there aren’t two Form1.resx/Form1.resources with the same LogicalName.

In the .csproj, ensure there are no duplicate <EmbeddedResource Include=...> entries.

Client doesn’t connect — ensure the server is running, the port is correct (default we saw: 5221), and Firewall isn’t blocking.

Dev HTTPS certificate issue — if switching to HTTPS: dotnet dev-certs https --trust.

🧪 Useful commands
# Build/clean
dotnet clean
dotnet build

# Run
dotnet run --project ConnectFourWeb
dotnet run --project ConnectFourClient

📜 License

Consider adding a license (e.g., MIT) to the LICENSE file.

👥 Credit

Developed by Nir Avraham and Daniel Rubinstien.
