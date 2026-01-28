using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using System.Linq;

namespace CsPlaywrightXun.Services.Notifications
{
    /// <summary>
    /// Notification rule configuration
    /// </summary>
    public class NotificationRule
    {
        /// <summary>
        /// Unique identifier for the rule
        /// </summary>
        [Required(ErrorMessage = "Rule ID is required")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Type of notification this rule applies to
        /// </summary>
        [Required(ErrorMessage = "Notification type is required")]
        public NotificationType Type { get; set; }

        /// <summary>
        /// List of recipient email addresses for this rule
        /// </summary>
        [Required(ErrorMessage = "At least one recipient is required")]
        [MinLength(1, ErrorMessage = "At least one recipient is required")]
        public List<string> Recipients { get; set; } = new();

        /// <summary>
        /// Whether this rule is currently enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Cooldown period to prevent notification spam
        /// </summary>
        public TimeSpan? CooldownPeriod { get; set; }

        /// <summary>
        /// Additional conditions for when this rule should apply
        /// </summary>
        public Dictionary<string, object> Conditions { get; set; } = new();

        /// <summary>
        /// Validates the notification rule
        /// </summary>
        /// <returns>True if rule is valid, false otherwise</returns>
        public bool IsValid()
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(Id))
                    return false;

                if (Recipients == null || Recipients.Count == 0)
                    return false;

                // Validate all recipient email addresses
                foreach (var recipient in Recipients)
                {
                    if (string.IsNullOrWhiteSpace(recipient))
                        return false;

                    try
                    {
                        var emailAddress = new MailAddress(recipient);
                    }
                    catch (FormatException)
                    {
                        return false;
                    }
                }

                // Validate cooldown period if specified
                if (CooldownPeriod.HasValue && CooldownPeriod.Value.TotalSeconds < 0)
                    return false;

                // Validate notification type is defined
                if (!Enum.IsDefined(typeof(NotificationType), Type))
                    return false;

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Gets validation errors for the notification rule
        /// </summary>
        /// <returns>List of validation error messages</returns>
        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Id))
                errors.Add("Rule ID is required");

            if (Recipients == null || Recipients.Count == 0)
                errors.Add("At least one recipient is required");
            else
            {
                for (int i = 0; i < Recipients.Count; i++)
                {
                    var recipient = Recipients[i];
                    if (string.IsNullOrWhiteSpace(recipient))
                    {
                        errors.Add($"Recipient at index {i} cannot be empty");
                    }
                    else
                    {
                        try
                        {
                            var emailAddress = new MailAddress(recipient);
                        }
                        catch (FormatException)
                        {
                            errors.Add($"Recipient '{recipient}' is not a valid email address");
                        }
                    }
                }

                // Check for duplicate recipients
                var duplicates = Recipients.GroupBy(r => r.ToLowerInvariant())
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key);

                foreach (var duplicate in duplicates)
                {
                    errors.Add($"Duplicate recipient found: {duplicate}");
                }
            }

            if (CooldownPeriod.HasValue && CooldownPeriod.Value.TotalSeconds < 0)
                errors.Add("Cooldown period cannot be negative");

            if (!Enum.IsDefined(typeof(NotificationType), Type))
                errors.Add($"Invalid notification type: {Type}");

            return errors;
        }

        /// <summary>
        /// Checks if the rule should apply based on conditions
        /// </summary>
        /// <param name="context">Context to evaluate conditions against</param>
        /// <returns>True if rule should apply, false otherwise</returns>
        public bool ShouldApply(Dictionary<string, object> context)
        {
            if (!IsEnabled)
                return false;

            if (Conditions == null || Conditions.Count == 0)
                return true;

            // Evaluate all conditions - all must be true for rule to apply
            foreach (var condition in Conditions)
            {
                if (!context.ContainsKey(condition.Key))
                    return false;

                var contextValue = context[condition.Key];
                var conditionValue = condition.Value;

                // Simple equality check - can be extended for more complex conditions
                if (!Equals(contextValue, conditionValue))
                    return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Types of notifications that can be sent
    /// </summary>
    public enum NotificationType
    {
        /// <summary>
        /// Notification sent when test execution starts
        /// </summary>
        TestStart,

        /// <summary>
        /// Notification sent when test execution completes successfully
        /// </summary>
        TestSuccess,

        /// <summary>
        /// Notification sent when test execution fails
        /// </summary>
        TestFailure,

        /// <summary>
        /// Notification sent when critical tests fail
        /// </summary>
        CriticalFailure,

        /// <summary>
        /// Notification sent when test report is generated
        /// </summary>
        ReportGenerated
    }
}