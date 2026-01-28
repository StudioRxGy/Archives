# Notification Configuration Index

This document provides an overview of all configuration-related files and resources.

## Configuration Files

### Production-Ready Configurations

| File | Purpose | Use Case |
|------|---------|----------|
| `appsettings.Notifications.json` | Base configuration | Default settings for all environments |
| `appsettings.Notifications.Development.json` | Development settings | Local development with MailHog |
| `appsettings.Notifications.Production.json` | Production settings | Production deployment with environment variables |

### Example Configurations

| File | Purpose | Description |
|------|---------|-------------|
| `appsettings.Notifications.Example.json` | Comprehensive example | All available options with comments |
| `appsettings.Notifications.Minimal.json` | Minimal example | Bare minimum configuration |

## Documentation

### Quick References

| Document | Purpose | Audience |
|----------|---------|----------|
| `CONFIGURATION_QUICK_REFERENCE.md` | Quick setup guide | Developers needing fast setup |
| `README.Notifications.md` | Complete configuration guide | All users |
| `CONFIGURATION_INDEX.md` | This file | Navigation and overview |

### Detailed Guides

| Document | Location | Purpose |
|----------|----------|---------|
| Notification Integration Guide | `../docs/NotificationIntegrationGuide.md` | Complete integration documentation |
| API Reference | `../docs/api-reference.md` | API documentation |
| Troubleshooting Guide | `../docs/troubleshooting-guide.md` | Problem resolution |

## Validation Tools

### Command-Line Tools

| Tool | Platform | Usage |
|------|----------|-------|
| `ConfigurationValidationTool.cs` | Cross-platform (.NET) | `dotnet run --project ConfigurationValidationTool -- <config-file>` |
| `Validate-NotificationConfig.ps1` | Windows (PowerShell) | `.\Validate-NotificationConfig.ps1 -ConfigFile <config-file>` |
| `validate-notification-config.sh` | Linux/Mac (Bash) | `./validate-notification-config.sh <config-file>` |

### Programmatic Validation

```csharp
using CsPlaywrightXun.Services.Notifications;

var validator = new ConfigurationValidator(logger);
var result = validator.ValidateConfiguration(configuration);
```

See: `CsPlaywrightXun/src/playwright/Services/Notifications/ConfigurationValidator.cs`

## Quick Start Paths

### For New Users

1. Read: `CONFIGURATION_QUICK_REFERENCE.md`
2. Copy: `appsettings.Notifications.Minimal.json` → `appsettings.Notifications.json`
3. Edit: Update SMTP settings and recipients
4. Validate: Run validation tool
5. Test: Send test email

### For Advanced Users

1. Read: `README.Notifications.md`
2. Copy: `appsettings.Notifications.Example.json` → `appsettings.Notifications.json`
3. Customize: Configure all options
4. Validate: Run validation tool
5. Deploy: Use environment-specific configurations

### For Production Deployment

1. Read: `README.Notifications.md` (Environment-Specific Configuration section)
2. Copy: `appsettings.Notifications.Production.json`
3. Setup: Configure environment variables
4. Validate: Run validation tool
5. Deploy: Deploy with secret management

## Configuration Workflow

```
┌─────────────────────────────────────────────────────────────┐
│ 1. Choose Configuration Template                            │
│    - Minimal: Quick start                                   │
│    - Example: Full features                                 │
│    - Production: Secure deployment                          │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│ 2. Edit Configuration                                        │
│    - Update SMTP settings                                   │
│    - Configure recipients                                   │
│    - Define notification rules                              │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│ 3. Validate Configuration                                    │
│    - Run validation tool                                    │
│    - Fix any errors                                         │
│    - Review warnings                                        │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│ 4. Test Configuration                                        │
│    - Test SMTP connection                                   │
│    - Send test email                                        │
│    - Verify receipt                                         │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│ 5. Deploy                                                    │
│    - Use environment variables (production)                 │
│    - Configure secret management                            │
│    - Monitor logs                                           │
└─────────────────────────────────────────────────────────────┘
```

## Common Tasks

### Validate Configuration

```bash
# PowerShell (Windows)
.\Validate-NotificationConfig.ps1 -ConfigFile appsettings.Notifications.json

# Bash (Linux/Mac)
./validate-notification-config.sh appsettings.Notifications.json

# .NET Tool
dotnet run --project ConfigurationValidationTool -- appsettings.Notifications.json
```

### Test SMTP Connection

```csharp
var smtpClient = serviceProvider.GetRequiredService<ISmtpClient>();
var canConnect = await smtpClient.TestConnectionAsync();
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
await smtpClient.SendEmailAsync(message);
```

## Configuration by Scenario

### Scenario 1: Local Development

**Goal**: Test notifications locally without sending real emails

**Configuration**: `appsettings.Notifications.Development.json`

**Setup**:
1. Install MailHog: `brew install mailhog` (Mac) or download from GitHub
2. Run MailHog: `mailhog`
3. View emails: http://localhost:8025
4. Use configuration with `Host: localhost`, `Port: 1025`, `EnableSsl: false`

### Scenario 2: Gmail Integration

**Goal**: Send notifications via Gmail

**Configuration**: Custom configuration based on `appsettings.Notifications.Example.json`

**Setup**:
1. Enable 2-Factor Authentication on Gmail
2. Generate App Password: https://myaccount.google.com/apppasswords
3. Use `Host: smtp.gmail.com`, `Port: 587`, `EnableSsl: true`
4. Use App Password (not account password)

### Scenario 3: Production Deployment

**Goal**: Secure production deployment with environment variables

**Configuration**: `appsettings.Notifications.Production.json`

**Setup**:
1. Use environment variables for sensitive data
2. Configure secret management (Azure Key Vault, AWS Secrets Manager)
3. Enable SSL/TLS
4. Set up monitoring and alerting
5. Configure appropriate cooldown periods

### Scenario 4: Multiple Environments

**Goal**: Different configurations for dev, staging, and production

**Configuration**: Multiple environment-specific files

**Setup**:
1. Create `appsettings.Notifications.{Environment}.json` for each environment
2. Use environment variable: `ASPNETCORE_ENVIRONMENT`
3. Configure different recipients per environment
4. Use different SMTP servers if needed

## Troubleshooting

### Configuration Not Loading

**Check**:
1. File exists in correct location
2. File name matches pattern: `appsettings.Notifications.*.json`
3. JSON syntax is valid
4. Environment variable is set correctly

**Solution**: Run validation tool to identify issues

### SMTP Connection Failed

**Check**:
1. Host and port are correct
2. Firewall allows SMTP port
3. SSL/TLS setting matches server
4. Credentials are correct

**Solution**: Use `TestConnectionAsync()` method

### Emails Not Received

**Check**:
1. Spam/junk folder
2. Email addresses are correct
3. Notification rules are enabled
4. Not in cooldown period

**Solution**: Check logs and validate configuration

## Additional Resources

- [Notification Integration Guide](../docs/NotificationIntegrationGuide.md) - Complete integration documentation
- [Configuration Quick Reference](CONFIGURATION_QUICK_REFERENCE.md) - Fast setup guide
- [Configuration README](README.Notifications.md) - Detailed configuration guide
- [API Reference](../docs/api-reference.md) - API documentation
- [Troubleshooting Guide](../docs/troubleshooting-guide.md) - Problem resolution

## Support

For issues or questions:

1. Check the troubleshooting section in documentation
2. Run validation tool to identify configuration errors
3. Review logs for detailed error messages
4. Consult the integration guide for setup instructions

## Version History

- **v1.0.0** (2026-01-14) - Initial release
  - Configuration files
  - Validation tools
  - Documentation
  - Examples
