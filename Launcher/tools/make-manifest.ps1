<#
.SYNOPSIS
    Generates a patch manifest.json for the Casual Dogma launcher.

.DESCRIPTION
    Walks a finished client/install folder, computes SHA-256 + size for every file,
    and writes a manifest.json that the launcher reads from PatchManifestUrl.

    Host the same folder (and the manifest) on a static web server, then point the
    launcher's "PatchManifestUrl" at the manifest URL and "BaseUrl" at the file root.

.EXAMPLE
    .\make-manifest.ps1 -SourceDir "D:\DDON\Client\Dragon's Dogma Online" `
                        -BaseUrl "https://cdn.casualdogma.example/client/" `
                        -Version "1.0.0" `
                        -OutFile "D:\DDON\dist\manifest.json"
#>
param(
    [Parameter(Mandatory = $true)] [string] $SourceDir,
    [Parameter(Mandatory = $true)] [string] $BaseUrl,
    [string] $Version = (Get-Date -Format "yyyy.MM.dd.HHmm"),
    [string] $OutFile = "manifest.json",
    # Files the launcher manages locally or that shouldn't be force-synced.
    [string[]] $Exclude = @("config.ini", "DDO_Launcher.ini", "temp/*", "*.log")
)

if (-not (Test-Path -LiteralPath $SourceDir)) {
    throw "SourceDir not found: $SourceDir"
}

$root = (Resolve-Path -LiteralPath $SourceDir).Path.TrimEnd('\', '/')
Write-Host "Scanning $root ..." -ForegroundColor Cyan

function Is-Excluded([string] $relPath) {
    foreach ($pat in $Exclude) {
        if ($relPath -like $pat) { return $true }
        if ($relPath -like ($pat -replace '/', '\')) { return $true }
    }
    return $false
}

$files = @()
$all = Get-ChildItem -LiteralPath $root -Recurse -File -Force
$i = 0
foreach ($f in $all) {
    $i++
    $rel = $f.FullName.Substring($root.Length + 1) -replace '\\', '/'
    if (Is-Excluded $rel) { continue }

    Write-Progress -Activity "Hashing files" -Status $rel -PercentComplete (($i / $all.Count) * 100)
    $hash = (Get-FileHash -LiteralPath $f.FullName -Algorithm SHA256).Hash.ToLower()
    $files += [ordered]@{
        path   = $rel
        size   = $f.Length
        sha256 = $hash
    }
}
Write-Progress -Activity "Hashing files" -Completed

if (-not $BaseUrl.EndsWith("/")) { $BaseUrl += "/" }

$manifest = [ordered]@{
    version = $Version
    baseUrl = $BaseUrl
    files   = $files
}

$outDir = Split-Path -Parent $OutFile
if ($outDir -and -not (Test-Path -LiteralPath $outDir)) {
    New-Item -ItemType Directory -Path $outDir -Force | Out-Null
}

$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $OutFile -Encoding UTF8

$totalBytes = 0L
foreach ($entry in $files) { $totalBytes += [int64] $entry.size }
$totalMb = [math]::Round($totalBytes / 1MB, 1)
Write-Host "Wrote $OutFile" -ForegroundColor Green
Write-Host "  version : $Version"
Write-Host "  files   : $($files.Count)"
Write-Host "  total   : $totalMb MB"
Write-Host ""
Write-Host "Next: host the client folder + manifest, then set in launcher.config.json:" -ForegroundColor Yellow
Write-Host "  `"PatchManifestUrl`": `"$BaseUrl`manifest.json`""
