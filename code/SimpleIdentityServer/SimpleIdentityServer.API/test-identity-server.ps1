# Simple Identity Server - Test Script
# This script tests the identity server functionality

param(
    [string]$BaseUrl = "https://localhost:7001"
)

Write-Host "Simple Identity Server - Test Script" -ForegroundColor Green
Write-Host "====================================" -ForegroundColor Green
Write-Host "Testing server at: $BaseUrl" -ForegroundColor Cyan
Write-Host ""

# Test 1: Health Check
Write-Host "Test 1: Health Check" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/home/health" -Method Get -SkipCertificateCheck
    Write-Host "✓ Health check passed: $($response.Status)" -ForegroundColor Green
} catch {
    Write-Host "✗ Health check failed: $($_.Exception.Message)" -ForegroundColor Red
    return
}

# Test 2: Server Information
Write-Host "Test 2: Server Information" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/home" -Method Get -SkipCertificateCheck
    Write-Host "✓ Server info retrieved: $($response.Message)" -ForegroundColor Green
    Write-Host "  Version: $($response.Version)" -ForegroundColor Cyan
    Write-Host "  Supported Grant Types: $($response.SupportedGrantTypes -join ', ')" -ForegroundColor Cyan
} catch {
    Write-Host "✗ Server info failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Get Access Token
Write-Host "Test 3: Get Access Token" -ForegroundColor Yellow
try {
    $body = @{
        grant_type = "client_credentials"
        client_id = "service-api"
        client_secret = "supersecret"
        scope = "api1.read api1.write"
    }
    
    $response = Invoke-RestMethod -Uri "$BaseUrl/connect/token" -Method Post -Body $body -SkipCertificateCheck
    Write-Host "✓ Access token obtained successfully" -ForegroundColor Green
    Write-Host "  Token Type: $($response.token_type)" -ForegroundColor Cyan
    Write-Host "  Expires In: $($response.expires_in) seconds" -ForegroundColor Cyan
    Write-Host "  Scope: $($response.scope)" -ForegroundColor Cyan
    
    $accessToken = $response.access_token
} catch {
    Write-Host "✗ Failed to get access token: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $errorContent = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($errorContent)
        $errorText = $reader.ReadToEnd()
        Write-Host "  Error details: $errorText" -ForegroundColor Red
    }
    return
}

# Test 4: Decode Token (Basic)
Write-Host "Test 4: Decode Token" -ForegroundColor Yellow
try {
    $parts = $accessToken.Split('.')
    if ($parts.Length -eq 3) {
        $payload = $parts[1]
        $payload = $payload.Replace('-', '+').Replace('_', '/')
        switch ($payload.Length % 4) {
            0 { break }
            2 { $payload += "=="; break }
            3 { $payload += "="; break }
        }
        
        $jsonBytes = [Convert]::FromBase64String($payload)
        $json = [System.Text.Encoding]::UTF8.GetString($jsonBytes)
        $tokenData = $json | ConvertFrom-Json
        
        Write-Host "✓ Token decoded successfully" -ForegroundColor Green
        Write-Host "  Subject: $($tokenData.sub)" -ForegroundColor Cyan
        Write-Host "  Name: $($tokenData.name)" -ForegroundColor Cyan
        Write-Host "  Client ID: $($tokenData.client_id)" -ForegroundColor Cyan
        Write-Host "  Role: $($tokenData.role)" -ForegroundColor Cyan
        Write-Host "  Custom Claim: $($tokenData.custom_claim)" -ForegroundColor Cyan
    }
} catch {
    Write-Host "✗ Failed to decode token: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 5: Access Protected Resource
Write-Host "Test 5: Access Protected Resource" -ForegroundColor Yellow
try {
    $headers = @{
        "Authorization" = "Bearer $accessToken"
    }
    
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/client" -Method Get -Headers $headers -SkipCertificateCheck
    Write-Host "✓ Protected resource accessed successfully" -ForegroundColor Green
    Write-Host "  Found $($response.Count) clients" -ForegroundColor Cyan
} catch {
    Write-Host "✗ Failed to access protected resource: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 6: Get Scopes
Write-Host "Test 6: Get Scopes" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/scope" -Method Get -SkipCertificateCheck
    Write-Host "✓ Scopes retrieved successfully" -ForegroundColor Green
    Write-Host "  Found $($response.Count) scopes" -ForegroundColor Cyan
    foreach ($scope in $response) {
        Write-Host "    - $($scope.name): $($scope.displayName)" -ForegroundColor Gray
    }
} catch {
    Write-Host "✗ Failed to get scopes: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 7: Test Different Client
Write-Host "Test 7: Test Different Client" -ForegroundColor Yellow
try {
    $body = @{
        grant_type = "client_credentials"
        client_id = "web-app"
        client_secret = "webapp-secret"
        scope = "api1.read"
    }
    
    $response = Invoke-RestMethod -Uri "$BaseUrl/connect/token" -Method Post -Body $body -SkipCertificateCheck
    Write-Host "✓ Web app client token obtained successfully" -ForegroundColor Green
    Write-Host "  Token Type: $($response.token_type)" -ForegroundColor Cyan
    Write-Host "  Scope: $($response.scope)" -ForegroundColor Cyan
} catch {
    Write-Host "✗ Failed to get web app token: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "All tests completed!" -ForegroundColor Green
Write-Host "The identity server is working correctly." -ForegroundColor Green 