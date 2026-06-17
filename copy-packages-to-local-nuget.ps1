#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Packs the Benday.EfCore NuGet packages and copies them to the local NuGet folder.

.DESCRIPTION
    Runs `dotnet pack` for each packable library in this solution and writes the
    resulting .nupkg files to a local NuGet feed folder:
      - Windows:     C:\LocalNuGet
      - macOS/Linux: ~/LocalNuGet
    The folder is created if it does not already exist.

.PARAMETER Configuration
    Build configuration to pack. Defaults to Release.

.EXAMPLE
    ./copy-packages-to-local-nuget.ps1
    ./copy-packages-to-local-nuget.ps1 -Configuration Debug
#>
[CmdletBinding()]
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

# This script lives in the same directory as the .slnx, so anchor paths to it.
$root = $PSScriptRoot

# Pick the local NuGet folder by OS. $IsWindows is set on PowerShell Core;
# Windows PowerShell (Desktop edition) is always Windows.
$onWindows = $IsWindows -or ($PSVersionTable.PSEdition -eq "Desktop")
if ($onWindows) {
    $localNuGet = "C:\LocalNuGet"
}
else {
    $localNuGet = Join-Path $HOME "LocalNuGet"
}

if (-not (Test-Path $localNuGet)) {
    Write-Host "Creating local NuGet folder: $localNuGet"
    New-Item -ItemType Directory -Path $localNuGet -Force | Out-Null
}

# The packable libraries (test/example projects are not packed).
$projects = @(
    "Benday.EfCore/Benday.EfCore.csproj",
    "Benday.EfCore.SqlServer/Benday.EfCore.SqlServer.csproj",
    "Benday.EfCore.Testing/Benday.EfCore.Testing.csproj"
)

foreach ($project in $projects) {
    $projectPath = Join-Path $root $project
    Write-Host ""
    Write-Host "Packing $project ($Configuration) -> $localNuGet"
    dotnet pack $projectPath --configuration $Configuration --output $localNuGet
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet pack failed for $project (exit code $LASTEXITCODE)."
    }
}

Write-Host ""
Write-Host "Done. Packages in $localNuGet :"
Get-ChildItem -Path $localNuGet -Filter "Benday.EfCore*.nupkg" |
    Sort-Object Name |
    ForEach-Object { Write-Host "  $($_.Name)" }
