using System.Collections.Generic;
using System.Threading.Tasks;

namespace CsPlaywrightXun.Services.Notifications
{
    /// <summary>
    /// Interface for managing recipients and notification rules
    /// </summary>
    public interface IRecipientManager
    {
        /// <summary>
        /// Gets recipients for a specific notification type based on rules and context
        /// </summary>
        /// <param name="notificationType">Type of notification</param>
        /// <param name="rules">List of notification rules</param>
        /// <param name="context">Context for rule evaluation</param>
        /// <param name="defaultRecipients">Default recipients if no rules match</param>
        /// <returns>List of recipient email addresses</returns>
        Task<List<string>> GetRecipientsAsync(
            NotificationType notificationType,
            List<NotificationRule> rules,
            Dictionary<string, object> context,
            List<string>? defaultRecipients = null);

        /// <summary>
        /// Checks if a notification should be sent based on frequency limits
        /// </summary>
        /// <param name="rule">Notification rule to check</param>
        /// <param name="notificationType">Type of notification</param>
        /// <returns>True if notification should be sent, false if rate limited</returns>
        bool ShouldSendNotification(NotificationRule rule, NotificationType notificationType);

        /// <summary>
        /// Updates failure count for escalation logic
        /// </summary>
        /// <param name="testSuiteName">Name of the test suite</param>
        /// <param name="isFailure">Whether this is a failure</param>
        void UpdateFailureCount(string testSuiteName, bool isFailure);

        /// <summary>
        /// Gets the current consecutive failure count for a test suite
        /// </summary>
        /// <param name="testSuiteName">Name of the test suite</param>
        /// <returns>Number of consecutive failures</returns>
        int GetConsecutiveFailureCount(string testSuiteName);

        /// <summary>
        /// Validates recipient email addresses
        /// </summary>
        /// <param name="recipients">List of recipient email addresses</param>
        /// <returns>List of valid email addresses</returns>
        List<string> ValidateRecipients(List<string> recipients);
    }
}