using System;
using System.Collections.Generic;

namespace CsPlaywrightXun.Services.Notifications
{
    /// <summary>
    /// Log entry for notification tracking
    /// </summary>
    public class NotificationLogEntry
    {
        /// <summary>
        /// Unique notification identifier
        /// </summary>
        public string NotificationId { get; set; } = string.Empty;

        /// <summary>
        /// Type of notification
        /// </summary>
        public NotificationType NotificationType { get; set; }

        /// <summary>
        /// List of recipients
        /// </summary>
        public List<string> Recipients { get; set; } = new();

        /// <summary>
        /// Email subject
        /// </summary>
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// Associated test suite name
        /// </summary>
        public string TestSuiteName { get; set; } = string.Empty;

        /// <summary>
        /// Time when notification attempt was made
        /// </summary>
        public DateTime AttemptTime { get; set; }

        /// <summary>
        /// Time when notification was completed (success or failure)
        /// </summary>
        public DateTime? CompletionTime { get; set; }

        /// <summary>
        /// Current status of the notification
        /// </summary>
        public NotificationStatus Status { get; set; }

        /// <summary>
        /// Error message if notification failed
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Duration of the notification attempt
        /// </summary>
        public TimeSpan? Duration => CompletionTime.HasValue 
            ? CompletionTime.Value - AttemptTime 
            : null;

        /// <summary>
        /// Whether the notification was successful
        /// </summary>
        public bool IsSuccessful => Status == NotificationStatus.Sent;

        /// <summary>
        /// Whether the notification failed
        /// </summary>
        public bool IsFailed => Status == NotificationStatus.Failed;
    }
}