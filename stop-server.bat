@echo off
REM Stop any running Arrowgene DDON server (dotnet host, direct exe, or Cursor background shells).

powershell -NoProfile -Command ^
  "$termDir = Join-Path $env:USERPROFILE '.cursor\projects\d-DDON\terminals';" ^
  "if (Test-Path $termDir) {" ^
  "  Get-ChildItem $termDir -Filter '*.txt' | ForEach-Object {" ^
  "    $head = Get-Content $_.FullName -TotalCount 6 -ErrorAction SilentlyContinue;" ^
  "    $cmd = ($head | Where-Object { $_ -match '^command:' }) -replace '^command:\s*','';" ^
  "    if ($cmd -match 'Arrowgene\.Ddon\.Cli') {" ^
  "      $pidLine = ($head | Where-Object { $_ -match '^pid:' });" ^
  "      if ($pidLine -match 'pid:\s*(\d+)') {" ^
  "        $shellPid = [int]$Matches[1];" ^
  "        Write-Host ('Stopping background shell PID {0} ({1})' -f $shellPid, $_.Name);" ^
  "        Stop-Process -Id $shellPid -Force -ErrorAction SilentlyContinue;" ^
  "      }" ^
  "    }" ^
  "  }" ^
  "};" ^
  "$procs = Get-CimInstance Win32_Process | Where-Object { ($_.Name -eq 'Arrowgene.Ddon.Cli.exe') -or (($_.Name -eq 'dotnet.exe') -and ($_.CommandLine -like '*Arrowgene.Ddon.Cli*')) };" ^
  "if (-not $procs) { Write-Host 'No DDON server process found.'; exit 0 };" ^
  "foreach ($p in $procs) { Write-Host ('Stopping {0} PID {1}' -f $p.Name, $p.ProcessId); Stop-Process -Id $p.ProcessId -Force -ErrorAction SilentlyContinue };" ^
  "Write-Host 'DDON server stopped.'"
