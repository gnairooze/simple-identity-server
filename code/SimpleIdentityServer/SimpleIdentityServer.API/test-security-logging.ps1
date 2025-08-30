#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Test script for Serilog Security Logging functionality
.DESCRIPTION
    This script tests the security logging implementation by making requests to trigger various security events
.PARAMETER BaseUrl
    The base URL of the Simple Identity Server API (default: https://localhost:7001)
.PARAMETER RequestCount
    Number of requests to make for rate limiting tests (default: 25)
.EXAMPLE
    .\test-security-logging.ps1 -BaseUrl "https://localhost:7001" -RequestCount 25
#>

param(
    [string]$BaseUrl = "https://localhost:7001",
    [int]$RequestCount = 25
)

Write-Host "Testing Serilog Security Logging Implementation" -ForegroundColor Green
Write-Host "Base URL: $BaseUrl" -ForegroundColor Yellow
Write-Host "Request Count: $RequestCount" -ForegroundColor Yellow
Write-Host ""

# Test 1: Normal token request (should log TOKEN_REQUEST_MONITORED)
Write-Host "Test 1: Normal token request..." -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/connect/token" -Method Post -ContentType "application/x-www-form-urlencoded" -Body "grant_type=client_credentials&client_id=test-client&client_secret=test-secret" -SkipCertificateCheck
    Write-Host "✓ Normal token request completed" -ForegroundColor Green
} catch {
    Write-Host "✗ Normal token request failed: $($_.Exception.Message)" -ForegroundColor Red
}

Start-Sleep -Seconds 2

# Test 2: Multiple rapid token requests (should trigger SUSPICIOUS_TOKEN_FREQUENCY)
Write-Host "Test 2: Rapid token requests to trigger suspicious activity..." -ForegroundColor Cyan
$successCount = 0
$rateLimitCount = 0

for ($i = 1; $i -le $RequestCount; $i++) {
    try {
        $response = Invoke-RestMethod -Uri "$BaseUrl/connect/token" -Method Post -ContentType "application/x-www-form-urlencoded" -Body "grant_type=client_credentials&client_id=test-client-rapid&client_secret=test-secret" -SkipCertificateCheck
        $successCount++
        Write-Host "Request $i : Success" -ForegroundColor Green
    } catch {
        if ($_.Exception.Response.StatusCode -eq 429) {
            $rateLimitCount++
            Write-Host "Request $i : Rate Limited (429)" -ForegroundColor Yellow
        } else {
            Write-Host "Request $i : Failed - $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    
    # Small delay to avoid overwhelming the server
    Start-Sleep -Milliseconds 100
}

Write-Host "✓ Rapid requests test completed: $successCount successful, $rateLimitCount rate limited" -ForegroundColor Green

Start-Sleep -Seconds 2

# Test 3: Introspection requests (should log INTROSPECTION_REQUEST)
Write-Host "Test 3: Token introspection requests..." -ForegroundColor Cyan
try {
    # First get a token
    $tokenResponse = Invoke-RestMethod -Uri "$BaseUrl/connect/token" -Method Post -ContentType "application/x-www-form-urlencoded" -Body "grant_type=client_credentials&client_id=test-client&client_secret=test-secret" -SkipCertificateCheck
    
    if ($tokenResponse.access_token) {
        # Then introspect it
        $introspectResponse = Invoke-RestMethod -Uri "$BaseUrl/connect/introspect" -Method Post -ContentType "application/x-www-form-urlencoded" -Body "token=$($tokenResponse.access_token)&client_id=test-client&client_secret=test-secret" -SkipCertificateCheck
        Write-Host "✓ Token introspection completed" -ForegroundColor Green
    }
} catch {
    Write-Host "✗ Token introspection failed: $($_.Exception.Message)" -ForegroundColor Red
}

Start-Sleep -Seconds 2

# Test 4: Invalid requests (should log REQUEST_EXCEPTION or error events)
Write-Host "Test 4: Invalid requests to test error logging..." -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/connect/token" -Method Post -ContentType "application/x-www-form-urlencoded" -Body "grant_type=invalid_grant&client_id=invalid&client_secret=invalid" -SkipCertificateCheck
} catch {
    Write-Host "✓ Invalid request properly rejected (expected)" -ForegroundColor Green
}

# Test 5: Health check requests (should log REQUEST_COMPLETED)
Write-Host "Test 5: Health check requests..." -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/home/health" -Method Get -SkipCertificateCheck
    Write-Host "✓ Health check completed" -ForegroundColor Green
} catch {
    Write-Host "✗ Health check failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "Security logging tests completed!" -ForegroundColor Green
Write-Host ""
Write-Host "To verify the logs were captured:" -ForegroundColor Yellow
Write-Host "1. Check the application console output for immediate feedback" -ForegroundColor White
Write-Host "2. Query the SecurityLogs table in the SimpleIdentityServerSecurityLogs database:" -ForegroundColor White
Write-Host ""
Write-Host "   SELECT TOP 50 TimeStamp, EventType, IpAddress, ClientId, Path, Method, StatusCode, DurationMs" -ForegroundColor Gray
Write-Host "   FROM SecurityLogs" -ForegroundColor Gray  
Write-Host "   ORDER BY TimeStamp DESC;" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Check for specific event types:" -ForegroundColor White
Write-Host ""
Write-Host "   SELECT EventType, COUNT(*) as Count, MAX(TimeStamp) as LastOccurrence" -ForegroundColor Gray
Write-Host "   FROM SecurityLogs" -ForegroundColor Gray
Write-Host "   WHERE TimeStamp >= DATEADD(minute, -10, GETUTCDATE())" -ForegroundColor Gray
Write-Host "   GROUP BY EventType;" -ForegroundColor Gray
Write-Host ""
Write-Host "Expected events from this test:" -ForegroundColor Yellow
Write-Host "- TOKEN_REQUEST_MONITORED (multiple)" -ForegroundColor White
Write-Host "- SUSPICIOUS_TOKEN_FREQUENCY (if >10 requests in 5 min)" -ForegroundColor White
Write-Host "- INTROSPECTION_REQUEST" -ForegroundColor White
Write-Host "- REQUEST_COMPLETED (multiple)" -ForegroundColor White
Write-Host "- REQUEST_EXCEPTION (for invalid requests)" -ForegroundColor White
