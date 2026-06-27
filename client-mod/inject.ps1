<#
  inject.ps1 - Inject ddon_mod.dll into a running DDO.exe.

  Injection needs to open DDO.exe with CREATE_THREAD rights, which usually requires
  running elevated. This script self-elevates if needed.

  Usage:
    .\inject.ps1                 # inject ddon_mod.dll (build first if missing)
    .\inject.ps1 -Dll <path>     # inject a specific DLL (e.g. synchook.dll)
    .\inject.ps1 -Build          # force a rebuild before injecting
#>
[CmdletBinding()]
param(
    [string]$Dll,
    [switch]$Build
)

$ErrorActionPreference = 'Stop'
$root   = Split-Path -Parent $MyInvocation.MyCommand.Path
$target = 'i686-pc-windows-msvc'

if (-not $Dll) { $Dll = Join-Path $root "ddon_mod\target\$target\release\ddon_mod.dll" }
$inj = Join-Path $root "inject\target\$target\release\inject.exe"

if ($Build -or -not (Test-Path $Dll) -or -not (Test-Path $inj)) {
    & (Join-Path $root 'build.ps1')
}

if (-not (Get-Process -Name 'DDO' -ErrorAction SilentlyContinue)) {
    Write-Host "[inject] DDO.exe is not running. Start the game, then re-run." -ForegroundColor Yellow
    exit 1
}

$genPath = 'C:\Users\Public\ddon_mod_gen.txt'
$pidPath = 'C:\Users\Public\ddon_mod_pid.txt'
$ddo = Get-Process -Name 'DDO' -ErrorAction SilentlyContinue | Select-Object -First 1
if ($ddo -and (Test-Path $genPath) -and (Test-Path $pidPath)) {
    $lastPid = (Get-Content $pidPath -Raw).Trim()
    if ($lastPid -eq $ddo.Id.ToString()) {
        $gen = (Get-Content $genPath -Raw).Trim()
        Write-Host "[inject] ABORT: mod already injected into this DDO session (pid $($ddo.Id), gen $gen)." -ForegroundColor Red
        Write-Host "[inject] Quit DDO completely, restart the game, then inject ONCE." -ForegroundColor Yellow
        Write-Host "[inject] Hot re-inject without restart leaves stale DLLs + VEH handlers and crashes." -ForegroundColor Yellow
        exit 1
    }
}

# Inject a COPY, never the canonical build output. A loaded DLL is locked on disk, so
# injecting the build artifact directly would block the next `cargo build`.
$stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$live  = Join-Path $env:TEMP "ddon_mod_$stamp.dll"
Copy-Item -LiteralPath $Dll -Destination $live -Force
Write-Host "[inject] injecting copy: $live" -ForegroundColor Cyan

# Self-elevate if not already admin.
$isAdmin = ([Security.Principal.WindowsPrincipal] `
    [Security.Principal.WindowsIdentity]::GetCurrent()
).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "[inject] elevating..." -ForegroundColor Cyan
    Start-Process -FilePath $inj -ArgumentList "`"$live`"" -Verb RunAs -Wait
} else {
    & $inj "$live"
}

$ddoAfter = Get-Process -Name 'DDO' -ErrorAction SilentlyContinue | Select-Object -First 1
if ($ddoAfter) {
    Set-Content -Path $pidPath -Value $ddoAfter.Id -NoNewline
}

Write-Host "[inject] done. Mod log: C:\Users\Public\ddon_mod.log" -ForegroundColor Green
