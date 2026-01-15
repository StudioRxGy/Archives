using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CsPlaywrightXun.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;

namespace CsPlaywrightXun.Services.Notifications
{
    /// <summary>
    /// Validates notification configuration settings
    /// </summary>
    public class ConfigurationValidator
    {
        private readonly ILogger<ConfigurationValidator> _logger;

        public ConfigurationValidator(ILogger<ConfigurationValidator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Validates notification configuration from IConfiguration
        /// </summary>
        /// <param name="configuration">Configuration instance</param>
        /// <returns>Validation result with errors if any</returns>
        public ValidationResult ValidateConfiguration(IConfiguration configuration)
        {
            var result = new ValidationResult();

            try
            {
                // Check if Notifications section exists
                var notificationsSection = configuration.GetSection("Notifications");
                if (!notificationsSection.Exists())
                {
                    result.AddError("Notifications configuration section not found");
                    return result;
                }

                // Validate SMTP configuration
                var smtpSection = notificationsSection.GetSection("Smtp");
                if (!smtpSection.Exists())
                {
                    result.AddError("SMTP configuration section not found");
                }
                else
                {
                    var smtpConfig = smtpSection.Get<SmtpConfiguration>();
                    if (smtpConfig != null)
                    {
                        var smtpErrors = smtpConfig.GetValidationErrors();
                        foreach (var error in smtpErrors)
                        {
                            result.AddError($"SMTP: {error}");
                        }
                    }
                    else
                    {
                        result.AddError("Failed to parse SMTP configuration");
                    }
                }

                // Validate notification rules
                var rulesSection = notificationsSection.GetSection("Rules");
                if (rulesSection.Exists())
                {
                    var rules = rulesSection.Get<List<NotificationRule>>();
                    if (rules != null)
                    {
                        for (int i = 0; i < rules.Count; i++)
                        {
                            var rule = rules[i];
                            var ruleErrors = rule.GetValidationErrors();
                            foreach (var error in ruleErrors)
                            {
                                result.AddError($"Rule '{rule.Id}' (index {i}): {error}");
                            }
                        }

                        // Check for duplicate rule IDs
                        var duplicateIds = rules.GroupBy(r => r.Id)
                            .Where(g => g.Count() > 1)
                            .Select(g => g.Key);

                        foreach (var duplicateId in duplicateIds)
                        {
                            result.AddError($"Duplicate rule ID found: {duplicateId}");
                        }
                    }
                }

                // Validate default recipients
                var defaultRecipients = notificationsSection.GetSection("DefaultRecipients").Get<List<string>>();
                if (defaultRecipients != null)
                {
                    for (int i = 0; i < defaultRecipients.Count; i++)
                    {
                        var recipient = defaultRecipients[i];
                        if (string.IsNullOrWhiteSpace(recipient))
                        {
                            result.AddError($"Default recipient at index {i} cannot be empty");
                        }
                        else
                        {
                            try
                            {
                                var emailAddress = new MailAddress(recipient);
                            }
                            catch (FormatException)
                            {
                                result.AddError($"Default recipient '{recipient}' is not a valid email address");
                            }
                        }
                    }
                }

                // Log validation results
                if (result.IsValid)
                {
                    _logger.LogInformation("Notification configuration validation passed");
                }
                else
                {
                    _logger.LogWarning("Notification configuration validation failed with {ErrorCount} errors", result.Errors.Count);
                    foreach (var error in result.Errors)
                    {
                        _logger.LogWarning("Validation error: {Error}", error);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during configuration validation");
                result.AddError($"Validation failed with exception: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Validates SMTP configuration specifically
        /// </summary>
        /// <param name="smtpConfig">SMTP configuration to validate</param>
        /// <returns>Validation result</returns>
        public ValidationResult ValidateSmtpConfiguration(SmtpConfiguration smtpConfig)
        {
            var result = new ValidationResult();

            if (smtpConfig == null)
            {
                result.AddError("SMTP configuration is null");
                return result;
            }

            var errors = smtpConfig.GetValidationErrors();
            foreach (var error in errors)
            {
                result.AddError(error);
            }

            return result;
        }

        /// <summary>
        /// Validates notification settings
        /// </summary>
        /// <param name="settings">Notification settings to validate</param>
        /// <returns>Validation result</returns>
        public ValidationResult ValidateNotificationSettings(NotificationSettings settings)
        {
            var result = new ValidationResult();

            if (settings == null)
            {
                result.AddError("Notification settings are null");
                return result;
            }

            var errors = settings.GetValidationErrors();
            foreach (var error in errors)
            {
                result.AddError(error);
            }

            return result;
        }

        /// <summary>
        /// Validates a single notification rule
        /// </summary>
        /// <param name="rule">Notification rule to validate</param>
        /// <returns>Validation result</returns>
        public ValidationResult ValidateNotificationRule(NotificationRule rule)
        {
            var result = new ValidationResult();

            if (rule == null)
            {
                result.AddError("Notification rule is null");
                return result;
            }

            var errors = rule.GetValidationErrors();
            foreach (var error in errors)
            {
                result.AddError(error);
            }

            return result;
        }

        /// <summary>
        /// Validates an email message
        /// </summary>
        /// <param name="message">Email message to validate</param>
        /// <returns>Validation result</returns>
        public ValidationResult ValidateEmailMessage(EmailMessage message)
        {
            var result = new ValidationResult();

            if (message == null)
            {
                result.AddError("Email message is null");
                return result;
            }

            var errors = message.GetValidationErrors();
            foreach (var error in errors)
            {
                result.AddError(error);
            }

            return result;
        }
    }

    /// <summary>
    /// Validation result containing errors if any
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// List of validation errors
        /// </summary>
        public List<string> Errors { get; } = new();

        /// <summary>
        /// Whether the validation passed (no errors)
        /// </summary>
        public bool IsValid => Errors.Count == 0;

        /// <summary>
        /// Adds an error to the validation result
        /// </summary>
        /// <param name="error">Error message</param>
        public void AddError(string error)
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                Errors.Add(error);
            }
        }

        /// <summary>
        /// Gets a formatted error message with all errors
        /// </summary>
        /// <returns>Formatted error message</returns>
        public string GetErrorMessage()
        {
            if (IsValid)
                return "Validation passed";

            return $"Validation failed with {Errors.Count} error(s):\n" + string.Join("\n", Errors.Select((e, i) => $"{i + 1}. {e}"));
        }
    }
}