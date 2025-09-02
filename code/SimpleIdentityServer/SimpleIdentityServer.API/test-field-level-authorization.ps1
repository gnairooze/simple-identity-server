# PowerShell script to test API3:2023 - Broken Object Property Level Authorization implementation
# This script tests field-level authorization controls

param(
    [string]$IdentityServerUrl = "https://localhost:7443",
    [string]$ResourceApiUrl = "https://localhost:7444",
    [switch]$SkipCertCheck
)

Write-Host "Testing API3:2023 - Field-Level Authorization Implementation" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan

if ($SkipCertCheck) {
    Write-Host "Skipping certificate validation..." -ForegroundColor Yellow
    [System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
    
    # For PowerShell Core 6+
    if ($PSVersionTable.PSVersion.Major -ge 6) {
        $PSDefaultParameterValues['Invoke-RestMethod:SkipCertificateCheck'] = $true
        $PSDefaultParameterValues['Invoke-WebRequest:SkipCertificateCheck'] = $true
    }
}

function Get-AccessToken {
    param(
        [string]$ClientId,
        [string]$ClientSecret,
        [string]$Scope = "api1.read"
    )
    
    $tokenEndpoint = "$IdentityServerUrl/connect/token"
    $body = @{
        grant_type = "client_credentials"
        client_id = $ClientId
        client_secret = $ClientSecret
        scope = $Scope
    }
    
    try {
        $response = Invoke-RestMethod -Uri $tokenEndpoint -Method Post -Body $body -ContentType "application/x-www-form-urlencoded"
        return $response.access_token
    }
    catch {
        Write-Host "Failed to get token for client $ClientId : $($_.Exception.Message)" -ForegroundColor Red
        return $null
    }
}

function Test-ApiEndpoint {
    param(
        [string]$Token,
        [string]$Endpoint,
        [string]$ClientType
    )
    
    $headers = @{
        Authorization = "Bearer $Token"
        Accept = "application/json"
    }
    
    try {
        Write-Host "Testing $Endpoint as $ClientType..." -ForegroundColor Yellow
        $response = Invoke-RestMethod -Uri $Endpoint -Headers $headers -Method Get
        
        Write-Host "Response received:" -ForegroundColor Green
        $response | ConvertTo-Json -Depth 3 | Write-Host
        
        # Analyze response for field-level filtering
        if ($response -is [Array] -and $response.Count -gt 0) {
            $firstItem = $response[0]
            $properties = $firstItem.PSObject.Properties.Name
            
            Write-Host "Available properties: $($properties -join ', ')" -ForegroundColor Cyan
            
            # Check for sensitive fields
            $sensitiveFields = @("temperatureC", "temperatureF", "humidity", "pressure", "internalId")
            $exposedSensitiveFields = $properties | Where-Object { $_ -in $sensitiveFields }
            
            if ($exposedSensitiveFields) {
                Write-Host "Sensitive fields exposed: $($exposedSensitiveFields -join ', ')" -ForegroundColor Magenta
            } else {
                Write-Host "No sensitive fields exposed - good field-level filtering!" -ForegroundColor Green
            }
        }
        
        Write-Host "---" -ForegroundColor Gray
        return $true
    }
    catch {
        Write-Host "Failed to call $Endpoint : $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "---" -ForegroundColor Gray
        return $false
    }
}

function Test-TokenIntrospection {
    param(
        [string]$Token,
        [string]$IntrospectingClientId,
        [string]$IntrospectingClientSecret
    )
    
    $introspectionEndpoint = "$IdentityServerUrl/connect/introspect"
    $body = @{
        token = $Token
        client_id = $IntrospectingClientId
        client_secret = $IntrospectingClientSecret
    }
    
    try {
        Write-Host "Testing token introspection with client $IntrospectingClientId..." -ForegroundColor Yellow
        $response = Invoke-RestMethod -Uri $introspectionEndpoint -Method Post -Body $body -ContentType "application/x-www-form-urlencoded"
        
        Write-Host "Introspection response:" -ForegroundColor Green
        $response | ConvertTo-Json -Depth 2 | Write-Host
        
        # Check for sensitive information exposure
        $sensitiveFields = @("sub", "scope", "token_type")
        $exposedFields = $response.PSObject.Properties.Name | Where-Object { $_ -in $sensitiveFields }
        
        if ($exposedFields) {
            Write-Host "Detailed token information exposed: $($exposedFields -join ', ')" -ForegroundColor Magenta
        } else {
            Write-Host "Only basic token information exposed - good introspection filtering!" -ForegroundColor Green
        }
        
        Write-Host "---" -ForegroundColor Gray
        return $true
    }
    catch {
        Write-Host "Failed to introspect token: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "---" -ForegroundColor Gray
        return $false
    }
}

# Test scenarios
Write-Host "`n1. Testing with different client types..." -ForegroundColor Cyan

# Test with service client (should see all fields)
Write-Host "`nTesting SERVICE CLIENT (should see sensitive data):" -ForegroundColor Yellow
$serviceToken = Get-AccessToken -ClientId "service-api" -ClientSecret "service-secret-key-here" -Scope "api1.read"
if ($serviceToken) {
    Test-ApiEndpoint -Token $serviceToken -Endpoint "$ResourceApiUrl/WeatherForecast" -ClientType "Service"
    Test-ApiEndpoint -Token $serviceToken -Endpoint "$ResourceApiUrl/WeatherForecast/detailed" -ClientType "Service"
}

# Test with web client (should see limited fields)
Write-Host "`nTesting WEB CLIENT (should see limited data):" -ForegroundColor Yellow
$webToken = Get-AccessToken -ClientId "web-app" -ClientSecret "web-secret-key-here" -Scope "api1.read"
if ($webToken) {
    Test-ApiEndpoint -Token $webToken -Endpoint "$ResourceApiUrl/WeatherForecast" -ClientType "Web"
    Test-ApiEndpoint -Token $webToken -Endpoint "$ResourceApiUrl/WeatherForecast/detailed" -ClientType "Web"
}

# Test with mobile client (should see limited fields)
Write-Host "`nTesting MOBILE CLIENT (should see limited data):" -ForegroundColor Yellow
$mobileToken = Get-AccessToken -ClientId "mobile-app" -ClientSecret "mobile-secret-key-here" -Scope "api1.read"
if ($mobileToken) {
    Test-ApiEndpoint -Token $mobileToken -Endpoint "$ResourceApiUrl/WeatherForecast" -ClientType "Mobile"
    Test-ApiEndpoint -Token $mobileToken -Endpoint "$ResourceApiUrl/WeatherForecast/detailed" -ClientType "Mobile"
}

# Test token introspection with different clients
Write-Host "`n2. Testing token introspection field-level authorization..." -ForegroundColor Cyan

if ($serviceToken) {
    # Test introspection by service client (trusted)
    Test-TokenIntrospection -Token $serviceToken -IntrospectingClientId "service-api" -IntrospectingClientSecret "service-secret-key-here"
    
    # Test introspection by web client (less trusted)
    Test-TokenIntrospection -Token $serviceToken -IntrospectingClientId "web-app" -IntrospectingClientSecret "web-secret-key-here"
}

Write-Host "`n3. Testing Identity Server endpoints (should not be filtered)..." -ForegroundColor Cyan

# Test discovery endpoint
try {
    Write-Host "Testing OpenID Configuration..." -ForegroundColor Yellow
    $config = Invoke-RestMethod -Uri "$IdentityServerUrl/.well-known/openid-configuration"
    Write-Host "Discovery endpoint working - $(($config.PSObject.Properties.Name).Count) properties exposed" -ForegroundColor Green
}
catch {
    Write-Host "Failed to get OpenID configuration: $($_.Exception.Message)" -ForegroundColor Red
}

# Test JWKS endpoint
try {
    Write-Host "Testing JWKS endpoint..." -ForegroundColor Yellow
    $jwks = Invoke-RestMethod -Uri "$IdentityServerUrl/.well-known/jwks"
    Write-Host "JWKS endpoint working - $($jwks.keys.Count) keys exposed" -ForegroundColor Green
}
catch {
    Write-Host "Failed to get JWKS: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nField-Level Authorization Testing Complete!" -ForegroundColor Cyan
Write-Host "Check the responses above to verify:" -ForegroundColor White
Write-Host "- Service clients see sensitive data (temperature, humidity, pressure)" -ForegroundColor White
Write-Host "- Web/Mobile clients see only basic data (date, summary, location)" -ForegroundColor White
Write-Host "- Admin clients see internal IDs" -ForegroundColor White
Write-Host "- Token introspection respects client permissions" -ForegroundColor White
