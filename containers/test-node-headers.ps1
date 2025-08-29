#!/usr/bin/env pwsh
# Test script to verify load balancer node identification headers

Write-Host "Testing Load Balancer Node Identification Headers" -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Green

# Test endpoints
$endpoints = @(
    "http://localhost/health",
    "https://localhost/health",
    "http://localhost/home/health"
)

# Function to test an endpoint and show node headers
function Test-NodeHeaders {
    param(
        [string]$Url,
        [int]$RequestCount = 5
    )
    
    Write-Host "`nTesting endpoint: $Url" -ForegroundColor Yellow
    Write-Host "Making $RequestCount requests to see load balancing in action..." -ForegroundColor Cyan
    
    for ($i = 1; $i -le $RequestCount; $i++) {
        try {
            Write-Host "`nRequest $i:" -ForegroundColor White
            
            # Make request and capture headers
            $response = Invoke-WebRequest -Uri $Url -Method GET -SkipCertificateCheck -ErrorAction Stop
            
            # Look for our node identification header
            $nodeHeader = $response.Headers['X-Served-By']
            if ($nodeHeader) {
                Write-Host "  ✅ X-Served-By: $nodeHeader" -ForegroundColor Green
            } else {
                Write-Host "  ❌ X-Served-By header not found" -ForegroundColor Red
            }
            
            # Show status code
            Write-Host "  Status: $($response.StatusCode)" -ForegroundColor Gray
            
            # Show other relevant headers
            $upstreamHeaders = $response.Headers.Keys | Where-Object { $_ -match "upstream|server|node" }
            foreach ($header in $upstreamHeaders) {
                Write-Host "  $header: $($response.Headers[$header])" -ForegroundColor Gray
            }
            
        } catch {
            Write-Host "  ❌ Request failed: $($_.Exception.Message)" -ForegroundColor Red
        }
        
        # Small delay between requests
        Start-Sleep -Milliseconds 500
    }
}

# Test each endpoint
foreach ($endpoint in $endpoints) {
    Test-NodeHeaders -Url $endpoint -RequestCount 6
}

Write-Host "`n=== Summary ===" -ForegroundColor Green
Write-Host "If the load balancer is working correctly, you should see:" -ForegroundColor White
Write-Host "- X-Served-By headers with values: api-instance-1, api-instance-2, api-instance-3" -ForegroundColor White
Write-Host "- Different node names across multiple requests (load balancing in action)" -ForegroundColor White
Write-Host "- Consistent responses from the health endpoints" -ForegroundColor White

Write-Host "`nTo restart the services with new configuration:" -ForegroundColor Yellow
Write-Host "docker-compose down && docker-compose up -d" -ForegroundColor Cyan
