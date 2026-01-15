using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CsPlaywrightXun.Services.Notifications
{
    /// <summary>
    /// Email address validation service implementation
    /// </summary>
    public class EmailValidator : IEmailValidator
    {
        private readonly ILogger<EmailValidator> _logger;
        private readonly INotificationLogger _notificationLogger;
        
        // RFC 5322 compliant email regex pattern
        private static readonly Regex EmailRegex = new(
            @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private const int MaxEmailLength = 254; // RFC 5321 limit

        /// <summary>
        /// Initializes a new instance of the EmailValidator class
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="notificationLogger">Notification logger for recording validation errors</param>
        public EmailValidator(ILogger<EmailValidator> logger, INotificationLogger notificationLogger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _notificationLogger = notificationLogger ?? throw new ArgumentNullException(nameof(notificationLogger));
        }

        /// <summary>
        /// Validates a single email address
        /// </summary>
        /// <param name="emailAddress">Email address to validate</param>
        /// <returns>Validation result with details</returns>
        public EmailValidationResult ValidateEmail(string emailAddress)
        {
            var result = new EmailValidationResult
            {
                OriginalEmail = emailAddress ?? string.Empty
            };

            try
            {
                // Check for null or empty
                if (string.IsNullOrWhiteSpace(emailAddress))
                {
                    result.IsValid = false;
                    result.ErrorType = EmailValidationErrorType.NullOrEmpty;
                    result.ErrorMessage = "Email address cannot be null or empty";
                    LogValidationError(result);
                    return result;
                }

                // Normalize the email address
                var normalizedEmail = emailAddress.Trim().ToLowerInvariant();
                result.NormalizedEmail = normalizedEmail;

                // Check length
                if (normalizedEmail.Length > MaxEmailLength)
                {
                    result.IsValid = false;
                    result.ErrorType = EmailValidationErrorType.TooLong;
                    result.ErrorMessage = $"Email address exceeds maximum length of {MaxEmailLength} characters";
                    LogValidationError(result);
                    return result;
                }

                // Check basic format with regex
                if (!EmailRegex.IsMatch(normalizedEmail))
                {
                    result.IsValid = false;
                    result.ErrorType = EmailValidationErrorType.InvalidFormat;
                    result.ErrorMessage = "Email address format is invalid";
                    LogValidationError(result);
                    return result;
                }

                // Use .NET MailAddress for additional validation
                try
                {
                    var mailAddress = new MailAddress(normalizedEmail);
                    
                    // Ensure the normalized address matches what MailAddress parsed
                    if (!string.Equals(mailAddress.Address, normalizedEmail, StringComparison.OrdinalIgnoreCase))
                    {
                        result.IsValid = false;
                        result.ErrorType = EmailValidationErrorType.InvalidFormat;
                        result.ErrorMessage = "Email address contains invalid characters or format";
                        LogValidationError(result);
                        return result;
                    }

                    // Additional domain validation
                    if (!IsValidDomain(mailAddress.Host))
                    {
                        result.IsValid = false;
                        result.ErrorType = EmailValidationErrorType.InvalidDomain;
                        result.ErrorMessage = "Email domain is invalid";
                        LogValidationError(result);
                        return result;
                    }

                    // Additional local part validation
                    var localPart = normalizedEmail.Split('@')[0];
                    if (!IsValidLocalPart(localPart))
                    {
                        result.IsValid = false;
                        result.ErrorType = EmailValidationErrorType.InvalidLocalPart;
                        result.ErrorMessage = "Email local part (before @) is invalid";
                        LogValidationError(result);
                        return result;
                    }

                    result.IsValid = true;
                    result.ErrorType = EmailValidationErrorType.None;
                    result.NormalizedEmail = mailAddress.Address;
                }
                catch (FormatException ex)
                {
                    result.IsValid = false;
                    result.ErrorType = EmailValidationErrorType.InvalidFormat;
                    result.ErrorMessage = $"Invalid email format: {ex.Message}";
                    LogValidationError(result);
                }
                catch (Exception ex)
                {
                    result.IsValid = false;
                    result.ErrorType = EmailValidationErrorType.InvalidFormat;
                    result.ErrorMessage = $"Email validation error: {ex.Message}";
                    LogValidationError(result);
                    _logger.LogError(ex, "Unexpected error validating email address: {Email}", emailAddress);
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ErrorType = EmailValidationErrorType.InvalidFormat;
                result.ErrorMessage = "Email validation failed due to unexpected error";
                _logger.LogError(ex, "Critical error during email validation for: {Email}", emailAddress);
                LogValidationError(result);
            }

            return result;
        }

        /// <summary>
        /// Validates multiple email addresses
        /// </summary>
        /// <param name="emailAddresses">List of email addresses to validate</param>
        /// <returns>List of validation results</returns>
        public List<EmailValidationResult> ValidateEmails(List<string> emailAddresses)
        {
            if (emailAddresses == null)
            {
                _logger.LogWarning("Null email address list provided for validation");
                return new List<EmailValidationResult>();
            }

            var results = new List<EmailValidationResult>();

            try
            {
                foreach (var email in emailAddresses)
                {
                    var result = ValidateEmail(email);
                    results.Add(result);
                }

                var validCount = results.Count(r => r.IsValid);
                var invalidCount = results.Count - validCount;

                _logger.LogInformation("Email validation completed: {ValidCount} valid, {InvalidCount} invalid out of {TotalCount} addresses",
                    validCount, invalidCount, results.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk email validation");
            }

            return results;
        }

        /// <summary>
        /// Gets only valid email addresses from a list
        /// </summary>
        /// <param name="emailAddresses">List of email addresses to filter</param>
        /// <returns>List of valid email addresses</returns>
        public List<string> GetValidEmails(List<string> emailAddresses)
        {
            if (emailAddresses == null)
                return new List<string>();

            try
            {
                var validationResults = ValidateEmails(emailAddresses);
                return validationResults
                    .Where(r => r.IsValid)
                    .Select(r => r.NormalizedEmail)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering valid emails");
                return new List<string>();
            }
        }

        /// <summary>
        /// Checks if an email address is valid
        /// </summary>
        /// <param name="emailAddress">Email address to check</param>
        /// <returns>True if valid, false otherwise</returns>
        public bool IsValidEmail(string emailAddress)
        {
            try
            {
                var result = ValidateEmail(emailAddress);
                return result.IsValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email validity for: {Email}", emailAddress);
                return false;
            }
        }

        /// <summary>
        /// Validates the domain part of an email address
        /// </summary>
        /// <param name="domain">Domain to validate</param>
        /// <returns>True if domain is valid, false otherwise</returns>
        private static bool IsValidDomain(string domain)
        {
            if (string.IsNullOrWhiteSpace(domain))
                return false;

            // Domain cannot start or end with a hyphen
            if (domain.StartsWith('-') || domain.EndsWith('-'))
                return false;

            // Domain cannot start or end with a dot
            if (domain.StartsWith('.') || domain.EndsWith('.'))
                return false;

            // Domain cannot have consecutive dots
            if (domain.Contains(".."))
                return false;

            // Each label in the domain should be valid
            var labels = domain.Split('.');
            foreach (var label in labels)
            {
                if (string.IsNullOrWhiteSpace(label))
                    return false;

                if (label.Length > 63) // RFC 1035 limit
                    return false;

                if (label.StartsWith('-') || label.EndsWith('-'))
                    return false;

                // Label should contain only alphanumeric characters and hyphens
                if (!Regex.IsMatch(label, @"^[a-zA-Z0-9-]+$"))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Validates the local part of an email address
        /// </summary>
        /// <param name="localPart">Local part to validate</param>
        /// <returns>True if local part is valid, false otherwise</returns>
        private static bool IsValidLocalPart(string localPart)
        {
            if (string.IsNullOrWhiteSpace(localPart))
                return false;

            if (localPart.Length > 64) // RFC 5321 limit
                return false;

            // Local part cannot start or end with a dot
            if (localPart.StartsWith('.') || localPart.EndsWith('.'))
                return false;

            // Local part cannot have consecutive dots
            if (localPart.Contains(".."))
                return false;

            return true;
        }

        /// <summary>
        /// Logs validation errors to the notification logger
        /// </summary>
        /// <param name="result">Validation result with error details</param>
        private void LogValidationError(EmailValidationResult result)
        {
            if (result.IsValid || _notificationLogger == null)
                return;

            try
            {
                // Log the validation error asynchronously (fire and forget)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _notificationLogger.LogValidationErrorAsync(
                            result.OriginalEmail,
                            result.ErrorType.ToString(),
                            result.ErrorMessage ?? "Unknown validation error");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to log email validation error for: {Email}", result.OriginalEmail);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating validation error logging for: {Email}", result.OriginalEmail);
            }
        }
    }
}