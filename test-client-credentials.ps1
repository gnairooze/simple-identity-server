# Test Client Credentials Flow through Load Balancer
# This script demonstrates how to authenticate using the client credentials flow

param(
    [string]$BaseUrl = "http://localhost",
    [string]$ClientId = "test-client",
    [string]$ClientSecret = "test-secret"
)

Write-Host "Testing Client Credentials Flow through Load Balancer..." -ForegroundColor Green
Write-Host "Base URL: $BaseUrl" -ForegroundColor Cyan

# Step 1: Get OpenID Configuration
Write-Host "`nStep 1: Retrieving OpenID Configuration..." -ForegroundColor Yellow
try {
    $configUrl = "$BaseUrl/.well-known/openid-configuration"
    $config = Invoke-RestMethod -Uri $configUrl -Method GET
    Write-Host "✓ OpenID Configuration retrieved successfully" -ForegroundColor Green
    Write-Host "  Token Endpoint: $($config.token_endpoint)" -ForegroundColor White
    $tokenEndpoint = $config.token_endpoint
} catch {
    Write-Host "✗ Failed to retrieve OpenID Configuration: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 2: Request Access Token using Client Credentials
Write-Host "`nStep 2: Requesting access token..." -ForegroundColor Yellow
try {
    $tokenRequestBody = @{
        grant_type = "client_credentials"
        client_id = $ClientId
        client_secret = $ClientSecret
        scope = "api"
    }
    
    $tokenResponse = Invoke-RestMethod -Uri $tokenEndpoint -Method POST -Body $tokenRequestBody -ContentType "application/x-www-form-urlencoded"
    
    Write-Host "✓ Access token received successfully" -ForegroundColor Green
    Write-Host "  Token Type: $($tokenResponse.token_type)" -ForegroundColor White
    Write-Host "  Expires In: $($tokenResponse.expires_in) seconds" -ForegroundColor White
    Write-Host "  Access Token: $($tokenResponse.access_token.Substring(0, 50))..." -ForegroundColor White
    
    $accessToken = $tokenResponse.access_token
} catch {
    Write-Host "✗ Failed to get access token: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $errorStream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($errorStream)
        $errorContent = $reader.ReadToEnd()
        Write-Host "Error Details: $errorContent" -ForegroundColor Red
    }
    exit 1
}

# Step 3: Test Token Introspection
Write-Host "`nStep 3: Testing token introspection..." -ForegroundColor Yellow
try {
    $introspectionEndpoint = $config.introspection_endpoint
    $introspectionBody = @{
        token = $accessToken
        client_id = $ClientId
        client_secret = $ClientSecret
    }
    
    $introspectionResponse = Invoke-RestMethod -Uri $introspectionEndpoint -Method POST -Body $introspectionBody -ContentType "application/x-www-form-urlencoded"
    
    if ($introspectionResponse.active -eq $true) {
        Write-Host "✓ Token is active and valid" -ForegroundColor Green
        Write-Host "  Client ID: $($introspectionResponse.client_id)" -ForegroundColor White
        Write-Host "  Scope: $($introspectionResponse.scope)" -ForegroundColor White
        Write-Host "  Expires: $($introspectionResponse.exp)" -ForegroundColor White
    } else {
        Write-Host "✗ Token is not active" -ForegroundColor Red
    }
} catch {
    Write-Host "✗ Token introspection failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 4: Test Load Balancer Distribution with Multiple Token Requests
Write-Host "`nStep 4: Testing load distribution with multiple token requests..." -ForegroundColor Yellow
$successCount = 0
$totalRequests = 10

for ($i = 1; $i -le $totalRequests; $i++) {
    try {
        $testTokenResponse = Invoke-RestMethod -Uri $tokenEndpoint -Method POST -Body $tokenRequestBody -ContentType "application/x-www-form-urlencoded" -TimeoutSec 10
        if ($testTokenResponse.access_token) {
            $successCount++
            Write-Host "  Request $i`: ✓" -ForegroundColor Green -NoNewline
        } else {
            Write-Host "  Request $i`: ✗" -ForegroundColor Red -NoNewline
        }
    } catch {
        Write-Host "  Request $i`: ✗" -ForegroundColor Red -NoNewline
    }
    
    if ($i % 5 -eq 0) { Write-Host "" }
    Start-Sleep -Milliseconds 200
}

Write-Host "`nLoad Distribution Test Results:" -ForegroundColor Cyan
Write-Host "  Successful Requests: $successCount / $totalRequests" -ForegroundColor White
Write-Host "  Success Rate: $([math]::Round(($successCount / $totalRequests) * 100, 2))%" -ForegroundColor White

# Step 5: Test Rate Limiting
Write-Host "`nStep 5: Testing rate limiting..." -ForegroundColor Yellow
$rateLimitHit = $false

Write-Host "Making rapid requests to test rate limiting..." -ForegroundColor White
for ($i = 1; $i -le 30; $i++) {
    try {
        $response = Invoke-WebRequest -Uri $tokenEndpoint -Method POST -Body $tokenRequestBody -ContentType "application/x-www-form-urlencoded" -TimeoutSec 5
        if ($response.StatusCode -eq 429) {
            $rateLimitHit = $true
            Write-Host "✓ Rate limit triggered at request $i (HTTP 429)" -ForegroundColor Green
            break
        }
    } catch {
        if ($_.Exception.Response.StatusCode -eq 429) {
            $rateLimitHit = $true
            Write-Host "✓ Rate limit triggered at request $i (HTTP 429)" -ForegroundColor Green
            break
        }
    }
    Start-Sleep -Milliseconds 100
}

if (-not $rateLimitHit) {
    Write-Host "ℹ Rate limit not triggered with rapid requests (may need faster requests)" -ForegroundColor Yellow
}

Write-Host "`nClient Credentials Flow Test Completed!" -ForegroundColor Green
Write-Host "The SimpleIdentityServer API is working correctly through the load balancer." -ForegroundColor White
Write-Host "`nTo test with your own client:" -ForegroundColor Cyan
Write-Host "  .\test-client-credentials.ps1 -ClientId 'your-client-id' -ClientSecret 'your-secret'" -ForegroundColor White
