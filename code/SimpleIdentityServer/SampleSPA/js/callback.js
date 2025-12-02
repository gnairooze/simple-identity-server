import { PKCEHelper } from './pkce-helper.js';
import { getConfig } from './config.js';

/**
 * OAuth 2.0 Callback Handler
 * This page handles the redirect from the authorization server
 */
class CallbackHandler {
    constructor() {
        this.config = getConfig();
        this.handleCallback();
    }

    /**
     * Handle the OAuth callback
     */
    async handleCallback() {
        try {
            // Parse URL parameters
            const urlParams = new URLSearchParams(window.location.search);

            // Check for error
            const error = urlParams.get('error');
            if (error) {
                const errorDescription = urlParams.get('error_description') || error;
                throw new Error(errorDescription);
            }

            // Get authorization code and state
            const code = urlParams.get('code');
            const state = urlParams.get('state');

            if (!code) {
                throw new Error('No authorization code received');
            }

            // Update status
            this.updateStatus('Exchanging authorization code for tokens...');

            // Exchange code for tokens
            const tokens = await PKCEHelper.exchangeCodeForTokens(
                this.config,
                code,
                state
            );

            // Validate tokens
            if (!tokens.access_token || !tokens.id_token) {
                throw new Error('Invalid token response from server');
            }

            // Parse and validate ID token
            const claims = PKCEHelper.parseJWT(tokens.id_token);
            console.log('User authenticated:', claims);

            // Store tokens
            sessionStorage.setItem('oauth_tokens', JSON.stringify(tokens));

            // Update status
            this.updateStatus('Authentication successful! Redirecting...');

            // Redirect to main page
            setTimeout(() => {
                window.location.href = '/index.html';
            }, 1000);

        } catch (error) {
            console.error('Callback error:', error);
            this.showError(error.message);
        }
    }

    /**
     * Update status message
     */
    updateStatus(message) {
        const statusElement = document.getElementById('callback-status');
        if (statusElement) {
            statusElement.textContent = message;
        }
    }

    /**
     * Show error message
     */
    showError(message) {
        const statusElement = document.getElementById('callback-status');
        const errorElement = document.getElementById('callback-error');

        if (statusElement) {
            statusElement.textContent = 'Authentication Failed';
        }

        if (errorElement) {
            errorElement.textContent = message;
            errorElement.style.display = 'block';
        }

        // Hide loading spinner
        const spinner = document.querySelector('.loading-spinner');
        if (spinner) {
            spinner.style.display = 'none';
        }

        // Redirect back to main page after 5 seconds
        setTimeout(() => {
            window.location.href = '/index.html';
        }, 5000);
    }
}

// Initialize callback handler when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    new CallbackHandler();
});

