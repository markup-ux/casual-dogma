<#
  build.ps1 - Build the 32-bit client-mod artifacts (ddon_mod.dll + inject.exe).

  Usage:
    .\build.ps1            # build both crates (release, i686)
    .\build.ps1 -Mod       # build only ddon_mod.dll
    .\build.ps1 -Inject    # build only inject.exe

  DDO.exe is 32-bit, so everything targets i686-pc-windows-msvc.
#>
[CmdletBinding()]
param(
    [switch]$Mod,
    [switch]$Inject
)

$ErrorActionPreference = 'Stop'
# Corp/AV TLS stacks sometimes can't do cert revocation checks against crates.io.
$env:CARGO_HTTP_CHECK_REVOKE = 'false'

$root   = Split-Path -Parent $MyInvocation.MyCommand.Path
$target = 'i686-pc-windows-msvc'

# Default: build everything.
if (-not $Mod -and -not $Inject) { $Mod = $true; $Inject = $true }

$crates = @()
if ($Mod)    { $crates += 'ddon_mod' }
if ($Inject) { $crates += 'inject' }

foreach ($c in $crates) {
    Write-Host "[build] $c ($target)" -ForegroundColor Cyan
    Push-Location (Join-Path $root $c)
    try {
        cargo build --release --target $target
        if ($LASTEXITCODE -ne 0) { throw "cargo build failed for $c" }
    } finally {
        Pop-Location
    }
}

$dll = Join-Path $root "ddon_mod\target\$target\release\ddon_mod.dll"
$inj = Join-Path $root "inject\target\$target\release\inject.exe"

Write-Host ""
if ($Mod -and (Test-Path $dll))    { Write-Host "[build] DLL: $dll" -ForegroundColor Green }
if ($Inject -and (Test-Path $inj)) { Write-Host "[build] INJ: $inj" -ForegroundColor Green }
