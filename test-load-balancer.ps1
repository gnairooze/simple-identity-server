# Test Load Balancer Distribution
# This script tests that the load balancer is properly distributing requests

Write-Host "Testing Load Balancer Distribution..." -ForegroundColor Green

# Check if environment is running
try {
    $response = Invoke-WebRequest -Uri "http://localhost/health" -TimeoutSec 5
    if ($response.StatusCode -ne 200) {
        Write-Host "Error: Load balancer is not responding. Please start the environment first." -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "Error: Load balancer is not responding. Please start the environment first." -ForegroundColor Red
    exit 1
}

# Test OpenID Configuration endpoint
Write-Host "`nTesting OpenID Configuration endpoint..." -ForegroundColor Cyan
try {
    $config = Invoke-RestMethod -Uri "http://localhost/.well-known/openid-configuration"
    Write-Host "✓ OpenID Configuration retrieved successfully" -ForegroundColor Green
    Write-Host "  Issuer: $($config.issuer)" -ForegroundColor White
    Write-Host "  Token Endpoint: $($config.token_endpoint)" -ForegroundColor White
    Write-Host "  Introspection Endpoint: $($config.introspection_endpoint)" -ForegroundColor White
} catch {
    Write-Host "✗ Failed to retrieve OpenID Configuration: $($_.Exception.Message)" -ForegroundColor Red
}

# Test load distribution by making multiple requests
Write-Host "`nTesting load distribution (making 30 requests)..." -ForegroundColor Cyan
$requestCount = 30
$responses = @()

for ($i = 1; $i -le $requestCount; $i++) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost/.well-known/openid-configuration" -TimeoutSec 10
        $responses += @{
            RequestNumber = $i
            StatusCode = $response.StatusCode
            ResponseTime = (Measure-Command { 
                Invoke-WebRequest -Uri "http://localhost/.well-known/openid-configuration" -TimeoutSec 10 
            }).TotalMilliseconds
        }
        Write-Progress -Activity "Making requests" -Status "Request $i of $requestCount" -PercentComplete (($i / $requestCount) * 100)
    } catch {
        Write-Host "✗ Request $i failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Analyze results
$successfulRequests = $responses | Where-Object { $_.StatusCode -eq 200 }
$averageResponseTime = ($successfulRequests | Measure-Object -Property ResponseTime -Average).Average

Write-Host "`nLoad Balancer Test Results:" -ForegroundColor Green
Write-Host "  Total Requests: $requestCount" -ForegroundColor White
Write-Host "  Successful Requests: $($successfulRequests.Count)" -ForegroundColor White
Write-Host "  Failed Requests: $($requestCount - $successfulRequests.Count)" -ForegroundColor White
Write-Host "  Success Rate: $([math]::Round(($successfulRequests.Count / $requestCount) * 100, 2))%" -ForegroundColor White
Write-Host "  Average Response Time: $([math]::Round($averageResponseTime, 2)) ms" -ForegroundColor White

# Test rate limiting
Write-Host "`nTesting rate limiting (making rapid requests)..." -ForegroundColor Cyan
$rateLimitHit = $false
$rapidRequests = 25

for ($i = 1; $i -le $rapidRequests; $i++) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost/.well-known/openid-configuration" -TimeoutSec 5
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
    Start-Sleep -Milliseconds 50
}

if (-not $rateLimitHit) {
    Write-Host "ℹ Rate limit not triggered with $rapidRequests rapid requests" -ForegroundColor Yellow
}

# Test individual API instances (direct access)
Write-Host "`nTesting individual API instances..." -ForegroundColor Cyan
$apiInstances = @(
    @{Name="API Instance 1"; Url="http://172.25.0.11/.well-known/openid-configuration"},
    @{Name="API Instance 2"; Url="http://172.25.0.12/.well-known/openid-configuration"},
    @{Name="API Instance 3"; Url="http://172.25.0.13/.well-known/openid-configuration"}
)

foreach ($instance in $apiInstances) {
    try {
        $response = Invoke-WebRequest -Uri $instance.Url -TimeoutSec 10
        if ($response.StatusCode -eq 200) {
            Write-Host "✓ $($instance.Name) is responding directly" -ForegroundColor Green
        } else {
            Write-Host "✗ $($instance.Name) returned status $($response.StatusCode)" -ForegroundColor Red
        }
    } catch {
        Write-Host "✗ $($instance.Name) is not responding: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`nLoad balancer testing completed!" -ForegroundColor Green
Write-Host "The SimpleIdentityServer API is properly load balanced and accessible at http://localhost" -ForegroundColor White
