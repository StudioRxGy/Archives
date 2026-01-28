using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using System.Linq;
using System.Text.Json.Serialization;

namespace CsPlaywrightXun.Services.Notifications
{
    /// <summary>
    /// Email message model for notification system
    /// </summary>
    public class EmailMessage
    {
        /// <summary>
        /// Unique identifier for the email message
        /// </summary>
        [Required(ErrorMessage = "Message ID is required")]
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// List of recipient email addresses
        /// </summary>
        [Required(ErrorMessage = "At least one recipient is required")]
        [MinLength(1, ErrorMessage = "At least one recipient is required")]
        [JsonPropertyName("toAddresses")]
        public List<string> ToAddresses { get; set; } = new();

        /// <summary>
        /// List of CC recipient email addresses
        /// </summary>
        [JsonPropertyName("ccAddresses")]
        public List<string> CcAddresses { get; set; } = new();

        /// <summary>
        /// Email subject line
        /// </summary>
        [Required(ErrorMessage = "Subject is required")]
        [StringLength(255, ErrorMessage = "Subject cannot exceed 255 characters")]
        [JsonPropertyName("subject")]
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// HTML body content
        /// </summary>
        [JsonPropertyName("htmlBody")]
        public string HtmlBody { get; set; } = string.Empty;

        /// <summary>
        /// Plain text body content
        /// </summary>
        [JsonPropertyName("plainTextBody")]
        public string PlainTextBody { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the message was created
        /// </summary>
        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Priority level of the notification
        /// </summary>
        [JsonPropertyName("priority")]
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

        /// <summary>
        /// Validates the email message
        /// </summary>
        /// <returns>True if message is valid, false otherwise</returns>
        public bool IsValid()
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(Id))
                    return false;

                if (string.IsNullOrWhiteSpace(Subject))
                    return false;

                if (ToAddresses == null || ToAddresses.Count == 0)
                    return false;

                // Must have either HTML or plain text body
                if (string.IsNullOrWhiteSpace(HtmlBody) && string.IsNullOrWhiteSpace(PlainTextBody))
                    return false;

                // Validate all TO email addresses
                foreach (var address in ToAddresses)
                {
                    if (string.IsNullOrWhiteSpace(address))
                        return false;

                    try
                    {
                        var emailAddress = new MailAddress(address);
                    }
                    catch (FormatException)
                    {
                        return false;
                    }
                }

                // Validate all CC email addresses if any
                if (CcAddresses != null)
                {
                    foreach (var address in CcAddresses)
                    {
                        if (!string.IsNullOrWhiteSpace(address))
                        {
                            try
                            {
                                var emailAddress = new MailAddress(address);
                            }
                            catch (FormatException)
                            {
                                return false;
                            }
                        }
                    }
                }

                // Validate priority is defined
                if (!Enum.IsDefined(typeof(NotificationPriority), Priority))
                    return false;

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Gets validation errors for the email message
        /// </summary>
        /// <returns>List of validation error messages</returns>
        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Id))
                errors.Add("Message ID is required");

            if (string.IsNullOrWhiteSpace(Subject))
                errors.Add("Subject is required");
            else if (Subject.Length > 255)
                errors.Add("Subject cannot exceed 255 characters");

            if (ToAddresses == null || ToAddresses.Count == 0)
                errors.Add("At least one recipient is required");
            else
            {
                for (int i = 0; i < ToAddresses.Count; i++)
                {
                    var address = ToAddresses[i];
                    if (string.IsNullOrWhiteSpace(address))
                    {
                        errors.Add($"TO address at index {i} cannot be empty");
                    }
                    else
                    {
                        try
                        {
                            var emailAddress = new MailAddress(address);
                        }
                        catch (FormatException)
                        {
                            errors.Add($"TO address '{address}' is not a valid email address");
                        }
                    }
                }

                // Check for duplicate TO addresses
                var duplicates = ToAddresses.GroupBy(a => a.ToLowerInvariant())
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key);

                foreach (var duplicate in duplicates)
                {
                    errors.Add($"Duplicate TO address found: {duplicate}");
                }
            }

            // Validate CC addresses if any
            if (CcAddresses != null)
            {
                for (int i = 0; i < CcAddresses.Count; i++)
                {
                    var address = CcAddresses[i];
                    if (!string.IsNullOrWhiteSpace(address))
                    {
                        try
                        {
                            var emailAddress = new MailAddress(address);
                        }
                        catch (FormatException)
                        {
                            errors.Add($"CC address '{address}' is not a valid email address");
                        }
                    }
                }

                // Check for duplicate CC addresses
                var ccDuplicates = CcAddresses.Where(a => !string.IsNullOrWhiteSpace(a))
                    .GroupBy(a => a.ToLowerInvariant())
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key);

                foreach (var duplicate in ccDuplicates)
                {
                    errors.Add($"Duplicate CC address found: {duplicate}");
                }
            }

            // Must have either HTML or plain text body
            if (string.IsNullOrWhiteSpace(HtmlBody) && string.IsNullOrWhiteSpace(PlainTextBody))
                errors.Add("Either HTML body or plain text body is required");

            if (!Enum.IsDefined(typeof(NotificationPriority), Priority))
                errors.Add($"Invalid priority: {Priority}");

            return errors;
        }

        /// <summary>
        /// Gets all unique recipient addresses (TO + CC)
        /// </summary>
        /// <returns>List of unique email addresses</returns>
        public List<string> GetAllRecipients()
        {
            var allRecipients = new List<string>();
            
            if (ToAddresses != null)
                allRecipients.AddRange(ToAddresses);
            
            if (CcAddresses != null)
                allRecipients.AddRange(CcAddresses.Where(a => !string.IsNullOrWhiteSpace(a)));

            return allRecipients.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }
    }

    /// <summary>
    /// Notification priority levels
    /// </summary>
    public enum NotificationPriority
    {
        Low,
        Normal,
        High,
        Critical
    }
}