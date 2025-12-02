import { PKCEHelper } from './pkce-helper.js';
import { getConfig } from './config.js';

/**
 * Main Application Logic
 */
class App {
    constructor() {
        this.config = getConfig();
        this.tokens = null;
        this.userInfo = null;
        this.init();
    }

    /**
     * Initialize the application
     */
    init() {
        // Check if user is already authenticated
        this.checkAuthState();

        // Setup event listeners
        this.setupEventListeners();

        // Display configuration
        this.displayConfiguration();
    }

    /**
     * Check if user is authenticated
     */
    checkAuthState() {
        const tokensJson = sessionStorage.getItem('oauth_tokens');
        if (tokensJson) {
            try {
                this.tokens = JSON.parse(tokensJson);
                const claims = PKCEHelper.parseJWT(this.tokens.id_token);

                // Check if token is still valid
                if (claims.exp && claims.exp > Date.now() / 1000) {
                    this.showAuthenticatedState(this.tokens, claims);
                } else {
                    // Token expired
                    this.logout();
                }
            } catch (error) {
                console.error('Error parsing stored tokens:', error);
                this.logout();
            }
        } else {
            this.showNotAuthenticatedState();
        }
    }

    /**
     * Setup event listeners
     */
    setupEventListeners() {
        // Login button
        const loginBtn = document.getElementById('login-btn');
        if (loginBtn) {
            loginBtn.addEventListener('click', () => this.login());
        }

        // Logout button
        const logoutBtn = document.getElementById('logout-btn');
        if (logoutBtn) {
            logoutBtn.addEventListener('click', () => this.logout());
        }

        // Refresh button
        const refreshBtn = document.getElementById('refresh-btn');
        if (refreshBtn) {
            refreshBtn.addEventListener('click', () => this.refreshUserInfo());
        }

        // Retry button
        const retryBtn = document.getElementById('retry-btn');
        if (retryBtn) {
            retryBtn.addEventListener('click', () => {
                this.hideAllSections();
                this.showNotAuthenticatedState();
            });
        }

        // Copy token buttons
        document.querySelectorAll('.btn-copy').forEach(btn => {
            btn.addEventListener('click', (e) => {
                const tokenType = e.target.dataset.token;
                this.copyToken(tokenType);
            });
        });
    }

    /**
     * Display configuration on the page
     */
    displayConfiguration() {
        document.getElementById('config-client-id').textContent = this.config.clientId;
        document.getElementById('config-auth-endpoint').textContent = this.config.authorizationEndpoint;
        document.getElementById('config-token-endpoint').textContent = this.config.tokenEndpoint;
        document.getElementById('config-redirect-uri').textContent = this.config.redirectUri;
    }

    /**
     * Initiate login flow
     */
    async login() {
        try {
            this.showLoading();
            const authUrl = await PKCEHelper.generateAuthorizationUrl(this.config);
            // Redirect to authorization endpoint
            window.location.href = authUrl;
        } catch (error) {
            console.error('Login error:', error);
            this.showError('Failed to initiate login: ' + error.message);
        }
    }

    /**
     * Logout user
     */
    logout() {
        // Clear stored tokens
        sessionStorage.removeItem('oauth_tokens');
        sessionStorage.removeItem('pkce_code_verifier');
        sessionStorage.removeItem('oauth_state');

        this.tokens = null;
        this.userInfo = null;

        this.showNotAuthenticatedState();
    }

    /**
     * Refresh user information
     */
    async refreshUserInfo() {
        if (!this.tokens || !this.tokens.access_token) {
            return;
        }

        try {
            const refreshBtn = document.getElementById('refresh-btn');
            refreshBtn.disabled = true;
            refreshBtn.innerHTML = '<span>🔄</span> Refreshing...';

            const userInfo = await PKCEHelper.getUserInfo(this.config, this.tokens.access_token);
            this.userInfo = userInfo;

            // Update display
            const claims = PKCEHelper.parseJWT(this.tokens.id_token);
            this.displayUserInfo(claims, userInfo);

            refreshBtn.disabled = false;
            refreshBtn.innerHTML = '<span>🔄</span> Refresh User Info';
        } catch (error) {
            console.error('Failed to refresh user info:', error);
            alert('Failed to refresh user info: ' + error.message);

            const refreshBtn = document.getElementById('refresh-btn');
            refreshBtn.disabled = false;
            refreshBtn.innerHTML = '<span>🔄</span> Refresh User Info';
        }
    }

    /**
     * Copy token to clipboard
     */
    async copyToken(tokenType) {
        if (!this.tokens) return;

        const token = tokenType === 'access' ? this.tokens.access_token : this.tokens.id_token;

        try {
            await navigator.clipboard.writeText(token);
            alert(`${tokenType === 'access' ? 'Access' : 'ID'} token copied to clipboard!`);
        } catch (error) {
            console.error('Failed to copy token:', error);
            alert('Failed to copy token to clipboard');
        }
    }

    /**
     * Show not authenticated state
     */
    showNotAuthenticatedState() {
        this.hideAllSections();
        document.getElementById('not-authenticated').style.display = 'block';
    }

    /**
     * Show authenticated state
     */
    showAuthenticatedState(tokens, claims) {
        this.hideAllSections();
        document.getElementById('authenticated').style.display = 'block';

        // Display tokens
        this.displayTokens(tokens);

        // Display user info
        this.displayUserInfo(claims);
    }

    /**
     * Display tokens
     */
    displayTokens(tokens) {
        document.getElementById('access-token-preview').textContent =
            tokens.access_token.substring(0, 50) + '...';
        document.getElementById('id-token-preview').textContent =
            tokens.id_token.substring(0, 50) + '...';
        document.getElementById('token-type').textContent = tokens.token_type || 'Bearer';
        document.getElementById('expires-in').textContent = tokens.expires_in || 'N/A';
    }

    /**
     * Display user information
     */
    displayUserInfo(claims, userInfo = null) {
        // Get user name and email
        const name = claims.name || claims.email || 'User';
        const email = claims.email || 'No email';

        // Display in header
        document.getElementById('user-name').textContent = name;
        document.getElementById('user-email').textContent = email;

        // Set initials
        const initials = name.split(' ')
            .map(part => part[0])
            .join('')
            .toUpperCase()
            .substring(0, 2);
        document.getElementById('user-initials').textContent = initials;

        // Display claims
        this.displayClaims(claims);
    }

    /**
     * Display claims from ID token
     */
    displayClaims(claims) {
        const claimsList = document.getElementById('claims-list');
        claimsList.innerHTML = '';

        // Filter and display important claims
        const importantClaims = [
            'sub', 'name', 'email', 'email_verified',
            'preferred_username', 'given_name', 'family_name',
            'role', 'roles', 'aud', 'iss', 'exp', 'iat'
        ];

        for (const [key, value] of Object.entries(claims)) {
            if (importantClaims.includes(key) || !key.startsWith('_')) {
                const claimItem = document.createElement('div');
                claimItem.className = 'claim-item';

                let displayValue = value;
                if (key === 'exp' || key === 'iat') {
                    displayValue = new Date(value * 1000).toLocaleString();
                } else if (Array.isArray(value)) {
                    displayValue = value.join(', ');
                } else if (typeof value === 'object') {
                    displayValue = JSON.stringify(value);
                }

                claimItem.innerHTML = `
                    <span class="claim-key">${key}</span>
                    <span class="claim-value">${displayValue}</span>
                `;

                claimsList.appendChild(claimItem);
            }
        }
    }

    /**
     * Show loading state
     */
    showLoading() {
        this.hideAllSections();
        document.getElementById('loading').style.display = 'block';
    }

    /**
     * Show error state
     */
    showError(message) {
        this.hideAllSections();
        document.getElementById('error').style.display = 'block';
        document.getElementById('error-message').textContent = message;
    }

    /**
     * Hide all sections
     */
    hideAllSections() {
        document.getElementById('not-authenticated').style.display = 'none';
        document.getElementById('authenticated').style.display = 'none';
        document.getElementById('loading').style.display = 'none';
        document.getElementById('error').style.display = 'none';
    }
}

// Initialize app when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    new App();
});

