/**
 * Core authentication module with security features
 */
export class AuthModule {
    constructor() {
        this.csrfToken = this.getCsrfToken();
        this.rateLimiter = new RateLimiter();
        this.validator = new InputValidator();
    }

    /**
     * Get CSRF token from meta tag or form
     */
    getCsrfToken() {
        const token = document.querySelector('meta[name="__RequestVerificationToken"]')?.content ||
                     document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        
        if (!token) {
            console.warn('CSRF token not found');
        }
        
        return token;
    }

    /**
     * Make secure AJAX request with CSRF protection
     */
    async secureRequest(url, data, options = {}) {
        if (!this.rateLimiter.canProceed()) {
            throw new Error('Rate limit exceeded. Please wait before trying again.');
        }

        const defaultOptions = {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest',
                ...(this.csrfToken && { 'RequestVerificationToken': this.csrfToken })
            },
            credentials: 'same-origin',
            body: JSON.stringify(data)
        };

        const requestOptions = { ...defaultOptions, ...options };

        try {
            const response = await fetch(url, requestOptions);
            
            if (!response.ok) {
                if (response.status === 429) {
                    this.rateLimiter.recordFailure();
                    throw new Error('Too many requests. Please wait before trying again.');
                }
                throw new Error(`Request failed: ${response.status}`);
            }

            return await response.json();
        } catch (error) {
            this.rateLimiter.recordFailure();
            throw error;
        }
    }

    /**
     * Validate form input securely
     */
    validateInput(formData) {
        return this.validator.validateForm(formData);
    }

    /**
     * Show error message with XSS protection
     */
    showError(message, container = null) {
        const errorContainer = container || document.querySelector('.alert-danger');
        if (errorContainer) {
            // Sanitize message to prevent XSS
            errorContainer.textContent = message;
            errorContainer.style.display = 'block';
            errorContainer.setAttribute('role', 'alert');
            errorContainer.focus();
        }
    }

    /**
     * Clear error messages
     */
    clearErrors() {
        const errorContainers = document.querySelectorAll('.alert-danger, .field-validation-error');
        errorContainers.forEach(container => {
            container.textContent = '';
            container.style.display = 'none';
        });
    }
}

/**
 * Rate limiting for authentication attempts
 */
class RateLimiter {
    constructor(maxAttempts = 5, windowMs = 300000) { // 5 attempts per 5 minutes
        this.maxAttempts = maxAttempts;
        this.windowMs = windowMs;
        this.attempts = this.getStoredAttempts();
    }

    canProceed() {
        this.cleanOldAttempts();
        return this.attempts.length < this.maxAttempts;
    }

    recordFailure() {
        this.attempts.push(Date.now());
        this.storeAttempts();
    }

    cleanOldAttempts() {
        const cutoff = Date.now() - this.windowMs;
        this.attempts = this.attempts.filter(time => time > cutoff);
        this.storeAttempts();
    }

    getStoredAttempts() {
        try {
            const stored = sessionStorage.getItem('auth_attempts');
            return stored ? JSON.parse(stored) : [];
        } catch {
            return [];
        }
    }

    storeAttempts() {
        try {
            sessionStorage.setItem('auth_attempts', JSON.stringify(this.attempts));
        } catch {
            // Storage failed, continue without persistence
        }
    }
}

/**
 * Input validation with security focus
 */
class InputValidator {
    constructor() {
        this.patterns = {
            email: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
            password: /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/
        };
    }

    validateForm(formData) {
        const errors = [];

        // Email validation
        if (formData.email && !this.patterns.email.test(formData.email)) {
            errors.push('Please enter a valid email address');
        }

        // Password validation
        if (formData.password && !this.patterns.password.test(formData.password)) {
            errors.push('Password must be at least 8 characters with uppercase, lowercase, number, and special character');
        }

        // Password confirmation
        if (formData.password && formData.confirmPassword && formData.password !== formData.confirmPassword) {
            errors.push('Passwords do not match');
        }

        return {
            isValid: errors.length === 0,
            errors
        };
    }

    sanitizeInput(input) {
        if (typeof input !== 'string') return input;
        
        // Basic XSS prevention
        return input
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#x27;')
            .replace(/\//g, '&#x2F;');
    }
}

