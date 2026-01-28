using System.Collections.Generic;

namespace CsPlaywrightXun.Services.Notifications
{
    /// <summary>
    /// Interface for email address validation service
    /// </summary>
    public interface IEmailValidator
    {
        /// <summary>
        /// Validates a single email address
        /// </summary>
        /// <param name="emailAddress">Email address to validate</param>
        /// <returns>Validation result with details</returns>
        EmailValidationResult ValidateEmail(string emailAddress);

        /// <summary>
        /// Validates multiple email addresses
        /// </summary>
        /// <param name="emailAddresses">List of email addresses to validate</param>
        /// <returns>List of validation results</returns>
        List<EmailValidationResult> ValidateEmails(List<string> emailAddresses);

        /// <summary>
        /// Gets only valid email addresses from a list
        /// </summary>
        /// <param name="emailAddresses">List of email addresses to filter</param>
        /// <returns>List of valid email addresses</returns>
        List<string> GetValidEmails(List<string> emailAddresses);

        /// <summary>
        /// Checks if an email address is valid
        /// </summary>
        /// <param name="emailAddress">Email address to check</param>
        /// <returns>True if valid, false otherwise</returns>
        bool IsValidEmail(string emailAddress);
    }

    /// <summary>
    /// Email validation result
    /// </summary>
    public class EmailValidationResult
    {
        /// <summary>
        /// Original email address that was validated
        /// </summary>
        public string OriginalEmail { get; set; } = string.Empty;

        /// <summary>
        /// Normalized email address (trimmed, lowercase)
        /// </summary>
        public string NormalizedEmail { get; set; } = string.Empty;

        /// <summary>
        /// Whether the email address is valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Validation error message if invalid
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Type of validation error
        /// </summary>
        public EmailValidationErrorType ErrorType { get; set; }
    }

    /// <summary>
    /// Types of email validation errors
    /// </summary>
    public enum EmailValidationErrorType
    {
        None,
        NullOrEmpty,
        InvalidFormat,
        TooLong,
        InvalidDomain,
        InvalidLocalPart,
        ContainsInvalidCharacters
    }
}