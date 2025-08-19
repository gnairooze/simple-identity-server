# Testing Resource.API with Postman

This guide explains how to test the protected Resource.API endpoints using Postman, including obtaining access tokens from the Identity Server and making authenticated requests.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Step 1: Start the Servers](#step-1-start-the-servers)
3. [Step 2: Obtain Access Token](#step-2-obtain-access-token)
4. [Step 3: Test Protected Endpoints](#step-3-test-protected-endpoints)
5. [Step 4: Test Authorization Scopes](#step-4-test-authorization-scopes)
6. [Common Issues and Troubleshooting](#common-issues-and-troubleshooting)
7. [Postman Collection Setup](#postman-collection-setup)

## Prerequisites

- Postman installed on your machine
- Simple Identity Server running on `https://localhost:7443`
- Resource.API running on `http://localhost:5198` or `https://localhost:7093`
- Basic understanding of OAuth 2.0 Client Credentials flow

## Step 1: Start the Servers

Before testing with Postman, ensure both servers are running:

### Start Identity Server
```bash
cd code/SimpleIdentityServer/SimpleIdentityServer.API
dotnet run
```
✅ Should be accessible at: `https://localhost:7443`

### Start Resource API
```bash
cd code/SimpleIdentityServer/Resource.API
dotnet run
```
✅ Should be accessible at `https://localhost:7093` (HTTPS)

## Step 2: Obtain Access Token

### 2.1 Create Token Request

1. **Open Postman** and create a new request
2. **Set Method**: `POST`
3. **Set URL**: `https://localhost:7443/connect/token`
4. **Configure Headers**:
   - `Content-Type`: `application/x-www-form-urlencoded`
   - `Accept`: `application/json`

### 2.2 Configure Request Body

Go to the **Body** tab and select **x-www-form-urlencoded**, then add these key-value pairs:

| Key | Value | Description |
|-----|-------|-------------|
| `grant_type` | `client_credentials` | OAuth 2.0 grant type |
| `client_id` | `service-api` | Registered client identifier |
| `client_secret` | `supersecret` | Client secret |
| `scope` | `api1.read api1.write` | Requested scopes (space-separated) |

### 2.3 Handle SSL Certificate Issues (Development)

If you encounter SSL certificate errors:

1. Go to **Postman Settings** → **General**
2. Turn **OFF** "SSL certificate verification"
3. Or use the HTTP endpoint if available

### 2.4 Send Request and Extract Token

**Expected Response:**
```json
{
  "access_token": "eyJhbGciOiJSUzI1NiIsImtpZCI6Ijc3MzVFRDM4ODNGNTA5...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "scope": "api1.read api1.write"
}
```

**Copy the `access_token` value** - you'll need it for the next steps.

## Step 3: Test Protected Endpoints

### 3.1 Test WeatherForecast Endpoint

1. **Create a new request**
2. **Set Method**: `GET`
3. **Set URL**: `https://localhost:7093/WeatherForecast`
4. **Configure Headers**:
   - `Authorization`: `Bearer YOUR_ACCESS_TOKEN_HERE`
   - `Accept`: `application/json`

### 3.2 Expected Successful Response

**Status**: `200 OK`

**Response Body**:
```json
[
  {
    "date": "2024-01-16",
    "temperatureC": 25,
    "temperatureF": 76,
    "summary": "Warm"
  },
  {
    "date": "2024-01-17",
    "temperatureC": 32,
    "temperatureF": 89,
    "summary": "Hot"
  },
  {
    "date": "2024-01-18",
    "temperatureC": 18,
    "temperatureF": 64,
    "summary": "Mild"
  },
  {
    "date": "2024-01-19",
    "temperatureC": 7,
    "temperatureF": 44,
    "summary": "Cool"
  },
  {
    "date": "2024-01-20",
    "temperatureC": -3,
    "temperatureF": 27,
    "summary": "Freezing"
  }
]
```

### 3.3 Test Without Authentication

1. **Remove the Authorization header**
2. **Send the same request**

**Expected Response**:
- **Status**: `401 Unauthorized`
- **Body**: Empty or error message

## Step 4: Test Authorization Scopes

### 4.1 Test with Limited Scope

To test scope-based authorization, request a token with limited scopes:

**Token Request Body** (only read scope):
| Key | Value |
|-----|-------|
| `grant_type` | `client_credentials` |
| `client_id` | `service-api` |
| `client_secret` | `supersecret` |
| `scope` | `api1.read` |

### 4.2 Test Different Client

You can also test with different registered clients:

**Web App Client**:
| Key | Value |
|-----|-------|
| `grant_type` | `client_credentials` |
| `client_id` | `web-app` |
| `client_secret` | `webapp-secret` |
| `scope` | `api1.read` |

**Mobile App Client**:
| Key | Value |
|-----|-------|
| `grant_type` | `client_credentials` |
| `client_id` | `mobile-app` |
| `client_secret` | `mobile-secret` |
| `scope` | `api1.read api1.write` |

## Step 5: Advanced Testing Scenarios

### 5.1 Test Token Expiration

1. **Wait for token to expire** (default: 1 hour)
2. **Use expired token** in API request
3. **Expected**: `401 Unauthorized` response

### 5.2 Test Invalid Token

1. **Modify a few characters** in the access token
2. **Send request** with invalid token
3. **Expected**: `401 Unauthorized` response

### 5.3 Test Token Introspection (Optional)

If you want to inspect token details:

**Request**:
- **Method**: `POST`
- **URL**: `https://localhost:7443/connect/introspect`
- **Headers**: `Content-Type: application/x-www-form-urlencoded`
- **Body**:
  | Key | Value |
  |-----|-------|
  | `client_id` | `service-api` |
  | `client_secret` | `supersecret` |
  | `token` | `YOUR_ACCESS_TOKEN` |

## Common Issues and Troubleshooting

### Issue 1: SSL Certificate Error
**Error**: `SSL certificate problem: self signed certificate`

**Solutions**:
- Disable SSL verification in Postman settings
- Use HTTP endpoints instead of HTTPS
- Add `-k` flag if using curl

### Issue 2: Connection Refused
**Error**: `Could not get response` or `Connection refused`

**Solutions**:
- Ensure both servers are running
- Check the correct ports (7443 for Identity Server, 5198/7093 for Resource API)
- Verify firewall settings

### Issue 3: 401 Unauthorized
**Possible Causes**:
- Missing or invalid Authorization header
- Expired access token
- Insufficient scopes
- Token format incorrect (missing "Bearer " prefix)

**Solutions**:
- Verify Authorization header format: `Bearer YOUR_TOKEN`
- Request a new access token
- Check requested scopes match endpoint requirements

### Issue 4: 400 Bad Request (Token Endpoint)
**Possible Causes**:
- Invalid client credentials
- Missing required parameters
- Incorrect grant_type

**Solutions**:
- Verify client_id and client_secret
- Ensure all required parameters are included
- Check parameter names and values

## Postman Collection Setup

### Create a Postman Collection

1. **Create Collection**: "Simple Identity Server API Tests"
2. **Add Environment Variables**:
   - `identity_server_url`: `https://localhost:7443`
   - `resource_api_url`: `http://localhost:5198`
   - `access_token`: (will be set dynamically)

### Collection Structure

```
📁 Simple Identity Server API Tests
├── 📂 Authentication
│   ├── 🔵 Get Access Token (Service API)
│   ├── 🔵 Get Access Token (Web App)
│   └── 🔵 Get Access Token (Mobile App)
├── 📂 Protected Endpoints
│   ├── 🟢 GET WeatherForecast (Authenticated)
│   ├── 🟢 GET WeatherForecast (No Auth) - Should Fail
│   └── 🔵 POST Introspect Token
└── 📂 Error Cases
    ├── 🟢 Invalid Token Test
    └── 🟢 Expired Token Test
```

### Pre-request Script for Token Management

Add this pre-request script to automatically use stored tokens:

```javascript
// Get token from environment variable
const token = pm.environment.get("access_token");

if (token) {
    pm.request.headers.add({
        key: "Authorization",
        value: `Bearer ${token}`
    });
}
```

### Post-response Script for Token Storage

Add this to the token request to automatically store the token:

```javascript
if (pm.response.code === 200) {
    const responseJson = pm.response.json();
    pm.environment.set("access_token", responseJson.access_token);
    console.log("Access token stored successfully");
}
```

## Testing Checklist

- [ ] Identity Server is running on port 7443
- [ ] Resource API is running on port 5198 or 7093
- [ ] Can obtain access token with valid client credentials
- [ ] Can access protected endpoint with valid token
- [ ] Receive 401 when accessing endpoint without token
- [ ] Receive 401 when using invalid/expired token
- [ ] Can test different clients (service-api, web-app, mobile-app)
- [ ] Scope-based authorization works correctly

## Security Notes

⚠️ **Important Security Reminders**:

1. **Client secrets** should be stored securely in production
2. **Access tokens** should be transmitted over HTTPS only
3. **Token storage** in Postman is for testing only - not production
4. **SSL certificate verification** should be enabled in production
5. **Token expiration** should be monitored and handled properly

## Additional Resources

- [OAuth 2.0 Client Credentials Flow](https://tools.ietf.org/html/rfc6749#section-4.4)
- [JWT Token Format](https://tools.ietf.org/html/rfc7519)
- [OpenIddict Documentation](https://documentation.openiddict.com/)
- [Postman Documentation](https://learning.postman.com/docs/)

---

**Happy Testing!** 🚀

This guide should help you thoroughly test your Resource.API endpoints using Postman. If you encounter any issues not covered here, check the server logs for additional debugging information.
