<#
  monitor.ps1 - Live monitor for DDON client-mod sessions.

  Watches, in one place:
    * DDO.exe lifetime         -> [MON] STARTED / EXITED, and [MON][CRASH] on a
                                  non-zero exit code (with the decoded crash code).
    * the mod logs             -> [MOD]  (C:\Users\Public\ddon_mod.log)
                                  [SYNC] (C:\Users\Public\ddon_synchook.log)
                                  The mod itself prints [CRASH]/[PANIC] lines here.
    * crash minidumps          -> [MON][DUMP] when ddon_mod writes a .dmp.
    * Windows error events      -> [MON][EVT] from the Application event log
                                  (Application Error / .NET Runtime / WER / Hang).

  Sentinel prefixes are stable so they are easy to grep/alert on:
    [MON]  lifecycle    [MON][CRASH]  process crash    [MON][DUMP]  minidump
    [MON][EVT] OS error event   [MOD]/[SYNC] mod log   [CRASH]/[PANIC] in-mod

  Usage:
    .\monitor.ps1
    .\monitor.ps1 -FromStart        # replay existing log history too
    .\monitor.ps1 -PollMs 500
#>
[CmdletBinding()]
param(
    [string]$ProcName  = 'DDO.exe',
    [string]$ModLog    = 'C:\Users\Public\ddon_mod.log',
    [string]$SyncLog   = 'C:\Users\Public\ddon_synchook.log',
    [string]$DumpDir   = 'C:\Users\Public',
    [int]$PollMs       = 1000,
    [switch]$FromStart
)

$ErrorActionPreference = 'Continue'
$base = [System.IO.Path]::GetFileNameWithoutExtension($ProcName)

function Now { (Get-Date).ToString('HH:mm:ss') }

# Decode a Windows exit/exception code to a friendly name when we recognize it.
function Decode-Code([int64]$code) {
    $u = $code -band 0xFFFFFFFF
    $name = switch ($u) {
        0xC0000005 { 'ACCESS_VIOLATION' }
        0xC000001D { 'ILLEGAL_INSTRUCTION' }
        0xC0000096 { 'PRIV_INSTRUCTION' }
        0xC00000FD { 'STACK_OVERFLOW' }
        0xC0000094 { 'INT_DIVIDE_BY_ZERO' }
        0xC000041D { 'FATAL_USER_CALLBACK' }
        0x40010004 { 'DBG_TERMINATE' }
        default    { 'exit' }
    }
    '0x{0:X8} ({1})' -f $u, $name
}

# Tail helper: prints any bytes appended to $path since we last looked.
function Read-NewLines([string]$path, [hashtable]$state, [string]$prefix) {
    if (-not (Test-Path -LiteralPath $path)) { return }
    $fs = $null
    try {
        $fs = [System.IO.File]::Open($path, [System.IO.FileMode]::Open,
              [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
        $len = $fs.Length
        $off = [int64]$state[$path]
        if ($len -lt $off) { $off = 0 }     # truncated / recreated
        if ($len -gt $off) {
            [void]$fs.Seek($off, [System.IO.SeekOrigin]::Begin)
            $buf = New-Object byte[] ($len - $off)
            [void]$fs.Read($buf, 0, $buf.Length)
            $text = [System.Text.Encoding]::UTF8.GetString($buf)
            foreach ($ln in ($text -split "`r?`n")) {
                if ($ln.Length -eq 0) { continue }
                $color = 'Gray'
                if ($ln -match '\[CRASH\]|\[PANIC\]') { $color = 'Red' }
                elseif ($ln -match '\[diag\]|loaded|armed') { $color = 'Cyan' }
                Write-Host "$prefix $ln" -ForegroundColor $color
            }
        }
        $state[$path] = $len
    } catch {
    } finally {
        if ($fs) { $fs.Dispose() }
    }
}

# --- init ---
$offsets = @{}
foreach ($p in @($ModLog, $SyncLog)) {
    if ($FromStart) { $offsets[$p] = 0 }
    elseif (Test-Path -LiteralPath $p) { $offsets[$p] = (Get-Item -LiteralPath $p).Length }
    else { $offsets[$p] = 0 }
}

$startTime  = Get-Date
$seenDumps  = @{}
foreach ($d in (Get-ChildItem -LiteralPath $DumpDir -Filter '*.dmp' -ErrorAction SilentlyContinue)) {
    $seenDumps[$d.FullName] = $true   # ignore pre-existing dumps
}
$lastEvt    = $startTime
$tracked    = $null
$evtTick    = 0
$aliveTick  = 0

Write-Host "============================================================"
Write-Host "[MON] monitor started $(Now)  proc=$ProcName  poll=${PollMs}ms"
Write-Host "[MON] mod=$ModLog"
Write-Host "[MON] sync=$SyncLog"
Write-Host "[MON] dumps=$DumpDir   events=Application log"
$running = Get-Process -Name $base -ErrorAction SilentlyContinue
Write-Host ("[MON] {0} currently {1}" -f $ProcName, ($(if ($running) { 'RUNNING pid=' + $running[0].Id } else { 'not running' })))
Write-Host "============================================================"

while ($true) {
    # ---- process lifetime ----
    if ($null -eq $tracked) {
        $p = Get-Process -Name $base -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($p) {
            $tracked = $p
            Write-Host "[MON] $ProcName STARTED pid=$($p.Id) at $(Now)" -ForegroundColor Green
        }
    } else {
        try { $tracked.Refresh() } catch {}
        if ($tracked.HasExited) {
            $pid0 = $tracked.Id
            $code = $null
            try { $code = $tracked.ExitCode } catch {}
            # Drain final log lines before reporting the exit.
            Read-NewLines $ModLog  $offsets '[MOD] '
            Read-NewLines $SyncLog $offsets '[SYNC]'
            if ($null -eq $code) {
                Write-Host "[MON] $ProcName EXITED pid=$pid0 code=unknown at $(Now)" -ForegroundColor Yellow
            } elseif ($code -eq 0) {
                Write-Host "[MON] $ProcName EXITED clean pid=$pid0 at $(Now)" -ForegroundColor Green
            } else {
                Write-Host "[MON][CRASH] $ProcName pid=$pid0 exit=$(Decode-Code $code) at $(Now)" -ForegroundColor Red
            }
            $tracked = $null
        }
    }

    # ---- log tails ----
    Read-NewLines $ModLog  $offsets '[MOD] '
    Read-NewLines $SyncLog $offsets '[SYNC]'

    # ---- new minidumps ----
    foreach ($d in (Get-ChildItem -LiteralPath $DumpDir -Filter '*.dmp' -ErrorAction SilentlyContinue)) {
        if (-not $seenDumps[$d.FullName]) {
            $seenDumps[$d.FullName] = $true
            $mb = [math]::Round($d.Length / 1MB, 1)
            Write-Host "[MON][DUMP] $($d.FullName)  (${mb} MB)" -ForegroundColor Magenta
        }
    }

    # ---- OS error events (every ~5s) ----
    $evtTick += $PollMs
    if ($evtTick -ge 5000) {
        $evtTick = 0
        try {
            $events = Get-WinEvent -FilterHashtable @{
                LogName      = 'Application'
                ProviderName = @('Application Error', '.NET Runtime', 'Application Hang',
                                 'Windows Error Reporting')
                StartTime    = $lastEvt
            } -ErrorAction Stop
            foreach ($e in $events) {
                if ($e.Message -match 'DDO') {
                    $first = (($e.Message -split "`r?`n") | Where-Object { $_.Trim() } | Select-Object -First 1)
                    Write-Host "[MON][EVT] $($e.TimeCreated.ToString('HH:mm:ss')) $($e.ProviderName): $($first.Trim())" -ForegroundColor Yellow
                }
            }
        } catch { }
        $lastEvt = Get-Date
    }

    # ---- liveness ping (every ~60s) ----
    $aliveTick += $PollMs
    if ($aliveTick -ge 60000) {
        $aliveTick = 0
        $st = if ($tracked) { "running pid=$($tracked.Id)" } else { 'idle' }
        Write-Host "[MON] watching ($st) $(Now)"
    }

    Start-Sleep -Milliseconds $PollMs
}
