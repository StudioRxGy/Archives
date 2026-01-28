# Notification Configuration Quick Reference

## Quick Start

### 1. Copy Example Configuration

```bash
cp appsettings.Notifications.Example.json appsettings.Notifications.json
```

### 2. Update SMTP Settings

```json
{
  "Notifications": {
    "Smtp": {
      "Host": "your-smtp-server.com",
      "Port": 587,
      "Username": "your-email@example.com",
      "Password": "your-password",
      "FromEmail": "noreply@example.com"
    }
  }
}
```

### 3. Update Recipients

```json
{
  "Notifications": {
    "DefaultRecipients": [
      "your-team@example.com"
    ]
  }
}
```

### 4. Validate Configuration

```bash
dotnet run --project ConfigurationValidationTool -- appsettings.Notifications.json
```

## Common Configurations

### Gmail

```json
{
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "EnableSsl": true,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "FromEmail": "your-email@gmail.com"
  }
}
```

**Setup Steps:**
1. Enable 2-Factor Authentication
2. Generate App Password: https://myaccount.google.com/apppasswords
3. Use App Password (not account password)

### Outlook/Office 365

```json
{
  "Smtp": {
    "Host": "smtp.office365.com",
    "Port": 587,
    "EnableSsl": true,
    "Username": "your-email@outlook.com",
    "Password": "your-password",
    "FromEmail": "your-email@outlook.com"
  }
}
```

### SendGrid

```json
{
  "Smtp": {
    "Host": "smtp.sendgrid.net",
    "Port": 587,
    "EnableSsl": true,
    "Username": "apikey",
    "Password": "SG.your-api-key",
    "FromEmail": "noreply@yourdomain.com"
  }
}
```

**Setup Steps:**
1. Create SendGrid account
2. Generate API Key
3. Use "apikey" as username
4. Use API key as password

### Local Development (MailHog)

```json
{
  "Smtp": {
    "Host": "localhost",
    "Port": 1025,
    "EnableSsl": false,
    "Username": "",
    "Password": "",
    "FromEmail": "dev@localhost"
  }
}
```

**Setup Steps:**
1. Install MailHog: `brew install mailhog` (Mac) or download from GitHub
2. Run: `mailhog`
3. View emails: http://localhost:8025

## Notification Rules

### Notify on All Failures

```json
{
  "Rules": [
    {
      "Id": "all-failures",
      "Type": "TestFailure",
      "Recipients": ["team@example.com"],
      "IsEnabled": true
    }
  ]
}
```

### Notify Only Critical Failures

```json
{
  "Rules": [
    {
      "Id": "critical-only",
      "Type": "CriticalFailure",
      "Recipients": ["manager@example.com"],
      "IsEnabled": true
    }
  ]
}
```

### Notify with Cooldown (Prevent Spam)

```json
{
  "Rules": [
    {
      "Id": "success-with-cooldown",
      "Type": "TestSuccess",
      "Recipients": ["team@example.com"],
      "IsEnabled": true,
      "CooldownPeriod": "00:15:00"
    }
  ]
}
```

### Conditional Notifications

```json
{
  "Rules": [
    {
      "Id": "prod-failures-only",
      "Type": "TestFailure",
      "Recipients": ["oncall@example.com"],
      "IsEnabled": true,
      "Conditions": {
        "Environment": "Production"
      }
    }
  ]
}
```

## Environment Variables

### Production Setup

**Configuration File:**
```json
{
  "Smtp": {
    "Host": "${SMTP_HOST}",
    "Port": 587,
    "Username": "${SMTP_USERNAME}",
    "Password": "${SMTP_PASSWORD}",
    "FromEmail": "${SMTP_FROM_EMAIL}"
  }
}
```

**Set Environment Variables:**

**Linux/Mac:**
```bash
export SMTP_HOST=smtp.example.com
export SMTP_USERNAME=your-email@example.com
export SMTP_PASSWORD=your-password
export SMTP_FROM_EMAIL=noreply@example.com
```

**Windows (PowerShell):**
```powershell
$env:SMTP_HOST="smtp.example.com"
$env:SMTP_USERNAME="your-email@example.com"
$env:SMTP_PASSWORD="your-password"
$env:SMTP_FROM_EMAIL="noreply@example.com"
```

**Windows (CMD):**
```cmd
set SMTP_HOST=smtp.example.com
set SMTP_USERNAME=your-email@example.com
set SMTP_PASSWORD=your-password
set SMTP_FROM_EMAIL=noreply@example.com
```

## Validation Checklist

- [ ] SMTP host is correct
- [ ] SMTP port is correct (25, 587, or 465)
- [ ] SSL/TLS is enabled for production
- [ ] From email is valid
- [ ] All recipient emails are valid
- [ ] No duplicate rule IDs
- [ ] Cooldown periods are in correct format (HH:MM:SS)
- [ ] Configuration file is valid JSON
- [ ] Sensitive data uses environment variables (production)

## Troubleshooting

### Cannot Connect to SMTP Server

**Check:**
1. Host and port are correct
2. Firewall allows SMTP port
3. SSL/TLS setting matches server requirements
4. Credentials are correct

**Test:**
```bash
telnet smtp.example.com 587
```

### Authentication Failed

**Check:**
1. Username and password are correct
2. Using app password (Gmail, Outlook)
3. Account allows SMTP access
4. 2FA is configured correctly

### Emails Not Received

**Check:**
1. Spam/junk folder
2. Email addresses are correct
3. Notification rules are enabled
4. Not in cooldown period
5. Conditions are met

### Configuration Validation Errors

**Run validation tool:**
```bash
dotnet run --project ConfigurationValidationTool -- appsettings.Notifications.json
```

**Common errors:**
- Invalid email format
- Port out of range
- Missing required fields
- Duplicate rule IDs
- Invalid cooldown format

## Testing

### Test Configuration

```bash
# Validate configuration
dotnet run --project ConfigurationValidationTool -- appsettings.Notifications.json
```

### Test SMTP Connection

```csharp
var smtpClient = serviceProvider.GetRequiredService<ISmtpClient>();
var canConnect = await smtpClient.TestConnectionAsync();
Console.WriteLine(canConnect ? "✓ Connected" : "✗ Failed");
```

### Send Test Email

```csharp
var message = new EmailMessage
{
    ToAddresses = new List<string> { "test@example.com" },
    Subject = "Test Email",
    HtmlBody = "<p>This is a test</p>"
};

var smtpClient = serviceProvider.GetRequiredService<ISmtpClient>();
var sent = await smtpClient.SendEmailAsync(message);
Console.WriteLine(sent ? "✓ Sent" : "✗ Failed");
```

## Security Best Practices

1. ✓ Use environment variables for passwords
2. ✓ Enable SSL/TLS in production
3. ✓ Use app-specific passwords
4. ✓ Restrict config file permissions
5. ✓ Rotate credentials regularly
6. ✓ Never commit passwords to git
7. ✓ Use secret management services (Azure Key Vault, AWS Secrets Manager)

## Additional Resources

- [Full Configuration Guide](README.Notifications.md)
- [Integration Guide](../docs/NotificationIntegrationGuide.md)
- [Troubleshooting Guide](../docs/troubleshooting-guide.md)
