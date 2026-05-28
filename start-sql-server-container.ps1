# PowerShell script to start a SQL Server container in Docker for local
# development and integration testing.
# Usage: .\start-sql-server-container.ps1 [-Remove] [-Pull]

param(
    [switch]$Remove,
    [switch]$Pull
)

$containerName = "sql_server"
$image = "mcr.microsoft.com/mssql/server:2022-latest"

if ($Remove) {
    Write-Host "Stopping, killing, and removing any existing '$containerName' container..."
    docker stop $containerName 2>$null
    docker kill $containerName 2>$null
    docker rm $containerName 2>$null
}

if ($Pull) {
    Write-Host "Pulling $image..."
    docker pull $image
}

# Start the SQL Server container
Write-Host "Starting SQL Server container '$containerName'..."
docker run --platform linux/amd64 -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=Pa$$word' -p 1433:1433 --name $containerName -d $image

# Show exposed ports
Write-Host "Exposed ports for '$containerName' container:"
docker port $containerName

Write-Host ""
Write-Host "Integration tests use this connection string:"
Write-Host '  Server=localhost; Database=benday-efcore-sqlserver; User Id=sa; Password=Pa$$word; TrustServerCertificate=True'
Write-Host "(The database and schema are created automatically via EF Core migrations on first test run.)"
