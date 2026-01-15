# Notification Configuration Guide

This directory contains configuration files for the email notification system.

## Configuration Files

### Available Configuration Files

- **appsettings.Notifications.json** - Base notification configuration
- **appsettings.Notifications.Development.json** - Development environment settings
- **appsettings.Notifications.Production.json** - Production environment settings
- **appsettings.Notifications.Example.json** - Comprehensive example with all options
- **appsettings.Notifications.Minimal.json** - Minimal configuration example

### Configuration File Priority

Configuration files are loaded in the following order (later files override earlier ones):

1. `appsettings.json`
2. `appsettings.Notifications.json`
3. `appsettings.Notifications.{Environment}.json`
4. Environment variables

## Configuration Structure

### Root Configuration

```json
{
  "Notifications": {
    "Enabled": true,
    "Smtp": { ... },
    "DefaultRecipients": [ ... ],
    "Rules": [ ... ]
  }
}
```

### SMTP Configuration

```json
{
  "Smtp": {
    "Host": "smtp.example.com",
    "Port": 587,
    "EnableSsl": true,
    "Username": "your-email@example.com",
    "Password": "your-password",
    "FromEmail": "noreply@example.com",
    "FromDisplayName": "Test Automation Framework",
    "TimeoutSeconds": 30,
    "MaxRetryAttempts": 3
  }
}
```

#### SMTP Configuration Options

| Option | Type | Required | Default | Description |
|--------|------|----------|---------|-------------|
| Host | string | Yes | - | SMTP server hostname or IP address |
| Port | int | Yes | 587 | SMTP server port (25, 587, 465) |
| EnableSsl | bool | No | true | Enable SSL/TLS encryption |
| Username | string | No | "" | SMTP authentication username |
| Password | string | No | "" | SMTP authentication password |
| FromEmail | string | Yes | - | Sender email address |
| FromDisplayName | string | No | "" | Sender display name |
| TimeoutSeconds | int | No | 30 | Connection timeout (5-300 seconds) |
| MaxRetryAttempts | int | No | 3 | Retry attempts on failure (0-10) |

### Notification Rules

```json
{
  "Rules": [
    {
      "Id": "test-failure-notification",
      "Type": "TestFailure",
      "Recipients": [
        "qa-team@example.com",
        "dev-team@example.com"
      ],
      "IsEnabled": true,
      "CooldownPeriod": "00:05:00",
      "Conditions": {
        "Environment": "Production"
      }
    }
  ]
}
```

#### Rule Configuration Options

| Option | Type | Required | Default | Description |
|--------|------|----------|---------|-------------|
| Id | string | Yes | - | Unique identifier for the rule |
| Type | enum | Yes | - | Notification type (see below) |
| Recipients | string[] | Yes | - | List of recipient email addresses |
| IsEnabled | bool | No | true | Enable or disable this rule |
| CooldownPeriod | TimeSpan? | No | null | Minimum time between notifications |
| Conditions | object | No | {} | Additional trigger conditions |

#### Notification Types

- **TestStart** - Sent when test execution begins
- **TestSuccess** - Sent when all tests pass
- **TestFailure** - Sent when any test fails
- **CriticalFailure** - Sent immediately for critical test failures
- **ReportGenerated** - Sent when test report is generated

### Cooldown Period Format

Cooldown periods use the TimeSpan format: `HH:MM:SS`

Examples:
- `"00:05:00"` - 5 minutes
- `"00:15:00"` - 15 minutes
- `"01:00:00"` - 1 hour
- `null` - No cooldown

## Environment-Specific Configuration

### Development Environment

Use local SMTP server (e.g., MailHog) for testing:

```json
{
  "Notifications": {
    "Smtp": {
      "Host": "localhost",
      "Port": 1025,
      "EnableSsl": false,
      "Username": "",
      "Password": "",
      "FromEmail": "dev-tests@localhost"
    }
  }
}
```

### Production Environment

Use environment variables for sensitive data:

```json
{
  "Notifications": {
    "Smtp": {
      "Host": "${SMTP_HOST}",
      "Port": 587,
      "EnableSsl": true,
      "Username": "${SMTP_USERNAME}",
      "Password": "${SMTP_PASSWORD}",
      "FromEmail": "${SMTP_FROM_EMAIL}"
    }
  }
}
```

Set environment variables:
```bash
export SMTP_HOST=smtp.example.com
export SMTP_USERNAME=your-email@example.com
export SMTP_PASSWORD=your-password
export SMTP_FROM_EMAIL=noreply@example.com
```

## Common SMTP Providers

### Gmail

```json
{
  "Host": "smtp.gmail.com",
  "Port": 587,
  "EnableSsl": true,
  "Username": "your-email@gmail.com",
  "Password": "your-app-password"
}
```

**Note**: Use App Password, not account password. Enable 2FA and generate an app password at: https://myaccount.google.com/apppasswords

### Outlook/Office 365

```json
{
  "Host": "smtp.office365.com",
  "Port": 587,
  "EnableSsl": true,
  "Username": "your-email@outlook.com",
  "Password": "your-password"
}
```

### SendGrid

```json
{
  "Host": "smtp.sendgrid.net",
  "Port": 587,
  "EnableSsl": true,
  "Username": "apikey",
  "Password": "your-sendgrid-api-key"
}
```

### Amazon SES

```json
{
  "Host": "email-smtp.us-east-1.amazonaws.com",
  "Port": 587,
  "EnableSsl": true,
  "Username": "your-smtp-username",
  "Password": "your-smtp-password"
}
```

### MailHog (Development)

```json
{
  "Host": "localhost",
  "Port": 1025,
  "EnableSsl": false,
  "Username": "",
  "Password": ""
}
```

## Configuration Validation

### Using the Validation Tool

Validate your configuration file before deployment:

#### C# Validation Tool

```bash
dotnet run --project ConfigurationValidationTool -- appsettings.Notifications.json
```

#### PowerShell Script (Windows)

```powershell
.\Validate-NotificationConfig.ps1 -ConfigFile appsettings.Notifications.json

# With verbose output
.\Validate-NotificationConfig.ps1 -ConfigFile appsettings.Notifications.json -Verbose
```

#### Bash Script (Linux/Mac)

```bash
# Make script executable (first time only)
chmod +x validate-notification-config.sh

# Run validation
./validate-notification-config.sh appsettings.Notifications.json
```

The validation tools check:
- ✓ JSON syntax validity
- ✓ Required fields presence
- ✓ Email address formats
- ✓ Port numbers and timeout values
- ✓ Duplicate rule IDs
- ✓ Cooldown period formats
- ✓ Environment variable usage

### Programmatic Validation

```csharp
using Microsoft.Extensions.Configuration;
using CsPlaywrightXun.Services.Notifications;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.Notifications.json")
    .Build();

var validator = new ConfigurationValidator(logger);
var result = validator.ValidateConfiguration(configuration);

if (!result.IsValid)
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"Error: {error}");
    }
}
```

## Troubleshooting

### Common Configuration Errors

1. **Invalid email format**
   - Error: "From Email must be a valid email address"
   - Solution: Ensure email addresses follow the format: `user@domain.com`

2. **Invalid port number**
   - Error: "Port must be between 1 and 65535"
   - Solution: Use standard SMTP ports: 25, 587, or 465

3. **Missing required fields**
   - Error: "SMTP Host is required"
   - Solution: Ensure all required fields are present and not empty

4. **Duplicate rule IDs**
   - Error: "Duplicate rule ID found: test-failure"
   - Solution: Ensure each rule has a unique ID

5. **Invalid cooldown period**
   - Error: "Cooldown period cannot be negative"
   - Solution: Use valid TimeSpan format or null

### Testing Configuration

1. **Validate configuration file**
   ```bash
   dotnet run --project ConfigurationValidationTool -- your-config.json
   ```

2. **Test SMTP connection**
   ```csharp
   var smtpClient = serviceProvider.GetRequiredService<ISmtpClient>();
   var canConnect = await smtpClient.TestConnectionAsync();
   ```

3. **Send test email**
   ```csharp
   var notificationService = serviceProvider.GetRequiredService<IEmailNotificationService>();
   await notificationService.SendTestStartNotificationAsync(context);
   ```

## Security Best Practices

1. **Never commit passwords** - Use environment variables or secret management
2. **Enable SSL/TLS** - Always use encrypted connections in production
3. **Use app passwords** - For services like Gmail, use app-specific passwords
4. **Restrict file permissions** - Limit access to configuration files
5. **Rotate credentials** - Regularly update SMTP passwords
6. **Use secret management** - Consider Azure Key Vault or AWS Secrets Manager

## Examples

### Minimal Configuration

See: `appsettings.Notifications.Minimal.json`

### Full Configuration

See: `appsettings.Notifications.Example.json`

### Development Configuration

See: `appsettings.Notifications.Development.json`

### Production Configuration

See: `appsettings.Notifications.Production.json`

## Additional Resources

- [Notification Integration Guide](../docs/NotificationIntegrationGuide.md)
- [SMTP Configuration Reference](../docs/api-reference.md)
- [Troubleshooting Guide](../docs/troubleshooting-guide.md)
