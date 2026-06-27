@echo off
REM Double-click setup for the DDON zone level-sync applier.
REM Prompts for the server address and character name (network mode), then registers the
REM auto-start task. Leave both blank to use local-file mode (only works if the game and
REM server share this PC).

setlocal
echo ============================================================
echo   DDON Level Sync - applier setup
echo ============================================================
echo.
set /p SERVERHOST=Server address (host or IP, blank = same PC as server): 
set /p CHARNAME=Your character name exactly as in-game (e.g. Navo Magi): 
echo.

set PS=powershell.exe -ExecutionPolicy Bypass -File "%~dp0install_applier_task.ps1"

if "%SERVERHOST%"=="" (
  echo Installing in local-file mode...
  %PS%
) else (
  echo Installing in network mode for "%CHARNAME%" via http://%SERVERHOST%:52099 ...
  %PS% -ServerUrl "http://%SERVERHOST%:52099" -CharName "%CHARNAME%"
)

echo.
echo Done. Approve the UAC prompt if one appears. You can close this window.
pause
