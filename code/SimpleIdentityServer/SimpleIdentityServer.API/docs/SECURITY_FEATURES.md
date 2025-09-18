# API4:2023 - Unrestricted Resource Consumption - Implementation Guide

This document describes the implementation of security measures to address **API4:2023 - Unrestricted Resource Consumption** from the OWASP API Security Top 10.

## Overview

The following security measures have been implemented to prevent unrestricted resource consumption:

1. **Rate Limiting Middleware** - Controls request frequency per client/IP
2. **Request Size Limits** - Prevents large payload attacks
3. **Connection Throttling** - Limits concurrent connections
4. **Database Timeout Configuration** - Prevents long-running queries
5. **Token Generation Monitoring** - Tracks and alerts on suspicious token request patterns

## Implemented Features

### 1. Rate Limiting

#### Global Rate Limiting
- **Default**: 100 requests per minute per client/IP
- **Configurable**: Via `appsettings.json` under `RateLimiting.Global`
- **Partition Key**: Uses client_id when available, falls back to IP address
- **Load Balancer Support**: Properly handles X-Forwarded-For headers for real client IP detection

#### Endpoint-Specific Rate Limiting

**Token Endpoint (`/connect/token`)**
- **Default**: 20 requests per minute per client
- **Policy**: `TokenPolicy`
- **Purpose**: Prevent token farming attacks

**Introspection Endpoint (`/connect/introspect`)**
- **Default**: 50 requests per minute per client
- **Policy**: `IntrospectionPolicy`
- **Purpose**: Prevent token validation abuse

#### Rate Limit Response
When rate limit is exceeded, the API returns:
- **HTTP Status**: 429 (Too Many Requests)
- **Headers**: `Retry-After` with seconds to wait
- **Body**: JSON error with retry information

```json
{
  "error": "too_many_requests",
  "error_description": "Rate limit exceeded. Please retry after the specified time.",
  "retry_after_seconds": "60"
}
```

### 2. Request Size Limits

- **Maximum Request Body Size**: 1MB (1,048,576 bytes)
- **Model Binding Collection Size**: Limited to 1000 items
- **Applies to**: Both IIS and Kestrel servers
- **Purpose**: Prevent memory exhaustion attacks

### 3. Connection Throttling

- **Maximum Concurrent Connections**: 100
- **Maximum Concurrent Upgraded Connections**: 100
- **Request Headers Timeout**: 30 seconds
- **Keep-Alive Timeout**: 2 minutes
- **Purpose**: Prevent connection exhaustion

### 4. Database Timeout Configuration

- **Command Timeout**: 30 seconds
- **Connection Retry**: 3 attempts with 5-second delay
- **Purpose**: Prevent database resource exhaustion

### 5. Security Monitoring

#### Token Request Monitoring
- **Suspicious Activity Threshold**: 10 requests in 5 minutes
- **High Frequency Threshold**: 100 requests in 1 hour
- **Tracking Retention**: 1 hour of request history
- **Logging**: Structured logging with correlation IDs
- **Load Balancer Support**: Real client IP tracking in logs

#### Request Performance Monitoring
- **Slow Request Threshold**: 5 seconds
- **Logging**: All requests with duration and metadata
- **Correlation**: Each request gets a unique ID for tracking
- **IP Address Accuracy**: Proper client IP detection behind load balancers

### 6. Load Balancer Support

#### Forwarded Headers Processing
- **X-Forwarded-For**: Extracts real client IP from proxy headers
- **X-Forwarded-Proto**: Handles protocol forwarding for HTTPS
- **Trusted Proxies**: Configurable list of trusted proxy IPs
- **Trusted Networks**: Support for CIDR network ranges
- **Security**: Limited header processing to prevent injection attacks

#### Configuration Options
- **Enable/Disable**: Can be toggled via configuration
- **Trusted Proxies**: Specific IP addresses of load balancers/proxies
- **Trusted Networks**: CIDR ranges for private networks
- **Forward Limit**: Maximum forwarded headers to process (default: 2)
- **Header Symmetry**: Optional requirement for all headers to be present

## Configuration

### appsettings.json

```json
{
  "RateLimiting": {
    "Global": {
      "PermitLimit": 100,
      "WindowMinutes": 1
    },
    "TokenEndpoint": {
      "PermitLimit": 20,
      "WindowMinutes": 1
    },
    "IntrospectionEndpoint": {
      "PermitLimit": 50,
      "WindowMinutes": 1
    },
    "SecurityMonitoring": {
      "SuspiciousRequestThreshold5Min": 10,
      "HighFrequencyRequestThreshold1Hour": 100,
      "SlowRequestThresholdSeconds": 5.0,
      "RequestTrackingRetentionHours": 1
    }
  },
  "LoadBalancer": {
    "EnableForwardedHeaders": true,
    "TrustedProxies": [
      "192.168.1.100",
      "10.0.0.10"
    ],
    "TrustedNetworks": [
      "10.0.0.0/8",
      "172.16.0.0/12",
      "192.168.0.0/16",
      "127.0.0.0/8"
    ],
    "ForwardLimit": 2,
    "RequireHeaderSymmetry": false
  }
}
```

### Environment-Specific Configuration

For production environments, consider:

- **Reducing rate limits** for stricter control
- **Enabling additional monitoring** with Application Insights
- **Implementing distributed rate limiting** for multi-instance deployments
- **Adding circuit breaker patterns** for external dependencies

## Monitoring and Alerting

### Log Events

The system logs the following security events:

1. **TOKEN_REQUEST_MONITORED** - Normal token requests with client tracking
2. **SUSPICIOUS_TOKEN_FREQUENCY** - High frequency token requests (>10 in 5 min)
3. **HIGH_TOKEN_FREQUENCY** - Very high frequency requests (>100 in 1 hour)
4. **INTROSPECTION_REQUEST** - Token introspection requests
5. **REQUEST_COMPLETED** - All completed requests with duration
6. **REQUEST_EXCEPTION** - Unhandled exceptions with context

### Log Structure

All security logs include:
- **RequestId**: Correlation ID for request tracking
- **EventType**: Type of security event
- **Timestamp**: UTC timestamp
- **IpAddress**: Client IP address
- **UserAgent**: Client user agent
- **Path**: Request path
- **Method**: HTTP method
- **StatusCode**: Response status code
- **Duration**: Request processing time (where applicable)

## Testing

### Rate Limiting Tests

1. **Token Endpoint Test**:
   ```bash
   # Should succeed for first 20 requests, then return 429
   for i in {1..25}; do
     curl -X POST http://localhost:5000/connect/token \
       -d "grant_type=client_credentials&client_id=test&client_secret=secret"
   done
   ```

2. **Global Rate Limit Test**:
   ```bash
   # Should succeed for first 100 requests, then return 429
   for i in {1..105}; do
     curl -X GET http://localhost:5000/api/health
   done
   ```

### Load Balancer Testing

Use the provided PowerShell test script:
```powershell
.\test-rate-limiting.ps1 -BaseUrl "http://localhost:5000" -RequestCount 25
```

**Manual Testing with curl**:
```bash
# Test direct access
curl -X POST http://localhost:5000/connect/token \
  -d "grant_type=client_credentials&client_id=test&client_secret=secret"

# Test with X-Forwarded-For (simulating load balancer)
curl -X POST http://localhost:5000/connect/token \
  -H "X-Forwarded-For: 192.168.1.100" \
  -H "X-Forwarded-Proto: https" \
  -d "grant_type=client_credentials&client_id=test&client_secret=secret"
```

### Load Testing

Consider using tools like:
- **Apache Bench (ab)** for simple load testing
- **Artillery** for more complex scenarios
- **k6** for JavaScript-based load testing
- **PowerShell script** (included) for rate limiting validation

## Security Benefits

1. **DDoS Protection**: Rate limiting prevents overwhelming the server
2. **Resource Protection**: Request size limits prevent memory exhaustion
3. **Connection Management**: Connection throttling prevents connection exhaustion
4. **Database Protection**: Timeouts prevent long-running queries
5. **Anomaly Detection**: Monitoring identifies suspicious patterns
6. **Forensic Capability**: Detailed logging enables security analysis

## Performance Impact

- **Rate Limiting**: Minimal overhead (~1-2ms per request)
- **Security Monitoring**: Low overhead with in-memory tracking
- **Request Size Validation**: Negligible impact
- **Database Timeouts**: No performance impact, prevents hangs

## Maintenance

### Regular Tasks

1. **Monitor rate limit metrics** to adjust thresholds
2. **Review security logs** for suspicious patterns
3. **Update rate limiting configuration** based on usage patterns
4. **Clean up old tracking data** (handled automatically)

### Scaling Considerations

For high-traffic scenarios:
- Consider **distributed rate limiting** with Redis
- Implement **sliding window** rate limiting for smoother experience
- Add **adaptive rate limiting** based on system load
- Use **CDN-level protection** for additional security

## Compliance

This implementation addresses:
- **OWASP API Security Top 10 - API4:2023**
- **CWE-770**: Allocation of Resources Without Limits or Throttling
- **CWE-400**: Uncontrolled Resource Consumption

## Next Steps

Consider implementing:
1. **Distributed rate limiting** for multi-instance deployments
2. **Adaptive rate limiting** based on system metrics
3. **Circuit breaker patterns** for external dependencies
4. **Advanced anomaly detection** with machine learning
5. **Integration with SIEM systems** for security monitoring
