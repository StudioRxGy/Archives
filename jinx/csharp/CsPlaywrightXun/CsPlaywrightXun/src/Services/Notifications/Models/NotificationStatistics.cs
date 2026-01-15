using System;
using System.Collections.Generic;

namespace CsPlaywrightXun.Services.Notifications
{
    /// <summary>
    /// Statistics for notification tracking
    /// </summary>
    public class NotificationStatistics
    {
        /// <summary>
        /// Start time of the statistics period
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// End time of the statistics period
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Total number of notifications attempted
        /// </summary>
        public int TotalNotifications { get; set; }

        /// <summary>
        /// Number of successful notifications
        /// </summary>
        public int SuccessfulNotifications { get; set; }

        /// <summary>
        /// Number of failed notifications
        /// </summary>
        public int FailedNotifications { get; set; }

        /// <summary>
        /// Number of pending notifications
        /// </summary>
        public int PendingNotifications { get; set; }

        /// <summary>
        /// Number of notifications currently retrying
        /// </summary>
        public int RetryingNotifications { get; set; }

        /// <summary>
        /// Success rate as a percentage
        /// </summary>
        public double SuccessRate => TotalNotifications > 0 
            ? (double)SuccessfulNotifications / TotalNotifications * 100 
            : 0;

        /// <summary>
        /// Failure rate as a percentage
        /// </summary>
        public double FailureRate => TotalNotifications > 0 
            ? (double)FailedNotifications / TotalNotifications * 100 
            : 0;

        /// <summary>
        /// Breakdown of notifications by type
        /// </summary>
        public Dictionary<NotificationType, int> NotificationsByType { get; set; } = new();

        /// <summary>
        /// Duration of the statistics period
        /// </summary>
        public TimeSpan Period => EndTime - StartTime;
    }
}