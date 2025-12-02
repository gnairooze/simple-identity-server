import { AuthModule } from './auth-module.js';

/**
 * Login page functionality with security features
 */
class LoginPage {
    constructor() {
        this.authModule = new AuthModule();
        this.form = document.getElementById('loginForm');
        this.submitButton = document.getElementById('loginButton');
        this.init();
    }

    init() {
        if (!this.form) return;

        this.setupEventListeners();
        this.setupPasswordToggle();
        this.setupFormValidation();
    }

    setupEventListeners() {
        this.form.addEventListener('submit', this.handleSubmit.bind(this));
        
        // Real-time validation
        const inputs = this.form.querySelectorAll('input[required]');
        inputs.forEach(input => {
            input.addEventListener('blur', this.validateField.bind(this));
            input.addEventListener('input', this.clearFieldError.bind(this));
        });
    }

    setupPasswordToggle() {
        const toggleButtons = document.querySelectorAll('.password-toggle');
        toggleButtons.forEach(button => {
            button.addEventListener('click', this.togglePasswordVisibility.bind(this));
        });
    }

    setupFormValidation() {
        // Enable submit button only when form is valid
        const requiredInputs = this.form.querySelectorAll('input[required]');
        requiredInputs.forEach(input => {
            input.addEventListener('input', this.updateSubmitButton.bind(this));
        });
    }

    async handleSubmit(event) {
        event.preventDefault();
        
        if (this.submitButton.disabled) return;

        this.authModule.clearErrors();
        this.setLoading(true);

        try {
            const formData = new FormData(this.form);
            const data = {
                email: formData.get('Email'),
                password: formData.get('Password'),
                rememberMe: formData.get('RememberMe') === 'true'
            };

            // Client-side validation
            const validation = this.authModule.validateInput(data);
            if (!validation.isValid) {
                this.showErrors(validation.errors);
                return;
            }

            // Submit form normally (let server handle the redirect)
            this.form.submit();

        } catch (error) {
            this.authModule.showError(error.message);
        } finally {
            this.setLoading(false);
        }
    }

    validateField(event) {
        const field = event.target;
        const value = field.value.trim();
        const fieldName = field.name;
        
        let isValid = true;
        let errorMessage = '';

        if (!value && field.required) {
            isValid = false;
            errorMessage = `${fieldName} is required`;
        } else if (fieldName === 'Email' && value && !this.authModule.validator.patterns.email.test(value)) {
            isValid = false;
            errorMessage = 'Please enter a valid email address';
        }

        this.showFieldError(field, isValid ? '' : errorMessage);
        return isValid;
    }

    clearFieldError(event) {
        const field = event.target;
        this.showFieldError(field, '');
    }

    showFieldError(field, message) {
        const errorElement = document.getElementById(field.getAttribute('aria-describedby'));
        if (errorElement) {
            errorElement.textContent = message;
            errorElement.style.display = message ? 'block' : 'none';
        }
        
        field.classList.toggle('is-invalid', !!message);
        field.setAttribute('aria-invalid', !!message);
    }

    showErrors(errors) {
        const errorContainer = document.querySelector('.alert-danger');
        if (errorContainer && errors.length > 0) {
            errorContainer.innerHTML = errors.map(error => `<div>${this.authModule.validator.sanitizeInput(error)}</div>`).join('');
            errorContainer.style.display = 'block';
        }
    }

    togglePasswordVisibility(event) {
        const button = event.currentTarget;
        const passwordInput = button.parentElement.querySelector('input[type="password"], input[type="text"]');
        const icon = button.querySelector('i');
        
        if (passwordInput.type === 'password') {
            passwordInput.type = 'text';
            icon.className = 'icon-eye-off';
            button.setAttribute('aria-label', 'Hide password');
        } else {
            passwordInput.type = 'password';
            icon.className = 'icon-eye';
            button.setAttribute('aria-label', 'Show password');
        }
    }

    updateSubmitButton() {
        const requiredInputs = this.form.querySelectorAll('input[required]');
        const allValid = Array.from(requiredInputs).every(input => input.value.trim() !== '');
        
        this.submitButton.disabled = !allValid;
    }

    setLoading(loading) {
        const buttonText = this.submitButton.querySelector('.button-text');
        const buttonSpinner = this.submitButton.querySelector('.button-spinner');
        
        this.submitButton.disabled = loading;
        buttonText.style.display = loading ? 'none' : 'inline';
        buttonSpinner.style.display = loading ? 'inline' : 'none';
        
        if (loading) {
            this.submitButton.setAttribute('aria-busy', 'true');
        } else {
            this.submitButton.removeAttribute('aria-busy');
        }
    }
}

// Initialize when DOM is loaded
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => new LoginPage());
} else {
    new LoginPage();
}

