/**
 * PKCE (Proof Key for Code Exchange) implementation for OAuth 2.0 Authorization Code flow
 */
export class PKCEHelper {
    /**
     * Generate a cryptographically secure code verifier
     */
    static generateCodeVerifier() {
        const array = new Uint8Array(32);
        crypto.getRandomValues(array);
        return this.base64URLEncode(array);
    }

    /**
     * Generate code challenge from verifier using SHA256
     */
    static async generateCodeChallenge(verifier) {
        const encoder = new TextEncoder();
        const data = encoder.encode(verifier);
        const digest = await crypto.subtle.digest('SHA-256', data);
        return this.base64URLEncode(new Uint8Array(digest));
    }

    /**
     * Base64 URL encode (RFC 4648 Section 5)
     */
    static base64URLEncode(array) {
        return btoa(String.fromCharCode(...array))
            .replace(/\+/g, '-')
            .replace(/\//g, '_')
            .replace(/=/g, '');
    }

    /**
     * Generate authorization URL with PKCE parameters
     */
    static async generateAuthorizationUrl(config) {
        const codeVerifier = this.generateCodeVerifier();
        const codeChallenge = await this.generateCodeChallenge(codeVerifier);
        
        // Store code verifier for token exchange
        sessionStorage.setItem('pkce_code_verifier', codeVerifier);
        
        const params = new URLSearchParams({
            response_type: 'code',
            client_id: config.clientId,
            redirect_uri: config.redirectUri,
            scope: config.scope || 'openid profile email',
            state: this.generateState(),
            code_challenge: codeChallenge,
            code_challenge_method: 'S256'
        });

        return `${config.authorizationEndpoint}?${params.toString()}`;
    }

    /**
     * Generate cryptographically secure state parameter
     */
    static generateState() {
        const array = new Uint8Array(16);
        crypto.getRandomValues(array);
        const state = this.base64URLEncode(array);
        
        // Store state for validation
        sessionStorage.setItem('oauth_state', state);
        
        return state;
    }

    /**
     * Exchange authorization code for tokens
     */
    static async exchangeCodeForTokens(config, authorizationCode, state) {
        // Validate state parameter
        const storedState = sessionStorage.getItem('oauth_state');
        if (!storedState || storedState !== state) {
            throw new Error('Invalid state parameter');
        }

        // Get stored code verifier
        const codeVerifier = sessionStorage.getItem('pkce_code_verifier');
        if (!codeVerifier) {
            throw new Error('Code verifier not found');
        }

        const tokenData = {
            grant_type: 'authorization_code',
            client_id: config.clientId,
            client_secret: config.clientSecret,
            code: authorizationCode,
            redirect_uri: config.redirectUri,
            code_verifier: codeVerifier
        };

        try {
            const response = await fetch(config.tokenEndpoint, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'Accept': 'application/json'
                },
                body: new URLSearchParams(tokenData)
            });

            if (!response.ok) {
                const errorData = await response.json().catch(() => ({}));
                throw new Error(errorData.error_description || 'Token exchange failed');
            }

            const tokens = await response.json();
            
            // Clean up stored values
            sessionStorage.removeItem('pkce_code_verifier');
            sessionStorage.removeItem('oauth_state');
            
            return tokens;
        } catch (error) {
            // Clean up on error
            sessionStorage.removeItem('pkce_code_verifier');
            sessionStorage.removeItem('oauth_state');
            throw error;
        }
    }

    /**
     * Validate and parse JWT token (basic validation)
     */
    static parseJWT(token) {
        try {
            const parts = token.split('.');
            if (parts.length !== 3) {
                throw new Error('Invalid JWT format');
            }

            const payload = JSON.parse(atob(parts[1].replace(/-/g, '+').replace(/_/g, '/')));
            
            // Check expiration
            if (payload.exp && payload.exp < Date.now() / 1000) {
                throw new Error('Token has expired');
            }

            return payload;
        } catch (error) {
            throw new Error('Invalid JWT token');
        }
    }
}

