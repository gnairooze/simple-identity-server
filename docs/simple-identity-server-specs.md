# identity server specs

## 0. Technology Stack

- OpenIdDict
- ASP.NET Core 8.0
- Entity Framework Core
- SQL Server
- Docker
- Modular Vanilla JavaScript (ES6+)
- Secure HTML5 & CSS3

## 1. Authentication Flows

### 1.1 Client Credentials Flow

- Use for service-to-service authentication
- No user interaction required
- Machine-to-machine communication

### 1.2 Authorization Code Flow

- Use for user authentication with web applications
- Includes PKCE (Proof Key for Code Exchange) for enhanced security
- Supports user login, logout, and password reset flows
- Redirect-based flow with authorization codes

## 2. Client Registration

### 2.1 Service-to-Service Clients (Client Credentials)

For each API/service that needs to authenticate:

- **Client ID**: A unique identifier for the client.
- **Client Secret or Credential**: Used to authenticate the client. a private key JWT.
- **Allowed Grant Types**: Set to `client_credentials` to restrict the flow to service-to-service scenarios.
- **Allowed Scopes**: Define which APIs/resources the client can access.

**Example Service Client Configuration (C#):**

```c#
new Client {
  ClientId = "service-api",
  ClientSecrets = { new Secret("supersecret".Sha256()) },
  AllowedGrantTypes = GrantTypes.ClientCredentials,
  AllowedScopes = { "api1.read", "api1.write" } 
}
```

### 2.2 Web Application Clients (Authorization Code)

For web applications that authenticate users:

- **Client ID**: A unique identifier for the web application.
- **Client Secret**: Confidential clients require a secret (stored securely on server).
- **Allowed Grant Types**: Set to `authorization_code` with PKCE support.
- **Redirect URIs**: Whitelist of allowed redirect URIs after authentication.
- **Post Logout Redirect URIs**: Allowed URIs after logout.
- **Allowed Scopes**: Include `openid`, `profile`, and custom scopes.

**Example Web Client Configuration (C#):**

```c#
new Client {
  ClientId = "web-app",
  ClientSecrets = { new Secret("web-secret".Sha256()) },
  AllowedGrantTypes = GrantTypes.Code,
  RequirePkce = true,
  RedirectUris = { "https://webapp.example.com/signin-oidc" },
  PostLogoutRedirectUris = { "https://webapp.example.com/signout-callback-oidc" },
  AllowedScopes = { "openid", "profile", "email", "api1.read" },
  AllowOfflineAccess = true // For refresh tokens
}
```

## 3. Defining Scopes and Claims

- **Scopes**: Each scope can be linked to specific claims that will be included in the access token.

- **Claims**: Assertions about the client (such as client ID, roles, or custom attributes) that are embedded in the access token and used by APIs for authorization decisions.

**How to Include Claims:**

- Define which claims should be issued for each scope.
- Claims will be fetched from a database..

**Example Scope with Claims:**

```c#
new ApiScope("api1.read", "Read access to API 1") {
  UserClaims = { "role", "client_id", "custom_claim" }
}
```

## 4. Client Authentication Methods

- **Private Key JWT**: use asymmetric keys.
- **Mutual TLS (mTLS)**: For even stronger client identity assurance.

## 5. Token Configuration

- **Access Token Content**: Ensure the token includes the necessary claims for the API to make authorization decisions.
- **Lifetime**: Set appropriate token lifetimes for your use case.
- **Signing**: Use strong signing algorithms to protect token integrity.

## 6. API Configuration

- **Validate Access Tokens**: APIs must validate incoming tokens, check their scopes, and extract claims for authorization.
- **Authorization Logic**: Use the claims in the token (such as roles or custom attributes) to enforce access control within your APIs.

## 7. Web Authentication Pages

### 7.1 Page Architecture

All authentication pages should be built using modular vanilla JavaScript with secure practices:

- **Modular ES6+ JavaScript**: Use ES modules for clean separation of concerns
- **Semantic HTML5**: Proper form elements with accessibility attributes
- **Secure CSS3**: No inline styles, proper Content Security Policy compliance
- **Progressive Enhancement**: Works without JavaScript for basic functionality

### 7.2 Required Pages

#### 7.2.1 Login Page (`/login`)

- Username/email and password fields
- "Remember me" checkbox (optional)
- "Forgot password?" link
- Client-side validation with server-side verification
- CSRF protection
- Rate limiting protection

#### 7.2.2 Registration Page (`/register`)

- Username, email, password, confirm password fields
- Password strength indicator
- Terms of service and privacy policy checkboxes
- Email verification workflow
- CAPTCHA integration for bot protection

#### 7.2.3 Password Reset Page (`/reset-password`)

- Email input for reset request
- Token-based reset form
- New password with confirmation
- Password strength requirements
- Secure token expiration (15-30 minutes)

#### 7.2.4 Logout Page (`/logout`)

- Confirmation dialog
- Single logout (SLO) support
- Clear all session data
- Redirect to safe landing page

### 7.3 Security Implementation

#### 7.3.1 JavaScript Security

```javascript
// Example secure module structure
export class AuthenticationModule {
  constructor() {
    this.csrfToken = this.getCsrfToken();
    this.rateLimiter = new RateLimiter();
  }
  
  async submitForm(formData) {
    // Input validation
    if (!this.validateInput(formData)) return false;
    
    // Rate limiting check
    if (!this.rateLimiter.canProceed()) {
      this.showError('Too many attempts. Please wait.');
      return false;
    }
    
    // Secure AJAX request
    return await this.secureRequest('/api/auth', formData);
  }
}
```

#### 7.3.2 HTML Security

```html
<!-- Example secure form structure -->
<form id="loginForm" method="POST" action="/login" novalidate>
  <input type="hidden" name="__RequestVerificationToken" value="{{csrfToken}}">
  
  <div class="form-group">
    <label for="username" class="sr-only">Username or Email</label>
    <input 
      type="text" 
      id="username" 
      name="username" 
      required 
      autocomplete="username"
      aria-describedby="username-error"
      maxlength="100">
    <div id="username-error" class="error-message" aria-live="polite"></div>
  </div>
  
  <div class="form-group">
    <label for="password" class="sr-only">Password</label>
    <input 
      type="password" 
      id="password" 
      name="password" 
      required 
      autocomplete="current-password"
      aria-describedby="password-error"
      minlength="8">
    <div id="password-error" class="error-message" aria-live="polite"></div>
  </div>
  
  <button type="submit" class="btn-primary">Sign In</button>
</form>
```

#### 7.3.3 CSS Security

```css
/* Secure CSS practices */
.form-container {
  max-width: 400px;
  margin: 0 auto;
  padding: 2rem;
  /* Prevent clickjacking */
  position: relative;
  z-index: 1;
}

/* Hide sensitive information from screen readers when needed */
.sensitive-hidden {
  position: absolute;
  left: -9999px;
  width: 1px;
  height: 1px;
  overflow: hidden;
}

/* Secure input styling */
input[type="password"] {
  font-family: monospace; /* Prevent font-based attacks */
  letter-spacing: 0.1em;
}
```

### 7.4 Content Security Policy (CSP)

Implement strict CSP headers for all authentication pages:

```http-headers
Content-Security-Policy: 
  default-src 'self'; 
  script-src 'self' 'unsafe-inline'; 
  style-src 'self' 'unsafe-inline'; 
  img-src 'self' data:; 
  font-src 'self'; 
  connect-src 'self'; 
  frame-ancestors 'none';
```

### 7.5 Additional Security Headers

```http-headers
X-Frame-Options: DENY
X-Content-Type-Options: nosniff
X-XSS-Protection: 1; mode=block
Referrer-Policy: strict-origin-when-cross-origin
Permissions-Policy: geolocation=(), microphone=(), camera=()
```

## 8. Authorization Code Flow Implementation

### 8.1 Flow Steps

1. **Authorization Request**: Client redirects user to authorization endpoint
2. **User Authentication**: User logs in through secure web pages
3. **Authorization Grant**: Server returns authorization code
4. **Token Request**: Client exchanges code for tokens
5. **Token Response**: Server returns access and refresh tokens

### 8.2 PKCE Implementation

```javascript
// Client-side PKCE implementation
class PKCEHelper {
  static generateCodeVerifier() {
    const array = new Uint8Array(32);
    crypto.getRandomValues(array);
    return this.base64URLEncode(array);
  }
  
  static async generateCodeChallenge(verifier) {
    const encoder = new TextEncoder();
    const data = encoder.encode(verifier);
    const digest = await crypto.subtle.digest('SHA-256', data);
    return this.base64URLEncode(new Uint8Array(digest));
  }
  
  static base64URLEncode(array) {
    return btoa(String.fromCharCode(...array))
      .replace(/\+/g, '-')
      .replace(/\//g, '_')
      .replace(/=/g, '');
  }
}
```

### 8.3 Server-side Configuration

```c#
// Configure Authorization Code flow in OpenIddict
options.SetAuthorizationEndpointUris("/connect/authorize")
       .SetTokenEndpointUris("/connect/token")
       .SetUserinfoEndpointUris("/connect/userinfo");

// Enable Authorization Code flow
options.AllowAuthorizationCodeFlow()
       .RequireProofKeyForCodeExchange(); // Enforce PKCE
```

## 9. Best Practices

### 9.1 Security

- **Audit and Monitor**: Keep logs of issued tokens and access for security reviews
- **HTTPS Only**: All authentication pages must use HTTPS
- **Secure Cookies**: Use HttpOnly, Secure, and SameSite attributes
- **Session Management**: Implement proper session timeout and renewal
- **Input Validation**: Validate all inputs on both client and server side
- **Rate Limiting**: Implement progressive delays for failed attempts

### 9.2 User Experience

- **Progressive Enhancement**: Ensure basic functionality without JavaScript
- **Accessibility**: Follow WCAG 2.1 AA guidelines
- **Mobile Responsive**: Optimize for mobile devices
- **Clear Error Messages**: Provide helpful, non-revealing error messages
- **Loading States**: Show appropriate loading indicators

### 9.3 Performance

- **Minimize JavaScript**: Keep authentication scripts lightweight
- **Lazy Loading**: Load non-critical resources after page load
- **Caching Strategy**: Implement appropriate caching for static assets
- **CDN Usage**: Serve static assets from CDN when possible

