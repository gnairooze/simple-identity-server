#!/usr/bin/env pwsh

# Test script to demonstrate debug logging functionality
# This script makes various HTTP requests to test the debug logging middleware

Write-Host "Testing Debug Logging Middleware" -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Green

# Base URL for the API
$baseUrl = "https://localhost:7001"

Write-Host "`n1. Testing GET request to /.well-known/openid-configuration" -ForegroundColor Yellow

try {
    $response = Invoke-WebRequest -Uri "$baseUrl/.well-known/openid-configuration" -UseBasicParsing -SkipCertificateCheck
    Write-Host "   Status: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "   Content-Type: $($response.Headers.'Content-Type')" -ForegroundColor Green
} catch {
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n2. Testing POST request to /connect/token (Token Request)" -ForegroundColor Yellow

$tokenBody = @{
    'grant_type' = 'client_credentials'
    'client_id' = 'test-client'
    'client_secret' = 'test-secret'
    'scope' = 'api1'
}

try {
    $response = Invoke-WebRequest -Uri "$baseUrl/connect/token" -Method POST -Body $tokenBody -UseBasicParsing -SkipCertificateCheck
    Write-Host "   Status: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "   Content-Type: $($response.Headers.'Content-Type')" -ForegroundColor Green
} catch {
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n3. Testing POST request with JSON body" -ForegroundColor Yellow

$jsonBody = @{
    'test' = 'data'
    'number' = 123
    'nested' = @{
        'property' = 'value'
    }
} | ConvertTo-Json

$headers = @{
    'Content-Type' = 'application/json'
    'User-Agent' = 'Debug-Test-Client/1.0'
    'X-Custom-Header' = 'CustomValue123'
}

try {
    $response = Invoke-WebRequest -Uri "$baseUrl/api/WeatherForecast" -Method POST -Body $jsonBody -Headers $headers -UseBasicParsing -SkipCertificateCheck
    Write-Host "   Status: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "   Content-Type: $($response.Headers.'Content-Type')" -ForegroundColor Green
} catch {
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n4. Testing GET request with query parameters" -ForegroundColor Yellow

try {
    $response = Invoke-WebRequest -Uri "$baseUrl/api/WeatherForecast?days=5&includeDetails=true" -UseBasicParsing -SkipCertificateCheck
    Write-Host "   Status: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "   Content-Type: $($response.Headers.'Content-Type')" -ForegroundColor Green
} catch {
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n5. Testing request with Authorization header (will be masked)" -ForegroundColor Yellow

$authHeaders = @{
    'Authorization' = 'Bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiYWRtaW4iOnRydWV9.TJVA95OrM7E2cBab30RMHrHDcEfxjoYZgeFONFh7HgQ'
    'User-Agent' = 'Debug-Test-Client-Auth/1.0'
}

try {
    $response = Invoke-WebRequest -Uri "$baseUrl/api/WeatherForecast" -Headers $authHeaders -UseBasicParsing -SkipCertificateCheck
    Write-Host "   Status: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "   Content-Type: $($response.Headers.'Content-Type')" -ForegroundColor Green
} catch {
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=================================" -ForegroundColor Green
Write-Host "Debug logging test completed!" -ForegroundColor Green
Write-Host "Check the application console output and logs for detailed request/response logging." -ForegroundColor Cyan
Write-Host "All requests and responses should be logged at DEBUG level with full headers and body content." -ForegroundColor Cyan
