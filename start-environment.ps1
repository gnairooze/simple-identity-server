# Start SimpleIdentityServer with Load Balancer
# This script starts the complete Docker environment for testing

Write-Host "Starting SimpleIdentityServer Load Balanced Environment..." -ForegroundColor Green

# Check if Docker is running
try {
    docker version | Out-Null
} catch {
    Write-Host "Error: Docker is not running. Please start Docker Desktop." -ForegroundColor Red
    exit 1
}

# Stop any existing containers
Write-Host "Stopping existing containers..." -ForegroundColor Yellow
docker-compose down -v

# Build and start services
Write-Host "Building and starting services..." -ForegroundColor Yellow
docker-compose up --build -d

# Wait for services to be ready
Write-Host "Waiting for services to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

# Check service health
Write-Host "Checking service health..." -ForegroundColor Yellow

$services = @(
    @{Name="Load Balancer"; Url="http://localhost/health"},
    @{Name="SQL Server"; Container="simple-identity-server-db"},
    @{Name="API Instance 1"; Container="simple-identity-server-api-1"},
    @{Name="API Instance 2"; Container="simple-identity-server-api-2"},
    @{Name="API Instance 3"; Container="simple-identity-server-api-3"}
)

foreach ($service in $services) {
    if ($service.Url) {
        try {
            $response = Invoke-WebRequest -Uri $service.Url -TimeoutSec 10
            if ($response.StatusCode -eq 200) {
                Write-Host "✓ $($service.Name) is healthy" -ForegroundColor Green
            } else {
                Write-Host "✗ $($service.Name) returned status $($response.StatusCode)" -ForegroundColor Red
            }
        } catch {
            Write-Host "✗ $($service.Name) is not responding" -ForegroundColor Red
        }
    } elseif ($service.Container) {
        $status = docker inspect -f '{{.State.Status}}' $service.Container 2>$null
        if ($status -eq "running") {
            Write-Host "✓ $($service.Name) container is running" -ForegroundColor Green
        } else {
            Write-Host "✗ $($service.Name) container is $status" -ForegroundColor Red
        }
    }
}

Write-Host ""
Write-Host "Environment started successfully!" -ForegroundColor Green
Write-Host "Access points:" -ForegroundColor Cyan
Write-Host "  Load Balancer: http://localhost" -ForegroundColor White
Write-Host "  OpenID Configuration: http://localhost/.well-known/openid-configuration" -ForegroundColor White
Write-Host "  Token Endpoint: http://localhost/connect/token" -ForegroundColor White
Write-Host "  Introspection Endpoint: http://localhost/connect/introspect" -ForegroundColor White
Write-Host ""
Write-Host "Direct API access (for testing):" -ForegroundColor Cyan
Write-Host "  API Instance 1: http://localhost:8081" -ForegroundColor White
Write-Host "  API Instance 2: http://localhost:8082" -ForegroundColor White
Write-Host "  API Instance 3: http://localhost:8083" -ForegroundColor White
Write-Host ""
Write-Host "Database:" -ForegroundColor Cyan
Write-Host "  SQL Server: localhost:1433" -ForegroundColor White
Write-Host "  Username: sa" -ForegroundColor White
Write-Host "  Password: StrongPassword123!" -ForegroundColor White
Write-Host ""
Write-Host "Use 'stop-environment.ps1' to stop all services" -ForegroundColor Yellow
