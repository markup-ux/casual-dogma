<#
uninstall_applier_task.ps1 - Remove the DDON level-sync applier scheduled task and stop the process.
Run from an elevated PowerShell:
    powershell -ExecutionPolicy Bypass -File uninstall_applier_task.ps1
#>
$ErrorActionPreference = "SilentlyContinue"
$TaskName = "DDONLevelSyncApplier"

Stop-ScheduledTask -TaskName $TaskName
Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false

# Best-effort stop of any running applier instance.
Get-CimInstance Win32_Process -Filter "Name='pythonw.exe' OR Name='python.exe'" |
    Where-Object { $_.CommandLine -like "*apply_sync.py*" } |
    ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }

Write-Host "Removed scheduled task '$TaskName' and stopped any running applier."
