# Runs automated supply-cache verification tests.
# Exit code 0 = safe to treat supply-cache registration logic as verified.
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$testProject = Join-Path $PSScriptRoot "Arrowgene.Ddon.Test\Arrowgene.Ddon.Test.csproj"

Write-Host "Running supply cache verification tests..."
dotnet test $testProject -c $Configuration --filter "FullyQualifiedName~SupplyCache" --logger "console;verbosity=normal"
exit $LASTEXITCODE
