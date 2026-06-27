@echo off
REM ============================================================
REM  Dragon's Dogma Online - Server launcher (visible console)
REM  Starts Arrowgene.Ddon.Cli in THIS window so logs stay visible.
REM  Do not use --service or hidden/background launch for local dev.
REM ============================================================

setlocal EnableDelayedExpansion
set "SERVER_DIR=%~dp0Server\Arrowgene.Ddon.Cli\bin\Release\net10.0"
set "SERVER_EXE=Arrowgene.Ddon.Cli.exe"

REM --- Check for an existing server before build/start noise ---
set "EXISTING_COUNT=0"
for /f %%A in ('powershell -NoProfile -Command "(Get-CimInstance Win32_Process | Where-Object { ($_.Name -eq 'Arrowgene.Ddon.Cli.exe') -or (($_.Name -eq 'dotnet.exe') -and ($_.CommandLine -like '*Arrowgene.Ddon.Cli*')) }).Count"') do set "EXISTING_COUNT=%%A"

if %EXISTING_COUNT% GTR 0 (
    echo.
    echo Existing DDON server process detected.
    echo Stopping it before starting a new instance...
    echo.
    call "%~dp0stop-server.bat"
    timeout /t 2 /nobreak >nul

    set "EXISTING_COUNT=0"
    for /f %%A in ('powershell -NoProfile -Command "(Get-CimInstance Win32_Process | Where-Object { ($_.Name -eq 'Arrowgene.Ddon.Cli.exe') -or (($_.Name -eq 'dotnet.exe') -and ($_.CommandLine -like '*Arrowgene.Ddon.Cli*')) }).Count"') do set "EXISTING_COUNT=%%A"
    if !EXISTING_COUNT! GTR 0 (
        echo.
        echo Could not stop the existing DDON server.
        echo Close the other server window or run stop-server.bat, then try again.
        echo.
        echo Server process exited.
        pause
        exit /b 1
    )
)

if not exist "%SERVER_DIR%\%SERVER_EXE%" (
    echo [ERROR] Server executable not found:
    echo         %SERVER_DIR%\%SERVER_EXE%
    echo Build it first with:  dotnet build Server\Arrowgene.Ddon.Cli\Arrowgene.Ddon.Cli.csproj -c Release
    echo.
    pause
    exit /b 1
)

echo Starting DDON server...
echo Directory: %SERVER_DIR%
echo Press Ctrl+C in this window to stop the server.
echo.

cd /d "%SERVER_DIR%"
"%SERVER_EXE%" server start

echo.
echo Server process exited.
pause
endlocal
