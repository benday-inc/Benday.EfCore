#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Builds the Benday.EfCore solution and copies the NuGet packages to the local NuGet folder.

.DESCRIPTION
    Builds the solution (GeneratePackageOnBuild produces the .nupkg files) and copies the
    resulting .nupkg files for each packable library to a local NuGet feed folder:
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
    "src/Benday.EfCore/Benday.EfCore.csproj",
    "src/Benday.EfCore.SqlServer/Benday.EfCore.SqlServer.csproj",
    "src/Benday.EfCore.Testing/Benday.EfCore.Testing.csproj"
)

Write-Host "Building solution ($Configuration)..."

dotnet build Benday.EfCore.slnx --configuration $Configuration
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed (exit code $LASTEXITCODE)."
}

Write-Host "Copying packages..."

foreach ($project in $projects) {
    $projectPath = Join-Path $root $project

    # verify project exists
    if (-not (Test-Path $projectPath)) {
        throw "Project not found: $projectPath"
    }

    # GeneratePackageOnBuild puts the .nupkg in the project's bin/$Configuration folder.
    $projectDir = Split-Path $projectPath -Parent
    $binDir = Join-Path $projectDir "bin/$Configuration"

    $nupkgs = Get-ChildItem -Path $binDir -Filter "*.nupkg" -ErrorAction SilentlyContinue
    if (-not $nupkgs) {
        throw "No .nupkg files found in $binDir for $project."
    }

    Write-Host ""
    foreach ($nupkg in $nupkgs) {
        Write-Host "Copying $($nupkg.Name) -> $localNuGet"
        Copy-Item -Path $nupkg.FullName -Destination $localNuGet -Force
    }
}

Write-Host ""
Write-Host "Done. Packages in $localNuGet :"
Get-ChildItem -Path $localNuGet -Filter "Benday.EfCore*.nupkg" |
    Sort-Object Name |
    ForEach-Object { Write-Host "  $($_.Name)" }
