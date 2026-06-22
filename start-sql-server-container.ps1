# PowerShell script to start SQL Server container in Docker
# Usage: .\start-sql-server-container.ps1 [-Remove] [-Pull] [-Refresh]
#                                         [-SaPassword <password>] [-Port <port>]

param(
    [switch]$Remove,
    [switch]$Pull,
    [switch]$Refresh,
    # SA login password. Must meet the SQL Server password policy: at least 8
    # characters with 3 of { uppercase, lowercase, digits, symbols }.
    [string]$SaPassword = 'Pa$$word',
    # Host port mapped to the container's SQL Server port (1433).
    [int]$Port = 1433
)

if ($Refresh) {
    $Remove = $true
    $Pull = $true
}

$imageName = "mcr.microsoft.com/mssql/server:2025-latest"
$containerName = "sql_server"

if ($Remove) {
    Write-Host "Stopping, killing, and removing any existing '$containerName' container..."
    docker stop $containerName 2>$null
    docker kill $containerName 2>$null
    docker rm $containerName 2>$null
}

if ($Pull) {
    Write-Host "Pulling the latest SQL Server Docker image..."
    docker pull $imageName
}

# Start the SQL Server container
Write-Host "Starting SQL Server container..."

# docker options (go BEFORE the image name)
$dockerOptions = @(
    "--platform", "linux/amd64"
    "--name", $containerName
    "--hostname", $containerName
    "--detach"
    "--publish", "${Port}:1433"
    "--env", "ACCEPT_EULA=Y"
    # MSSQL_SA_PASSWORD replaces the deprecated SA_PASSWORD env var.
    "--env", "MSSQL_SA_PASSWORD=$SaPassword"
)

Write-Host "Running: docker run $($dockerOptions -join ' ') $imageName"
docker run @dockerOptions $imageName

# Show exposed ports
Write-Host "Exposed ports for '$containerName' container:"
docker port $containerName
