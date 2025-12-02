import { AuthModule } from './auth-module.js';

/**
 * Registration page functionality with password strength checker
 */
class RegisterPage {
    constructor() {
        this.authModule = new AuthModule();
        this.form = document.getElementById('registerForm');
        this.submitButton = document.getElementById('registerButton');
        this.init();
    }

    init() {
        if (!this.form) return;

        this.setupEventListeners();
        this.setupPasswordToggle();
        this.setupFormValidation();
        this.setupPasswordStrength();
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
        const requiredInputs = this.form.querySelectorAll('input[required]');
        const termsCheckbox = this.form.querySelector('input[name="AgreeToTerms"]');
        
        requiredInputs.forEach(input => {
            input.addEventListener('input', this.updateSubmitButton.bind(this));
        });
        
        if (termsCheckbox) {
            termsCheckbox.addEventListener('change', this.updateSubmitButton.bind(this));
        }
    }

    setupPasswordStrength() {
        const passwordInput = this.form.querySelector('input[name="Password"]');
        const strengthIndicator = document.getElementById('password-strength');
        
        if (passwordInput && strengthIndicator) {
            passwordInput.addEventListener('input', (e) => {
                const strength = this.checkPasswordStrength(e.target.value);
                this.updateStrengthIndicator(strengthIndicator, strength);
            });
        }
    }

    checkPasswordStrength(password) {
        let strength = 0;
        
        if (password.length >= 8) strength++;
        if (password.length >= 12) strength++;
        if (/[a-z]/.test(password)) strength++;
        if (/[A-Z]/.test(password)) strength++;
        if (/[0-9]/.test(password)) strength++;
        if (/[@$!%*?&]/.test(password)) strength++;
        
        return strength;
    }

    updateStrengthIndicator(indicator, strength) {
        const strengthLevels = [
            { label: 'Very Weak', class: 'strength-very-weak' },
            { label: 'Weak', class: 'strength-weak' },
            { label: 'Fair', class: 'strength-fair' },
            { label: 'Good', class: 'strength-good' },
            { label: 'Strong', class: 'strength-strong' },
            { label: 'Very Strong', class: 'strength-very-strong' }
        ];
        
        const level = strengthLevels[Math.min(strength, strengthLevels.length - 1)];
        indicator.textContent = `Password strength: ${level.label}`;
        indicator.className = `password-strength ${level.class}`;
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
                confirmPassword: formData.get('ConfirmPassword')
            };

            // Client-side validation
            const validation = this.authModule.validateInput(data);
            if (!validation.isValid) {
                this.showErrors(validation.errors);
                return;
            }

            // Submit form normally
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
        } else if (fieldName === 'Password' && value && !this.authModule.validator.patterns.password.test(value)) {
            isValid = false;
            errorMessage = 'Password must be at least 8 characters with uppercase, lowercase, number, and special character';
        }

        this.showFieldError(field, isValid ? '' : errorMessage);
        return isValid;
    }

    clearFieldError(event) {
        const field = event.target;
        this.showFieldError(field, '');
    }

    showFieldError(field, message) {
        const errorElement = document.getElementById(field.getAttribute('aria-describedby')?.split(' ')[0]);
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
        const termsCheckbox = this.form.querySelector('input[name="AgreeToTerms"]');
        
        const allValid = Array.from(requiredInputs).every(input => {
            if (input.type === 'checkbox') {
                return input.checked;
            }
            return input.value.trim() !== '';
        });
        
        const termsAgreed = termsCheckbox ? termsCheckbox.checked : true;
        
        this.submitButton.disabled = !(allValid && termsAgreed);
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
    document.addEventListener('DOMContentLoaded', () => new RegisterPage());
} else {
    new RegisterPage();
}

