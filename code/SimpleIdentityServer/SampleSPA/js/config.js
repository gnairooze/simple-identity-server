/**
 * OAuth 2.0 Configuration
 * Update these values to match your Identity Server setup
 */
export const config = {
    // OAuth 2.0 Client Configuration
    clientId: 'web-app',
    clientSecret: '', // Optional: Leave empty for public clients (PKCE)

    // Identity Server Endpoints
    authorizationEndpoint: 'https://localhost:8443/connect/authorize',
    tokenEndpoint: 'https://localhost:8443/connect/token',
    userinfoEndpoint: 'https://localhost:8443/connect/userinfo',
    logoutEndpoint: 'https://localhost:8443/connect/logout',

    // OAuth 2.0 Parameters
    redirectUri: window.location.origin + '/callback.html',
    postLogoutRedirectUri: window.location.origin + '/index.html',
    // Scopes: 'openid' is required, 'profile' and 'email' provide user info
    // Note: 'roles' scope requires additional database setup (run fix-scopes.sql)
    scope: 'openid profile email',

    // Response Type (use 'code' for Authorization Code flow)
    responseType: 'code'
};

/**
 * Get the full configuration object
 */
export function getConfig() {
    return config;
}

/**
 * Update configuration (useful for dynamic configuration)
 */
export function updateConfig(updates) {
    Object.assign(config, updates);
}

