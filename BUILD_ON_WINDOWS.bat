@echo off
setlocal
where dotnet >nul 2>nul
if errorlevel 1 (
  echo .NET 8 SDK is not installed.
  echo Download it from Microsoft's official .NET website, then run this file again.
  pause
  exit /b 1
)

dotnet publish RouteForge\RouteForge.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o RouteForge-Windows
if errorlevel 1 (
  echo Build failed.
  pause
  exit /b 1
)

echo.
echo Build complete: RouteForge-Windows\RouteForge.exe
pause
