# Email Service Configuration

The Simple Identity Server includes a production-ready email service that supports SMTP email delivery. The service can operate in two modes:

1. **Development Mode**: Emails are logged to the console instead of being sent
2. **Production Mode**: Emails are sent via SMTP

## Configuration Methods

You can configure the email service using either:
- **appsettings.json** (for local development)
- **Environment Variables** (for production/containerized deployments)

Environment variables take precedence over appsettings.json values.

## Configuration Options

### appsettings.json Configuration

Add the following section to your `appsettings.json` or `appsettings.Production.json`:

```json
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "EnableSsl": true,
    "Username": "your-email@example.com",
    "Password": "your-app-password",
    "FromEmail": "noreply@simpleidentityserver.com",
    "FromName": "Simple Identity Server",
    "DevelopmentMode": false,
    "TimeoutSeconds": 30
  }
}
```

### Environment Variables Configuration

For production deployments, use environment variables to keep sensitive credentials secure:

| Environment Variable | Description | Required | Example |
|---------------------|-------------|----------|---------|
| `SIMPLE_IDENTITY_SERVER_EMAIL_SMTP_HOST` | SMTP server hostname | Yes (in production) | `smtp.gmail.com` |
| `SIMPLE_IDENTITY_SERVER_EMAIL_SMTP_PORT` | SMTP server port | No (defaults to 587) | `587` |
| `SIMPLE_IDENTITY_SERVER_EMAIL_USERNAME` | SMTP authentication username | Yes (in production) | `your-email@example.com` |
| `SIMPLE_IDENTITY_SERVER_EMAIL_PASSWORD` | SMTP authentication password | Yes (in production) | `your-app-password` |
| `SIMPLE_IDENTITY_SERVER_EMAIL_FROM_ADDRESS` | Sender email address | Yes | `noreply@example.com` |
| `SIMPLE_IDENTITY_SERVER_EMAIL_FROM_NAME` | Sender display name | No | `Simple Identity Server` |
| `SIMPLE_IDENTITY_SERVER_EMAIL_DEVELOPMENT_MODE` | Enable development mode | No (defaults to true) | `false` |

## SMTP Provider Examples

### Gmail

1. Enable 2-factor authentication on your Google account
2. Generate an App Password: [Google App Passwords](https://myaccount.google.com/apppasswords)
3. Configuration:
   - **SmtpHost**: `smtp.gmail.com`
   - **SmtpPort**: `587`
   - **EnableSsl**: `true`
   - **Username**: Your Gmail address
   - **Password**: Your App Password

### Microsoft 365 / Outlook

1. Configuration:
   - **SmtpHost**: `smtp.office365.com`
   - **SmtpPort**: `587`
   - **EnableSsl**: `true`
   - **Username**: Your Microsoft 365 email address
   - **Password**: Your account password

### SendGrid

1. Create an API key in SendGrid dashboard
2. Configuration:
   - **SmtpHost**: `smtp.sendgrid.net`
   - **SmtpPort**: `587`
   - **EnableSsl**: `true`
   - **Username**: `apikey` (literally the word "apikey")
   - **Password**: Your SendGrid API key

### Amazon SES

1. Verify your sending email address or domain in AWS SES
2. Create SMTP credentials in AWS SES console
3. Configuration:
   - **SmtpHost**: `email-smtp.[region].amazonaws.com` (e.g., `email-smtp.us-east-1.amazonaws.com`)
   - **SmtpPort**: `587`
   - **EnableSsl**: `true`
   - **Username**: Your SMTP username from AWS
   - **Password**: Your SMTP password from AWS

### Mailgun

1. Get your SMTP credentials from Mailgun dashboard
2. Configuration:
   - **SmtpHost**: `smtp.mailgun.org`
   - **SmtpPort**: `587`
   - **EnableSsl**: `true`
   - **Username**: Your Mailgun SMTP username
   - **Password**: Your Mailgun SMTP password

## Development Mode

In development mode (`DevelopmentMode: true`), emails are not actually sent. Instead, they are logged to the console with full details including the recipient, subject, and body.

This is useful for:
- Local development without SMTP configuration
- Testing email templates and content
- Debugging email flows

To enable development mode:

**appsettings.json:**
```json
{
  "Email": {
    "DevelopmentMode": true,
    "FromEmail": "noreply@simpleidentityserver.com"
  }
}
```

**Environment Variable:**
```bash
export SIMPLE_IDENTITY_SERVER_EMAIL_DEVELOPMENT_MODE=true
```

## Production Mode

For production, set `DevelopmentMode: false` and provide all required SMTP settings:

**Environment Variables (Recommended):**
```bash
export SIMPLE_IDENTITY_SERVER_EMAIL_SMTP_HOST=smtp.gmail.com
export SIMPLE_IDENTITY_SERVER_EMAIL_SMTP_PORT=587
export SIMPLE_IDENTITY_SERVER_EMAIL_USERNAME=your-email@example.com
export SIMPLE_IDENTITY_SERVER_EMAIL_PASSWORD=your-app-password
export SIMPLE_IDENTITY_SERVER_EMAIL_FROM_ADDRESS=noreply@example.com
export SIMPLE_IDENTITY_SERVER_EMAIL_FROM_NAME="Simple Identity Server"
export SIMPLE_IDENTITY_SERVER_EMAIL_DEVELOPMENT_MODE=false
```

## Docker Compose Example

```yaml
services:
  identity-server:
    image: simple-identity-server:latest
    environment:
      - SIMPLE_IDENTITY_SERVER_EMAIL_SMTP_HOST=smtp.gmail.com
      - SIMPLE_IDENTITY_SERVER_EMAIL_SMTP_PORT=587
      - SIMPLE_IDENTITY_SERVER_EMAIL_USERNAME=your-email@example.com
      - SIMPLE_IDENTITY_SERVER_EMAIL_PASSWORD=your-app-password
      - SIMPLE_IDENTITY_SERVER_EMAIL_FROM_ADDRESS=noreply@example.com
      - SIMPLE_IDENTITY_SERVER_EMAIL_FROM_NAME=Simple Identity Server
      - SIMPLE_IDENTITY_SERVER_EMAIL_DEVELOPMENT_MODE=false
```

## Kubernetes Secret Example

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: email-credentials
type: Opaque
stringData:
  smtp-host: smtp.gmail.com
  smtp-port: "587"
  smtp-username: your-email@example.com
  smtp-password: your-app-password
  from-address: noreply@example.com
  from-name: "Simple Identity Server"
  development-mode: "false"
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: identity-server
spec:
  template:
    spec:
      containers:
      - name: identity-server
        image: simple-identity-server:latest
        env:
        - name: SIMPLE_IDENTITY_SERVER_EMAIL_SMTP_HOST
          valueFrom:
            secretKeyRef:
              name: email-credentials
              key: smtp-host
        - name: SIMPLE_IDENTITY_SERVER_EMAIL_SMTP_PORT
          valueFrom:
            secretKeyRef:
              name: email-credentials
              key: smtp-port
        - name: SIMPLE_IDENTITY_SERVER_EMAIL_USERNAME
          valueFrom:
            secretKeyRef:
              name: email-credentials
              key: smtp-username
        - name: SIMPLE_IDENTITY_SERVER_EMAIL_PASSWORD
          valueFrom:
            secretKeyRef:
              name: email-credentials
              key: smtp-password
        - name: SIMPLE_IDENTITY_SERVER_EMAIL_FROM_ADDRESS
          valueFrom:
            secretKeyRef:
              name: email-credentials
              key: from-address
        - name: SIMPLE_IDENTITY_SERVER_EMAIL_FROM_NAME
          valueFrom:
            secretKeyRef:
              name: email-credentials
              key: from-name
        - name: SIMPLE_IDENTITY_SERVER_EMAIL_DEVELOPMENT_MODE
          valueFrom:
            secretKeyRef:
              name: email-credentials
              key: development-mode
```

## Email Templates

The service includes two built-in email templates with responsive HTML:

1. **Email Confirmation**: Sent when a user registers a new account
2. **Password Reset**: Sent when a user requests to reset their password

Both templates include:
- Professional HTML design with CSS styling
- Plain text fallback for email clients that don't support HTML
- Security notices and best practices
- Branded with your configured `FromName`

## Troubleshooting

### Emails Not Being Sent

1. **Check Development Mode**: Ensure `DevelopmentMode` is set to `false`
2. **Verify SMTP Credentials**: Test your SMTP credentials with an email client
3. **Check Firewall**: Ensure outbound connections on port 587 (or your configured port) are allowed
4. **Review Logs**: Check application logs for detailed error messages

### Gmail "Less Secure Apps" Error

Google has deprecated "less secure apps" access. You must:
1. Enable 2-factor authentication
2. Use an App Password instead of your regular password

### Authentication Failures

- Verify your username and password are correct
- Some providers require the full email address as the username
- Check if your account needs special SMTP permissions

### SSL/TLS Errors

- Ensure `EnableSsl` is set correctly for your provider
- Most modern SMTP servers use port 587 with `StartTLS`
- Port 465 typically uses SSL from the start
- Port 25 is usually unencrypted (not recommended for production)

## Security Best Practices

1. **Never commit credentials** to source control
2. **Use environment variables** in production
3. **Use App Passwords** when available (Gmail, Microsoft)
4. **Enable 2FA** on email accounts used for SMTP
5. **Restrict SMTP credentials** to email sending only
6. **Monitor email sending** for unusual activity
7. **Use dedicated email addresses** (e.g., `noreply@yourdomain.com`)
8. **Set appropriate timeout values** to prevent hanging connections

## Required NuGet Packages

The email service uses the following packages (already included):

- **MailKit**: Cross-platform SMTP client library
- **MimeKit**: MIME message creation and parsing

These are industry-standard, actively maintained libraries for .NET email handling.

