# Production Simulation Environment Setup Script
# This script configures your local environment to simulate production settings

Write-Host "Setting up Production Simulation Environment..." -ForegroundColor Green

# Set environment to Production
$env:ASPNETCORE_ENVIRONMENT = "Production"
Write-Host "✓ Set ASPNETCORE_ENVIRONMENT to Production" -ForegroundColor Green

# Certificate password for production simulation
$env:SIMPLE_IDENTITY_SERVER_CERT_PASSWORD = "DevProductionSimulation123!"
Write-Host "✓ Set certificate password" -ForegroundColor Green

# Database connections - using separate databases for production simulation
$env:SIMPLE_IDENTITY_SERVER_DEFAULT_CONNECTION_STRING = "Server=localhost;Database=SimpleIdentityServer_ProdSim;Integrated Security=true;TrustServerCertificate=true;MultipleActiveResultSets=true"
Write-Host "✓ Set main database connection (SimpleIdentityServer_ProdSim)" -ForegroundColor Green

$env:SIMPLE_IDENTITY_SERVER_SECURITY_LOGS_CONNECTION_STRING = "Server=localhost;Database=SimpleIdentityServer_SecurityLogs_ProdSim;Integrated Security=true;TrustServerCertificate=true;MultipleActiveResultSets=true"
Write-Host "✓ Set security logs database connection (SimpleIdentityServer_SecurityLogs_ProdSim)" -ForegroundColor Green

# CORS origins for local development
$env:SIMPLE_IDENTITY_SERVER_CORS_ALLOWED_ORIGINS = "http://localhost:3000;https://localhost:3000;http://localhost:5000;https://localhost:5001"
Write-Host "✓ Set CORS allowed origins for local development" -ForegroundColor Green

# Optional: Node name for load balancing simulation
$env:SIMPLE_IDENTITY_SERVER_NODE_NAME = "LocalProductionSim-Node1"
Write-Host "✓ Set node name for load balancing simulation" -ForegroundColor Green

# IMPORTANT: Clear any Kestrel certificate configuration from Docker environment
# These variables cause the PEM certificate error when running locally
Remove-Item Env:Kestrel__Certificates__Default__Path -ErrorAction SilentlyContinue
Remove-Item Env:Kestrel__Certificates__Default__KeyPath -ErrorAction SilentlyContinue
Write-Host "✓ Cleared Docker Kestrel certificate configuration" -ForegroundColor Green

# Set HTTP-only URLs for local development (no HTTPS certificate needed)
$env:ASPNETCORE_URLS = "http://localhost:5000"
Write-Host "✓ Set application URLs to HTTP-only for local development" -ForegroundColor Green

Write-Host "`nProduction Simulation Environment Setup Complete!" -ForegroundColor Cyan
Write-Host "Current Environment Variables:" -ForegroundColor Yellow
Write-Host "  ASPNETCORE_ENVIRONMENT: $env:ASPNETCORE_ENVIRONMENT"
Write-Host "  Certificate Password: [SET]"
Write-Host "  Main Database: SimpleIdentityServer_ProdSim"
Write-Host "  Security Logs Database: SimpleIdentityServer_SecurityLogs_ProdSim"
Write-Host "  CORS Origins: $env:SIMPLE_IDENTITY_SERVER_CORS_ALLOWED_ORIGINS"
Write-Host "  Node Name: $env:SIMPLE_IDENTITY_SERVER_NODE_NAME"

Write-Host "`nNext steps:" -ForegroundColor Cyan
Write-Host "1. Create certificate directory: mkdir certs"
Write-Host "2. Run the application: dotnet run"
Write-Host "3. The application will auto-create certificates and databases"

Write-Host "`nNote: This uses separate 'ProdSim' databases to avoid conflicts with development data." -ForegroundColor Yellow
