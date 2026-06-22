# PowerShell script to start all local development dependencies needed to run
# the integration tests.
# Usage: .\start-local-dev.ps1 [-Remove] [-Pull]

param(
    [switch]$Remove,
    [switch]$Pull
)

Write-Host "=== Starting local development environment ==="
Write-Host

Write-Host "--- SQL Server ---"
& "$PSScriptRoot/start-sql-server-container.ps1" -Remove:$Remove -Pull:$Pull

Write-Host
Write-Host "=== Local development environment is ready ==="
