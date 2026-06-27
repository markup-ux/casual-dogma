<#
install_applier_task.ps1 - Register the DDON level-sync applier as a Scheduled Task.

Why a scheduled task: DDO.exe runs elevated (its manifest requires admin), so the applier must be
elevated too. A scheduled task with "highest privileges" runs the applier silently at logon with no
extra UAC prompt, and the applier idles until DDO.exe appears. This is the cleanest auto-start.

Run this ONCE from an elevated PowerShell:
    powershell -ExecutionPolicy Bypass -File install_applier_task.ps1

Options:
    -Signal <path>     Override the sync signal file (default the server's sync_state.json)
    -ServerUrl <url>   Use network mode instead of the local file (see apply_sync.py --server)
    -CharName <name>   Character name for network mode (e.g. "Navo Magi")
#>
param(
    [string]$Signal = "",
    [string]$ServerUrl = "",
    [string]$CharName = ""
)

# Self-elevate: registering a Highest-privilege task requires admin. If we're not elevated, relaunch
# ourselves elevated (one UAC prompt) preserving the parameters.
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    $relaunchArgs = @("-ExecutionPolicy","Bypass","-File","`"$PSCommandPath`"")
    if ($Signal)    { $relaunchArgs += @("-Signal","`"$Signal`"") }
    if ($ServerUrl) { $relaunchArgs += @("-ServerUrl","`"$ServerUrl`"") }
    if ($CharName)  { $relaunchArgs += @("-CharName","`"$CharName`"") }
    Start-Process -FilePath "powershell.exe" -ArgumentList $relaunchArgs -Verb RunAs
    return
}

$ErrorActionPreference = "Stop"
$TaskName = "DDONLevelSyncApplier"
$LogPath = Join-Path $PSScriptRoot "install_task.log"
function Log($m) { $line = "[{0}] {1}" -f (Get-Date -Format o), $m; Write-Host $line; Add-Content -Path $LogPath -Value $line }
Set-Content -Path $LogPath -Value "" -ErrorAction SilentlyContinue
trap { Log "ERROR: $($_ | Out-String)"; exit 1 }

# Prefer the standalone exe (no Python needed). Look next to this script, then in dist\.
function Resolve-Applier {
    $candidates = @(
        (Join-Path $PSScriptRoot "apply_sync.exe"),
        (Join-Path $PSScriptRoot "dist\apply_sync.exe")
    )
    foreach ($c in $candidates) { if (Test-Path $c) { return @{ Exe = $c; Script = $null } } }
    # Fall back to running the .py via pythonw (dev machines).
    $script = Join-Path $PSScriptRoot "apply_sync.py"
    if (-not (Test-Path $script)) { throw "Neither apply_sync.exe nor apply_sync.py found next to this script." }
    $py = (Get-Command pythonw.exe -ErrorAction SilentlyContinue).Source
    if (-not $py) {
        $pyExe = (Get-Command python.exe -ErrorAction SilentlyContinue).Source
        if (-not $pyExe) { throw "No apply_sync.exe and Python not on PATH. Ship the exe or install Python." }
        $candidate = Join-Path (Split-Path $pyExe) "pythonw.exe"
        $py = if (Test-Path $candidate) { $candidate } else { $pyExe }
    }
    return @{ Exe = $py; Script = $script }
}

$applier = Resolve-Applier
$exe = $applier.Exe

$argList = @()
if ($applier.Script) { $argList += "`"$($applier.Script)`"" }   # pythonw needs the script path
if ($ServerUrl) { $argList += @("--server", "`"$ServerUrl`"") }
if ($CharName)  { $argList += @("--char", "`"$CharName`"") }
if ($Signal)    { $argList += @("--signal", "`"$Signal`"") }
$arguments = [string]::Join(" ", $argList)
$workDir = Split-Path $exe

Log "Registering task '$TaskName'"
Log "  exe    : $exe"
Log "  args   : $arguments"
Log "  whoami : $(whoami)"

$action    = New-ScheduledTaskAction -Execute $exe -Argument $arguments -WorkingDirectory $workDir
$trigger   = New-ScheduledTaskTrigger -AtLogOn -User $env:USERNAME
$principal = New-ScheduledTaskPrincipal -UserId $env:USERNAME -LogonType Interactive -RunLevel Highest
$settings  = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries `
                -StartWhenAvailable -ExecutionTimeLimit ([TimeSpan]::Zero) -RestartCount 3 -RestartInterval (New-TimeSpan -Minutes 1)

Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Principal $principal `
    -Settings $settings -Description "DDON zone level-sync combat applier (external, non-invasive)." -Force | Out-Null
Log "Register-ScheduledTask succeeded."

Start-ScheduledTask -TaskName $TaskName
Log "Started task. Auto-starts at logon. Applier log: $((Join-Path $PSScriptRoot 'applier.log'))"
