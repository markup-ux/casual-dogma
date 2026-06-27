@echo off
REM Double-click to diagnose DDON level-sync from logs and live state.
setlocal
cd /d "%~dp0"
echo Running level-sync diagnostics...
echo.
python "%~dp0diagnose_levelsync.py" %*
if errorlevel 1 (
  echo.
  echo Diagnostics found errors — see report above and diagnose_report.txt
) else (
  echo.
  echo Done — see diagnose_report.txt for the full report.
)
echo.
pause
