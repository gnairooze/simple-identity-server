# Test script to verify tokens are now encrypted (opaque)
# This script tests the token format change after removing DisableAccessTokenEncryption()

Write-Host "Testing Encrypted Token Configuration" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green

# Test parameters
$identityServerUrl = "https://localhost:7443"
$resourceApiUrl = "https://localhost:7001" 
$clientId = "service-api"
$clientSecret = "your-secret-here"

Write-Host "`n1. Testing token generation..." -ForegroundColor Yellow

# Request a token
$tokenRequest = @{
    grant_type = "client_credentials"
    client_id = $clientId
    client_secret = $clientSecret
    scope = "api1.read"
}

try {
    $tokenResponse = Invoke-RestMethod -Uri "$identityServerUrl/connect/token" -Method Post -Body $tokenRequest -ContentType "application/x-www-form-urlencoded"
    
    $accessToken = $tokenResponse.access_token
    Write-Host "✓ Token generated successfully" -ForegroundColor Green
    Write-Host "Token length: $($accessToken.Length) characters" -ForegroundColor Cyan
    
    # Check if token looks like JWT (3 parts separated by dots) or encrypted (opaque)
    $tokenParts = $accessToken.Split('.')
    if ($tokenParts.Count -eq 3 -and $accessToken.StartsWith("eyJ")) {
        Write-Host "⚠️  Token appears to be JWT format (readable)" -ForegroundColor Red
        Write-Host "   First 50 chars: $($accessToken.Substring(0, [Math]::Min(50, $accessToken.Length)))" -ForegroundColor Yellow
    } else {
        Write-Host "✓ Token appears to be encrypted/opaque format" -ForegroundColor Green
        Write-Host "   First 50 chars: $($accessToken.Substring(0, [Math]::Min(50, $accessToken.Length)))" -ForegroundColor Yellow
    }
    
    Write-Host "`n2. Testing token introspection..." -ForegroundColor Yellow
    
    # Test introspection
    $introspectRequest = @{
        token = $accessToken
        client_id = $clientId
        client_secret = $clientSecret
    }
    
    $introspectResponse = Invoke-RestMethod -Uri "$identityServerUrl/connect/introspect" -Method Post -Body $introspectRequest -ContentType "application/x-www-form-urlencoded"
    
    if ($introspectResponse.active -eq $true) {
        Write-Host "✓ Token introspection successful - token is active" -ForegroundColor Green
    } else {
        Write-Host "✗ Token introspection failed - token is not active" -ForegroundColor Red
    }
    
    Write-Host "`n3. Testing resource API access..." -ForegroundColor Yellow
    
    # Test accessing resource API
    $headers = @{
        "Authorization" = "Bearer $accessToken"
    }
    
    try {
        $resourceResponse = Invoke-RestMethod -Uri "$resourceApiUrl/WeatherForecast" -Method Get -Headers $headers
        Write-Host "✓ Resource API access successful" -ForegroundColor Green
        Write-Host "   Returned $($resourceResponse.Count) weather records" -ForegroundColor Cyan
    } catch {
        Write-Host "✗ Resource API access failed: $($_.Exception.Message)" -ForegroundColor Red
    }
    
} catch {
    Write-Host "✗ Token generation failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=====================================" -ForegroundColor Green
Write-Host "Test completed" -ForegroundColor Green
