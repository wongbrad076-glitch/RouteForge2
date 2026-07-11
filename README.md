# RouteForge

RouteForge is a free, open-source-style Windows MVP inspired by game-routing tools.

## What this first version does

- Tests several destinations for average latency, jitter, and packet loss
- Scores each route so unstable routes are penalized
- Imports WireGuard `.conf` relay profiles
- Connects or disconnects a selected WireGuard tunnel
- Launches a chosen game executable

## What it does not do yet

- It does not include a worldwide relay network
- It does not automatically identify every game server
- It does not duplicate packets across multiple live paths
- It does not guarantee lower ping; a relay only helps when the ISP's normal route is inefficient

## Requirements

- Windows 10 or 11
- .NET 8 SDK to build
- WireGuard for Windows for relay connections
- Visual Studio 2022 Community, or the `dotnet` command line

## Build

Open PowerShell inside the extracted `RouteForge` folder:

```powershell
dotnet restore
dotnet build -c Release
dotnet run --project .\RouteForge\RouteForge.csproj
```

Publish a standalone Windows build:

```powershell
dotnet publish .\RouteForge\RouteForge.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The app will appear under:

```text
RouteForge\bin\Release\net8.0-windows\win-x64\publish\
```

## Relay setup

You need a WireGuard server or VPN provider that gives you a WireGuard `.conf` file.

1. Install WireGuard for Windows.
2. Open RouteForge.
3. Select **Add relay profile**.
4. Choose the `.conf` file.
5. Select the profile and click **Connect selected relay**.
6. Run route tests before and after connecting.
7. Keep the relay only when it improves the score.

## Recommended next development steps

1. Add automatic before/after comparison for every relay.
2. Add game profiles with known server hosts and ports.
3. Add per-application split tunneling using Windows Filtering Platform.
4. Add a small relay agent for inexpensive VPS servers.
5. Add live route switching with a safe cooldown.
6. Add signed installer and automatic updates.

## Important

Use this only with games and networks whose rules permit VPN or relay routing.
Do not use it to bypass bans, regional restrictions, or access controls.
