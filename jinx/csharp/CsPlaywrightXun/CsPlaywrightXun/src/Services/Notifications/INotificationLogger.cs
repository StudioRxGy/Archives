using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CsPlaywrightXun.Services.Notifications
{
    /// <summary>
    /// Interface for notification logging and history tracking
    /// </summary>
    public interface INotificationLogger
    {
        /// <summary>
        /// Logs a notification attempt
        /// </summary>
        /// <param name="notificationId">Unique notification identifier</param>
        /// <param name="notificationType">Type of notification</param>
        /// <param name="recipients">List of recipients</param>
        /// <param name="subject">Email subject</param>
        /// <param name="testSuiteName">Associated test suite name</param>
        Task LogNotificationAttemptAsync(
            string notificationId,
            NotificationType notificationType,
            List<string> recipients,
            string subject,
            string? testSuiteName = null);

        /// <summary>
        /// Updates the status of a notification
        /// </summary>
        /// <param name="notificationId">Unique notification identifier</param>
        /// <param name="status">New status</param>
        /// <param name="errorMessage">Error message if failed</param>
        Task UpdateNotificationStatusAsync(
            string notificationId,
            NotificationStatus status,
            string? errorMessage = null);

        /// <summary>
        /// Gets the status of a specific notification
        /// </summary>
        /// <param name="notificationId">Unique notification identifier</param>
        /// <returns>Notification status or null if not found</returns>
        Task<NotificationStatus?> GetNotificationStatusAsync(string notificationId);

        /// <summary>
        /// Gets the complete log entry for a notification
        /// </summary>
        /// <param name="notificationId">Unique notification identifier</param>
        /// <returns>Notification log entry or null if not found</returns>
        Task<NotificationLogEntry?> GetNotificationLogAsync(string notificationId);

        /// <summary>
        /// Gets notification history for a specific test suite
        /// </summary>
        /// <param name="testSuiteName">Name of the test suite</param>
        /// <param name="maxEntries">Maximum number of entries to return</param>
        /// <returns>List of notification log entries</returns>
        Task<List<NotificationLogEntry>> GetTestSuiteNotificationHistoryAsync(
            string testSuiteName, 
            int maxEntries = 100);

        /// <summary>
        /// Gets all notification history within a time range
        /// </summary>
        /// <param name="startTime">Start time for the range</param>
        /// <param name="endTime">End time for the range</param>
        /// <param name="notificationType">Optional filter by notification type</param>
        /// <returns>List of notification log entries</returns>
        Task<List<NotificationLogEntry>> GetNotificationHistoryAsync(
            DateTime startTime,
            DateTime endTime,
            NotificationType? notificationType = null);

        /// <summary>
        /// Gets notification statistics for a time period
        /// </summary>
        /// <param name="startTime">Start time for the period</param>
        /// <param name="endTime">End time for the period</param>
        /// <returns>Notification statistics</returns>
        Task<NotificationStatistics> GetNotificationStatisticsAsync(
            DateTime startTime,
            DateTime endTime);

        /// <summary>
        /// Clears old notification history entries
        /// </summary>
        /// <param name="olderThan">Remove entries older than this date</param>
        /// <returns>Number of entries removed</returns>
        Task<int> CleanupOldHistoryAsync(DateTime olderThan);

        /// <summary>
        /// Logs an email validation error
        /// </summary>
        /// <param name="emailAddress">Email address that failed validation</param>
        /// <param name="errorType">Type of validation error</param>
        /// <param name="errorMessage">Detailed error message</param>
        Task LogValidationErrorAsync(string emailAddress, string errorType, string errorMessage);
    }
}