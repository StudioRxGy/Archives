using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace CsPlaywrightXun.Services.Notifications
{
    /// <summary>
    /// SMTP server configuration settings
    /// </summary>
    public class SmtpConfiguration
    {
        /// <summary>
        /// SMTP server hostname or IP address
        /// </summary>
        [Required(ErrorMessage = "SMTP Host is required")]
        [StringLength(255, ErrorMessage = "Host name cannot exceed 255 characters")]
        public string Host { get; set; } = string.Empty;

        /// <summary>
        /// SMTP server port number
        /// </summary>
        [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535")]
        public int Port { get; set; } = 587;

        /// <summary>
        /// Enable SSL/TLS encryption
        /// </summary>
        public bool EnableSsl { get; set; } = true;

        /// <summary>
        /// SMTP authentication username
        /// </summary>
        [StringLength(255, ErrorMessage = "Username cannot exceed 255 characters")]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// SMTP authentication password
        /// </summary>
        [StringLength(255, ErrorMessage = "Password cannot exceed 255 characters")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Sender email address
        /// </summary>
        [Required(ErrorMessage = "From Email is required")]
        [EmailAddress(ErrorMessage = "From Email must be a valid email address")]
        public string FromEmail { get; set; } = string.Empty;

        /// <summary>
        /// Sender display name
        /// </summary>
        [StringLength(100, ErrorMessage = "Display name cannot exceed 100 characters")]
        public string FromDisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Connection timeout in seconds
        /// </summary>
        [Range(5, 300, ErrorMessage = "Timeout must be between 5 and 300 seconds")]
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Maximum number of retry attempts for failed sends
        /// </summary>
        [Range(0, 10, ErrorMessage = "Max retry attempts must be between 0 and 10")]
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Validates the SMTP configuration
        /// </summary>
        /// <returns>True if configuration is valid, false otherwise</returns>
        public bool IsValid()
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(Host))
                    return false;

                if (string.IsNullOrWhiteSpace(FromEmail))
                    return false;

                // Validate email format
                var emailAddress = new MailAddress(FromEmail);

                // Validate port range
                if (Port < 1 || Port > 65535)
                    return false;

                // Validate timeout range
                if (TimeoutSeconds < 5 || TimeoutSeconds > 300)
                    return false;

                // Validate retry attempts
                if (MaxRetryAttempts < 0 || MaxRetryAttempts > 10)
                    return false;

                return true;
            }
            catch (FormatException)
            {
                // Invalid email format
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Gets validation errors for the configuration
        /// </summary>
        /// <returns>List of validation error messages</returns>
        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Host))
                errors.Add("SMTP Host is required");

            if (string.IsNullOrWhiteSpace(FromEmail))
                errors.Add("From Email is required");
            else
            {
                try
                {
                    var emailAddress = new MailAddress(FromEmail);
                }
                catch (FormatException)
                {
                    errors.Add("From Email must be a valid email address");
                }
            }

            if (Port < 1 || Port > 65535)
                errors.Add("Port must be between 1 and 65535");

            if (TimeoutSeconds < 5 || TimeoutSeconds > 300)
                errors.Add("Timeout must be between 5 and 300 seconds");

            if (MaxRetryAttempts < 0 || MaxRetryAttempts > 10)
                errors.Add("Max retry attempts must be between 0 and 10");

            if (!string.IsNullOrEmpty(FromDisplayName) && FromDisplayName.Length > 100)
                errors.Add("Display name cannot exceed 100 characters");

            if (!string.IsNullOrEmpty(Username) && Username.Length > 255)
                errors.Add("Username cannot exceed 255 characters");

            if (!string.IsNullOrEmpty(Password) && Password.Length > 255)
                errors.Add("Password cannot exceed 255 characters");

            return errors;
        }
    }
}