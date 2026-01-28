using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using CsPlaywrightXun.Services.Notifications;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CsPlaywrightXun.Core.Configuration
{
    /// <summary>
    /// Configuration extension methods for notification services
    /// </summary>
    public static class NotificationServiceConfiguration
    {
        /// <summary>
        /// Add notification services to the dependency injection container
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration instance</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddNotificationServices(
            this IServiceCollection services, 
            IConfiguration configuration)
        {
            // Add logging services if not already registered
            services.AddLogging();

            // Configure SMTP settings from configuration
            services.Configure<SmtpConfiguration>(
                configuration.GetSection("Notifications:Smtp"));

            // Configure notification rules from configuration
            services.Configure<NotificationSettings>(
                configuration.GetSection("Notifications"));

            // Register notification service interfaces
            // Note: Implementations will be added in subsequent tasks
            services.AddScoped<IEmailNotificationService, EmailNotificationService>();
            services.AddScoped<ISmtpClient, SmtpClient>();
            services.AddScoped<IEmailTemplateEngine, EmailTemplateEngine>();
            services.AddScoped<IRecipientManager, RecipientManager>();

            return services;
        }

        /// <summary>
        /// Validate notification configuration
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection ValidateNotificationConfiguration(
            this IServiceCollection services)
        {
            // Add enhanced configuration validation for SMTP
            services.AddOptions<SmtpConfiguration>()
                .Validate(config => !string.IsNullOrWhiteSpace(config.Host), 
                    "SMTP Host is required")
                .Validate(config => config.Port > 0 && config.Port <= 65535, 
                    "SMTP Port must be between 1 and 65535")
                .Validate(config => !string.IsNullOrWhiteSpace(config.FromEmail), 
                    "From Email is required")
                .Validate(config => config.TimeoutSeconds >= 5 && config.TimeoutSeconds <= 300,
                    "Timeout must be between 5 and 300 seconds")
                .Validate(config => config.MaxRetryAttempts >= 0 && config.MaxRetryAttempts <= 10,
                    "Max retry attempts must be between 0 and 10")
                .Validate(config => config.IsValid(),
                    "SMTP configuration validation failed");

            // Add validation for notification settings
            services.AddOptions<NotificationSettings>()
                .Validate(settings => settings.Smtp != null,
                    "SMTP configuration is required")
                .Validate(settings => settings.Rules != null,
                    "Notification rules collection is required")
                .Validate(settings => ValidateNotificationRules(settings.Rules),
                    "One or more notification rules are invalid")
                .Validate(settings => ValidateDefaultRecipients(settings.DefaultRecipients),
                    "One or more default recipients are invalid");

            return services;
        }

        /// <summary>
        /// Validates a collection of notification rules
        /// </summary>
        /// <param name="rules">Rules to validate</param>
        /// <returns>True if all rules are valid</returns>
        private static bool ValidateNotificationRules(List<NotificationRule> rules)
        {
            if (rules == null)
                return false;

            foreach (var rule in rules)
            {
                if (!rule.IsValid())
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Validates default recipient email addresses
        /// </summary>
        /// <param name="recipients">Recipients to validate</param>
        /// <returns>True if all recipients are valid</returns>
        private static bool ValidateDefaultRecipients(List<string> recipients)
        {
            if (recipients == null)
                return true; // Default recipients are optional

            foreach (var recipient in recipients)
            {
                if (string.IsNullOrWhiteSpace(recipient))
                    return false;

                try
                {
                    var emailAddress = new System.Net.Mail.MailAddress(recipient);
                }
                catch (System.FormatException)
                {
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Notification settings configuration
    /// </summary>
    public class NotificationSettings
    {
        /// <summary>
        /// SMTP configuration
        /// </summary>
        [Required(ErrorMessage = "SMTP configuration is required")]
        public SmtpConfiguration Smtp { get; set; } = new();

        /// <summary>
        /// Notification rules
        /// </summary>
        [Required(ErrorMessage = "Notification rules are required")]
        public List<NotificationRule> Rules { get; set; } = new();

        /// <summary>
        /// Default recipients for all notifications
        /// </summary>
        public List<string> DefaultRecipients { get; set; } = new();

        /// <summary>
        /// Whether notifications are enabled globally
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Validates the notification settings
        /// </summary>
        /// <returns>True if settings are valid, false otherwise</returns>
        public bool IsValid()
        {
            try
            {
                // Validate SMTP configuration
                if (Smtp == null || !Smtp.IsValid())
                    return false;

                // Validate notification rules
                if (Rules == null)
                    return false;

                foreach (var rule in Rules)
                {
                    if (!rule.IsValid())
                        return false;
                }

                // Validate default recipients if any
                if (DefaultRecipients != null)
                {
                    foreach (var recipient in DefaultRecipients)
                    {
                        if (string.IsNullOrWhiteSpace(recipient))
                            return false;

                        try
                        {
                            var emailAddress = new System.Net.Mail.MailAddress(recipient);
                        }
                        catch (System.FormatException)
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Gets validation errors for the notification settings
        /// </summary>
        /// <returns>List of validation error messages</returns>
        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();

            if (Smtp == null)
            {
                errors.Add("SMTP configuration is required");
            }
            else
            {
                var smtpErrors = Smtp.GetValidationErrors();
                errors.AddRange(smtpErrors);
            }

            if (Rules == null)
            {
                errors.Add("Notification rules are required");
            }
            else
            {
                for (int i = 0; i < Rules.Count; i++)
                {
                    var rule = Rules[i];
                    var ruleErrors = rule.GetValidationErrors();
                    foreach (var error in ruleErrors)
                    {
                        errors.Add($"Rule {i}: {error}");
                    }
                }
            }

            if (DefaultRecipients != null)
            {
                for (int i = 0; i < DefaultRecipients.Count; i++)
                {
                    var recipient = DefaultRecipients[i];
                    if (string.IsNullOrWhiteSpace(recipient))
                    {
                        errors.Add($"Default recipient at index {i} cannot be empty");
                    }
                    else
                    {
                        try
                        {
                            var emailAddress = new System.Net.Mail.MailAddress(recipient);
                        }
                        catch (System.FormatException)
                        {
                            errors.Add($"Default recipient '{recipient}' is not a valid email address");
                        }
                    }
                }
            }

            return errors;
        }
    }

    // Placeholder classes for the actual implementations that will be created in later tasks
    internal class EmailNotificationService : IEmailNotificationService
    {
        public Task<NotificationStatus> GetNotificationStatusAsync(string notificationId)
        {
            throw new NotImplementedException("Implementation will be added in task 7");
        }

        public Task SendReportGeneratedNotificationAsync(ReportInfo reportInfo)
        {
            throw new NotImplementedException("Implementation will be added in task 7");
        }

        public Task SendTestFailureNotificationAsync(TestSuiteResult result)
        {
            throw new NotImplementedException("Implementation will be added in task 7");
        }

        public Task SendTestStartNotificationAsync(TestExecutionContext context)
        {
            throw new NotImplementedException("Implementation will be added in task 7");
        }

        public Task SendTestSuccessNotificationAsync(TestSuiteResult result)
        {
            throw new NotImplementedException("Implementation will be added in task 7");
        }

        public Task<bool> ValidateSmtpConfigurationAsync()
        {
            throw new NotImplementedException("Implementation will be added in task 7");
        }
    }

    internal class SmtpClient : ISmtpClient
    {
        private SmtpConfiguration? _configuration;
        private readonly ILogger<SmtpClient> _logger;

        public SmtpClient(ILogger<SmtpClient> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Configure(SmtpConfiguration config)
        {
            _configuration = config ?? throw new ArgumentNullException(nameof(config));
            
            if (!_configuration.IsValid())
            {
                var errors = _configuration.GetValidationErrors();
                var errorMessage = string.Join(", ", errors);
                _logger.LogError("Invalid SMTP configuration: {Errors}", errorMessage);
                throw new ArgumentException($"Invalid SMTP configuration: {errorMessage}", nameof(config));
            }

            _logger.LogInformation("SMTP client configured for host {Host}:{Port}", _configuration.Host, _configuration.Port);
        }

        public async Task<bool> SendEmailAsync(EmailMessage message)
        {
            if (_configuration == null)
            {
                _logger.LogError("SMTP client not configured");
                return false;
            }

            if (message == null)
            {
                _logger.LogError("Email message is null");
                return false;
            }

            if (!message.IsValid())
            {
                var errors = message.GetValidationErrors();
                _logger.LogError("Invalid email message: {Errors}", string.Join(", ", errors));
                return false;
            }

            var retryCount = 0;
            var maxRetries = _configuration.MaxRetryAttempts;

            while (retryCount <= maxRetries)
            {
                try
                {
                    using var client = new System.Net.Mail.SmtpClient(_configuration.Host, _configuration.Port);
                    
                    // Configure SSL/TLS
                    client.EnableSsl = _configuration.EnableSsl;
                    client.Timeout = _configuration.TimeoutSeconds * 1000; // Convert to milliseconds

                    // Configure authentication if credentials are provided
                    if (!string.IsNullOrEmpty(_configuration.Username) && !string.IsNullOrEmpty(_configuration.Password))
                    {
                        client.Credentials = new System.Net.NetworkCredential(_configuration.Username, _configuration.Password);
                    }

                    // Create mail message
                    using var mailMessage = new System.Net.Mail.MailMessage();
                    
                    // Set sender
                    mailMessage.From = string.IsNullOrEmpty(_configuration.FromDisplayName) 
                        ? new System.Net.Mail.MailAddress(_configuration.FromEmail)
                        : new System.Net.Mail.MailAddress(_configuration.FromEmail, _configuration.FromDisplayName);

                    // Add recipients
                    foreach (var toAddress in message.ToAddresses)
                    {
                        mailMessage.To.Add(toAddress);
                    }

                    // Add CC recipients if any
                    if (message.CcAddresses != null)
                    {
                        foreach (var ccAddress in message.CcAddresses.Where(a => !string.IsNullOrWhiteSpace(a)))
                        {
                            mailMessage.CC.Add(ccAddress);
                        }
                    }

                    // Set subject and body
                    mailMessage.Subject = message.Subject;
                    
                    if (!string.IsNullOrEmpty(message.HtmlBody))
                    {
                        mailMessage.Body = message.HtmlBody;
                        mailMessage.IsBodyHtml = true;
                    }
                    else if (!string.IsNullOrEmpty(message.PlainTextBody))
                    {
                        mailMessage.Body = message.PlainTextBody;
                        mailMessage.IsBodyHtml = false;
                    }

                    // Set priority
                    mailMessage.Priority = message.Priority switch
                    {
                        NotificationPriority.Low => System.Net.Mail.MailPriority.Low,
                        NotificationPriority.High => System.Net.Mail.MailPriority.High,
                        NotificationPriority.Critical => System.Net.Mail.MailPriority.High,
                        _ => System.Net.Mail.MailPriority.Normal
                    };

                    // Send the email
                    await client.SendMailAsync(mailMessage);
                    
                    _logger.LogInformation("Email sent successfully to {Recipients} with subject '{Subject}'", 
                        string.Join(", ", message.ToAddresses), message.Subject);
                    
                    return true;
                }
                catch (System.Net.Mail.SmtpException ex)
                {
                    retryCount++;
                    _logger.LogWarning("SMTP error on attempt {Attempt}/{MaxAttempts}: {Error}", 
                        retryCount, maxRetries + 1, ex.Message);

                    if (retryCount > maxRetries)
                    {
                        _logger.LogError(ex, "Failed to send email after {MaxAttempts} attempts", maxRetries + 1);
                        return false;
                    }

                    // Wait before retry (exponential backoff)
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount - 1));
                    await Task.Delay(delay);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error sending email: {Error}", ex.Message);
                    return false;
                }
            }

            return false;
        }

        public async Task<bool> TestConnectionAsync()
        {
            if (_configuration == null)
            {
                _logger.LogError("SMTP client not configured");
                return false;
            }

            try
            {
                using var client = new System.Net.Mail.SmtpClient(_configuration.Host, _configuration.Port);
                
                // Configure SSL/TLS
                client.EnableSsl = _configuration.EnableSsl;
                client.Timeout = _configuration.TimeoutSeconds * 1000; // Convert to milliseconds

                // Configure authentication if credentials are provided
                if (!string.IsNullOrEmpty(_configuration.Username) && !string.IsNullOrEmpty(_configuration.Password))
                {
                    client.Credentials = new System.Net.NetworkCredential(_configuration.Username, _configuration.Password);
                }

                // Create a test message to validate the connection
                using var testMessage = new System.Net.Mail.MailMessage();
                testMessage.From = new System.Net.Mail.MailAddress(_configuration.FromEmail);
                testMessage.To.Add(_configuration.FromEmail); // Send to self for testing
                testMessage.Subject = "SMTP Connection Test";
                testMessage.Body = "This is a test message to validate SMTP connection.";

                // We'll use a different approach - just try to connect without sending
                // Unfortunately, .NET SmtpClient doesn't have a direct connection test method
                // So we'll create a minimal test message but catch specific exceptions
                
                try
                {
                    await client.SendMailAsync(testMessage);
                    _logger.LogInformation("SMTP connection test successful for {Host}:{Port}", _configuration.Host, _configuration.Port);
                    return true;
                }
                catch (System.Net.Mail.SmtpException ex) when (ex.StatusCode == System.Net.Mail.SmtpStatusCode.MailboxBusy ||
                                                               ex.StatusCode == System.Net.Mail.SmtpStatusCode.InsufficientStorage ||
                                                               ex.StatusCode == System.Net.Mail.SmtpStatusCode.TransactionFailed)
                {
                    // These errors indicate we connected successfully but couldn't send (which is fine for a connection test)
                    _logger.LogInformation("SMTP connection successful (server responded with: {StatusCode})", ex.StatusCode);
                    return true;
                }
            }
            catch (System.Net.Mail.SmtpException ex)
            {
                _logger.LogError("SMTP connection test failed: {Error} (StatusCode: {StatusCode})", ex.Message, ex.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SMTP connection test failed with unexpected error: {Error}", ex.Message);
                return false;
            }
        }
    }

    internal class EmailTemplateEngine : IEmailTemplateEngine
    {
        public void RegisterTemplate(string name, string templateContent)
        {
            throw new NotImplementedException("Implementation will be added in task 5");
        }

        public Task<string> RenderTemplateAsync(string templateName, object model)
        {
            throw new NotImplementedException("Implementation will be added in task 5");
        }

        public Task<bool> ValidateTemplateAsync(string templateName)
        {
            throw new NotImplementedException("Implementation will be added in task 5");
        }
    }
}