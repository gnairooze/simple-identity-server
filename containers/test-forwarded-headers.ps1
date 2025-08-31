#!/usr/bin/env pwsh

# Test script to verify X-Forwarded-For headers are properly passed through the load balancer
# This script tests both the nginx load balancer and direct API instance access

Write-Host "=== Testing X-Forwarded-For Header Propagation ===" -ForegroundColor Green
Write-Host ""

# Function to make HTTP request and display headers
function Test-ForwardedHeaders {
    param(
        [string]$Url,
        [string]$Description,
        [hashtable]$Headers = @{}
    )
    
    Write-Host "Testing: $Description" -ForegroundColor Yellow
    Write-Host "URL: $Url" -ForegroundColor Gray
    
    try {
        # Add custom headers to simulate forwarded requests
        $requestHeaders = @{
            'User-Agent' = 'ForwardedHeaderTest/1.0'
        }
        
        # Add any additional headers
        foreach ($key in $Headers.Keys) {
            $requestHeaders[$key] = $Headers[$key]
        }
        
        # Make the request
        $response = Invoke-WebRequest -Uri $Url -Headers $requestHeaders -UseBasicParsing -SkipCertificateCheck -TimeoutSec 10
        
        Write-Host "✓ Response Status: $($response.StatusCode)" -ForegroundColor Green
        
        # Display response headers that show node information
        if ($response.Headers.ContainsKey('X-Served-By')) {
            Write-Host "✓ Served by: $($response.Headers['X-Served-By'])" -ForegroundColor Green
        }
        
        # Try to extract any debug information from the response
        $content = $response.Content
        if ($content -match "client.*ip|remote.*ip|forwarded" -or $content.Length -lt 1000) {
            Write-Host "Response content (first 500 chars):" -ForegroundColor Gray
            Write-Host ($content.Substring(0, [Math]::Min(500, $content.Length))) -ForegroundColor Gray
        }
        
    }
    catch {
        Write-Host "✗ Error: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    Write-Host ""
}

# Test 1: Direct load balancer access (should add X-Forwarded-For)
Write-Host "1. Testing Load Balancer (should add X-Forwarded-For header)" -ForegroundColor Cyan
Test-ForwardedHeaders -Url "https://identity.dev.test/health" -Description "Load Balancer Health Check"

# Test 2: Load balancer with existing X-Forwarded-For (should append)
Write-Host "2. Testing Load Balancer with existing X-Forwarded-For (should append)" -ForegroundColor Cyan
Test-ForwardedHeaders -Url "https://identity.dev.test/health" -Description "Load Balancer with existing XFF" -Headers @{
    'X-Forwarded-For' = '192.168.1.100'
}

# Test 3: Direct API instance access (no load balancer)
Write-Host "3. Testing Direct API Instance Access (bypassing load balancer)" -ForegroundColor Cyan
Test-ForwardedHeaders -Url "https://localhost:8081/home/health" -Description "Direct API Instance 1"
Test-ForwardedHeaders -Url "https://localhost:8082/home/health" -Description "Direct API Instance 2"
Test-ForwardedHeaders -Url "https://localhost:8083/home/health" -Description "Direct API Instance 3"

# Test 4: Test token endpoint through load balancer
Write-Host "4. Testing Token Endpoint through Load Balancer" -ForegroundColor Cyan
try {
    $tokenHeaders = @{
        'Content-Type' = 'application/x-www-form-urlencoded'
        'X-Forwarded-For' = '203.0.113.195'  # Example external IP
    }
    
    $body = @{
        'grant_type' = 'client_credentials'
        'client_id' = 'test-client'
        'client_secret' = 'test-secret'
        'scope' = 'api'
    }
    
    $response = Invoke-WebRequest -Uri "https://identity.dev.test/connect/token" -Method POST -Headers $tokenHeaders -Body $body -UseBasicParsing -SkipCertificateCheck -TimeoutSec 10
    Write-Host "✓ Token endpoint accessible through load balancer: $($response.StatusCode)" -ForegroundColor Green
    
    if ($response.Headers.ContainsKey('X-Served-By')) {
        Write-Host "✓ Token request served by: $($response.Headers['X-Served-By'])" -ForegroundColor Green
    }
}
catch {
    Write-Host "Token endpoint test (expected to fail with invalid credentials): $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== Nginx Access Log Analysis ===" -ForegroundColor Green
Write-Host "To verify X-Forwarded-For headers are being processed, check the nginx logs:"
Write-Host "docker logs simple-identity-server-lb | grep xff=" -ForegroundColor Gray
Write-Host ""
Write-Host "Expected log format should show:"
Write-Host "xff=\"original_client_ip, load_balancer_ip\" - indicating header chaining" -ForegroundColor Gray
Write-Host ""

Write-Host "=== API Instance Log Analysis ===" -ForegroundColor Green
Write-Host "To verify API instances receive forwarded headers, check their logs:"
Write-Host "docker logs simple-identity-server-api-1 | grep -i 'forwarded\|xff\|remote'" -ForegroundColor Gray
Write-Host "docker logs simple-identity-server-api-2 | grep -i 'forwarded\|xff\|remote'" -ForegroundColor Gray
Write-Host "docker logs simple-identity-server-api-3 | grep -i 'forwarded\|xff\|remote'" -ForegroundColor Gray
Write-Host ""

Write-Host "=== Test Complete ===" -ForegroundColor Green
