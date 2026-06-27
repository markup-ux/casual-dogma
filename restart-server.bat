@echo off
REM Stop, rebuild, and start the DDON server in a visible console window.
REM Do NOT use --service or hidden/minimized launch here; this window is the server console.

setlocal
set "ROOT=%~dp0"
set "CLI_PROJ=%ROOT%Server\Arrowgene.Ddon.Cli\Arrowgene.Ddon.Cli.csproj"

echo === Stopping any running DDON server ===
call "%ROOT%stop-server.bat"
echo.

echo === Rebuilding server (Release) ===
dotnet build "%CLI_PROJ%" -c Release
if errorlevel 1 (
    echo [ERROR] Build failed. Server not started.
    pause
    exit /b 1
)
echo.

echo === Starting DDON server in a new console window ===
start "DDON Server" cmd /k ""%ROOT%start-server.bat""
echo Server launch requested. Look for the "DDON Server" console window.
endlocal
