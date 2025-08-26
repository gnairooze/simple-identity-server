# Stop SimpleIdentityServer Load Balanced Environment
# This script stops all Docker containers and cleans up

Write-Host "Stopping SimpleIdentityServer Load Balanced Environment..." -ForegroundColor Yellow

# Stop and remove containers, networks, and volumes
docker-compose down -v

Write-Host "Environment stopped and cleaned up successfully!" -ForegroundColor Green
Write-Host "All containers, networks, and volumes have been removed." -ForegroundColor White
