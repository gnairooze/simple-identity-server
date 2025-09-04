#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Test script to verify X-Forwarded-For header logging
.DESCRIPTION
    This script tests whether X-Forwarded-For headers are being properly logged
.PARAMETER BaseUrl
    The base URL of the Simple Identity Server API (default: https://localhost:7001)
.EXAMPLE
    .\test-x-forwarded-for.ps1 -BaseUrl "https://localhost:7001"
#>

param(
    [string]$BaseUrl = "https://localhost:7001"
)

Write-Host "Testing X-Forwarded-For Header Logging" -ForegroundColor Green
Write-Host "Base URL: $BaseUrl" -ForegroundColor Yellow
Write-Host ""

# Test 1: Simple request with X-Forwarded-For header
Write-Host "Test 1: Request with X-Forwarded-For header..." -ForegroundColor Cyan
try {
    $headers = @{
        "X-Forwarded-For" = "203.0.113.42, 198.51.100.17"
        "User-Agent" = "TestClient/1.0"
    }
    
    $response = Invoke-RestMethod -Uri "$BaseUrl/home/health" -Method Get -Headers $headers -SkipCertificateCheck
    Write-Host "✓ Request with X-Forwarded-For completed successfully" -ForegroundColor Green
    Write-Host "  X-Forwarded-For sent: 203.0.113.42, 198.51.100.17" -ForegroundColor White
} catch {
    Write-Host "✗ Request failed: $($_.Exception.Message)" -ForegroundColor Red
}

Start-Sleep -Seconds 2

# Test 2: Token request with X-Forwarded-For header
Write-Host "Test 2: Token request with X-Forwarded-For header..." -ForegroundColor Cyan
try {
    $headers = @{
        "X-Forwarded-For" = "192.168.1.100"
        "User-Agent" = "TestClient/1.0"
    }
    
    $response = Invoke-RestMethod -Uri "$BaseUrl/connect/token" -Method Post -ContentType "application/x-www-form-urlencoded" -Body "grant_type=client_credentials&client_id=test-client&client_secret=test-secret" -Headers $headers -SkipCertificateCheck
    Write-Host "✓ Token request with X-Forwarded-For completed successfully" -ForegroundColor Green
    Write-Host "  X-Forwarded-For sent: 192.168.1.100" -ForegroundColor White
} catch {
    Write-Host "✗ Token request failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "Test completed!" -ForegroundColor Green
Write-Host ""
Write-Host "Check the application logs to see if X-Forwarded-For headers were captured:" -ForegroundColor Yellow
Write-Host "1. Look for DebugLoggingMiddleware output with request headers" -ForegroundColor White
Write-Host "2. Look for SecurityMonitoringMiddleware output with IP addresses" -ForegroundColor White
Write-Host "3. Check the SecurityLogs database table for the logged IP addresses" -ForegroundColor White
Write-Host ""
Write-Host "Expected behavior:" -ForegroundColor Yellow
Write-Host "- DebugLoggingMiddleware should NOT see X-Forwarded-For in headers (consumed by UseForwardedHeaders)" -ForegroundColor White
Write-Host "- SecurityMonitoringMiddleware should see the processed IP address" -ForegroundColor White
Write-Host "- If middleware order is wrong, the original X-Forwarded-For value won't be preserved" -ForegroundColor White
