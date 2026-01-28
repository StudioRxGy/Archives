using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CsPlaywrightXun.Services.Notifications
{
    /// <summary>
    /// Notification logger for tracking email sending status and history
    /// </summary>
    public class NotificationLogger : INotificationLogger
    {
        private readonly ILogger<NotificationLogger> _logger;
        private readonly ConcurrentDictionary<string, NotificationLogEntry> _notificationHistory;
        private readonly ConcurrentDictionary<string, List<NotificationLogEntry>> _testSuiteHistory;
        private readonly object _lockObject = new();

        public NotificationLogger(ILogger<NotificationLogger> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _notificationHistory = new ConcurrentDictionary<string, NotificationLogEntry>();
            _testSuiteHistory = new ConcurrentDictionary<string, List<NotificationLogEntry>>();
        }

        /// <summary>
        /// Logs a notification attempt
        /// </summary>
        /// <param name="notificationId">Unique notification identifier</param>
        /// <param name="notificationType">Type of notification</param>
        /// <param name="recipients">List of recipients</param>
        /// <param name="subject">Email subject</param>
        /// <param name="testSuiteName">Associated test suite name</param>
        public async Task LogNotificationAttemptAsync(
            string notificationId,
            NotificationType notificationType,
            List<string> recipients,
            string subject,
            string? testSuiteName = null)
        {
            try
            {
                var logEntry = new NotificationLogEntry
                {
                    NotificationId = notificationId,
                    NotificationType = notificationType,
                    Recipients = recipients?.ToList() ?? new List<string>(),
                    Subject = subject ?? string.Empty,
                    TestSuiteName = testSuiteName ?? string.Empty,
                    AttemptTime = DateTime.UtcNow,
                    Status = NotificationStatus.Pending
                };

                _notificationHistory.TryAdd(notificationId, logEntry);

                if (!string.IsNullOrWhiteSpace(testSuiteName))
                {
                    AddToTestSuiteHistory(testSuiteName, logEntry);
                }

                _logger.LogInformation("Logged notification attempt: {NotificationId} for {NotificationType}", 
                    notificationId, notificationType);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log notification attempt: {NotificationId}", notificationId);
            }
        }

        /// <summary>
        /// Updates the status of a notification
        /// </summary>
        /// <param name="notificationId">Unique notification identifier</param>
        /// <param name="status">New status</param>
        /// <param name="errorMessage">Error message if failed</param>
        public async Task UpdateNotificationStatusAsync(
            string notificationId,
            NotificationStatus status,
            string? errorMessage = null)
        {
            try
            {
                if (_notificationHistory.TryGetValue(notificationId, out var logEntry))
                {
                    logEntry.Status = status;
                    logEntry.CompletionTime = DateTime.UtcNow;
                    logEntry.ErrorMessage = errorMessage ?? string.Empty;

                    _logger.LogInformation("Updated notification status: {NotificationId} -> {Status}", 
                        notificationId, status);

                    if (status == NotificationStatus.Failed && !string.IsNullOrWhiteSpace(errorMessage))
                    {
                        _logger.LogError("Notification failed: {NotificationId} - {ErrorMessage}", 
                            notificationId, errorMessage);
                    }
                }
                else
                {
                    _logger.LogWarning("Attempted to update status for unknown notification: {NotificationId}", 
                        notificationId);
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update notification status: {NotificationId}", notificationId);
            }
        }

        /// <summary>
        /// Gets the status of a specific notification
        /// </summary>
        /// <param name="notificationId">Unique notification identifier</param>
        /// <returns>Notification status or null if not found</returns>
        public async Task<NotificationStatus?> GetNotificationStatusAsync(string notificationId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(notificationId))
                    return null;

                var status = _notificationHistory.TryGetValue(notificationId, out var logEntry) 
                    ? logEntry.Status 
                    : (NotificationStatus?)null;

                await Task.CompletedTask;
                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get notification status: {NotificationId}", notificationId);
                return null;
            }
        }

        /// <summary>
        /// Gets the complete log entry for a notification
        /// </summary>
        /// <param name="notificationId">Unique notification identifier</param>
        /// <returns>Notification log entry or null if not found</returns>
        public async Task<NotificationLogEntry?> GetNotificationLogAsync(string notificationId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(notificationId))
                    return null;

                var logEntry = _notificationHistory.TryGetValue(notificationId, out var entry) 
                    ? entry 
                    : null;

                await Task.CompletedTask;
                return logEntry;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get notification log: {NotificationId}", notificationId);
                return null;
            }
        }

        /// <summary>
        /// Gets notification history for a specific test suite
        /// </summary>
        /// <param name="testSuiteName">Name of the test suite</param>
        /// <param name="maxEntries">Maximum number of entries to return</param>
        /// <returns>List of notification log entries</returns>
        public async Task<List<NotificationLogEntry>> GetTestSuiteNotificationHistoryAsync(
            string testSuiteName, 
            int maxEntries = 100)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(testSuiteName))
                    return new List<NotificationLogEntry>();

                var key = testSuiteName.ToLowerInvariant();
                var history = _testSuiteHistory.TryGetValue(key, out var entries) 
                    ? entries.OrderByDescending(e => e.AttemptTime).Take(maxEntries).ToList()
                    : new List<NotificationLogEntry>();

                await Task.CompletedTask;
                return history;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get test suite notification history: {TestSuiteName}", testSuiteName);
                return new List<NotificationLogEntry>();
            }
        }

        /// <summary>
        /// Gets all notification history within a time range
        /// </summary>
        /// <param name="startTime">Start time for the range</param>
        /// <param name="endTime">End time for the range</param>
        /// <param name="notificationType">Optional filter by notification type</param>
        /// <returns>List of notification log entries</returns>
        public async Task<List<NotificationLogEntry>> GetNotificationHistoryAsync(
            DateTime startTime,
            DateTime endTime,
            NotificationType? notificationType = null)
        {
            try
            {
                var allEntries = _notificationHistory.Values
                    .Where(e => e.AttemptTime >= startTime && e.AttemptTime <= endTime);

                if (notificationType.HasValue)
                {
                    allEntries = allEntries.Where(e => e.NotificationType == notificationType.Value);
                }

                var result = allEntries
                    .OrderByDescending(e => e.AttemptTime)
                    .ToList();

                await Task.CompletedTask;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get notification history for time range: {StartTime} - {EndTime}", 
                    startTime, endTime);
                return new List<NotificationLogEntry>();
            }
        }

        /// <summary>
        /// Gets notification statistics for a time period
        /// </summary>
        /// <param name="startTime">Start time for the period</param>
        /// <param name="endTime">End time for the period</param>
        /// <returns>Notification statistics</returns>
        public async Task<NotificationStatistics> GetNotificationStatisticsAsync(
            DateTime startTime,
            DateTime endTime)
        {
            try
            {
                var entries = await GetNotificationHistoryAsync(startTime, endTime);

                var statistics = new NotificationStatistics
                {
                    StartTime = startTime,
                    EndTime = endTime,
                    TotalNotifications = entries.Count,
                    SuccessfulNotifications = entries.Count(e => e.Status == NotificationStatus.Sent),
                    FailedNotifications = entries.Count(e => e.Status == NotificationStatus.Failed),
                    PendingNotifications = entries.Count(e => e.Status == NotificationStatus.Pending),
                    RetryingNotifications = entries.Count(e => e.Status == NotificationStatus.Retrying),
                    NotificationsByType = entries
                        .GroupBy(e => e.NotificationType)
                        .ToDictionary(g => g.Key, g => g.Count())
                };

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get notification statistics for time range: {StartTime} - {EndTime}", 
                    startTime, endTime);
                
                return new NotificationStatistics
                {
                    StartTime = startTime,
                    EndTime = endTime
                };
            }
        }

        /// <summary>
        /// Clears old notification history entries
        /// </summary>
        /// <param name="olderThan">Remove entries older than this date</param>
        /// <returns>Number of entries removed</returns>
        public async Task<int> CleanupOldHistoryAsync(DateTime olderThan)
        {
            try
            {
                var removedCount = 0;

                // Clean up main notification history
                var keysToRemove = _notificationHistory
                    .Where(kvp => kvp.Value.AttemptTime < olderThan)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    if (_notificationHistory.TryRemove(key, out _))
                    {
                        removedCount++;
                    }
                }

                // Clean up test suite history
                lock (_lockObject)
                {
                    foreach (var testSuiteKey in _testSuiteHistory.Keys.ToList())
                    {
                        if (_testSuiteHistory.TryGetValue(testSuiteKey, out var entries))
                        {
                            var filteredEntries = entries.Where(e => e.AttemptTime >= olderThan).ToList();
                            
                            if (filteredEntries.Count != entries.Count)
                            {
                                _testSuiteHistory.TryUpdate(testSuiteKey, filteredEntries, entries);
                            }

                            // Remove empty test suite histories
                            if (filteredEntries.Count == 0)
                            {
                                _testSuiteHistory.TryRemove(testSuiteKey, out _);
                            }
                        }
                    }
                }

                _logger.LogInformation("Cleaned up {Count} old notification history entries", removedCount);

                await Task.CompletedTask;
                return removedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup old notification history");
                return 0;
            }
        }

        /// <summary>
        /// Adds a log entry to test suite history
        /// </summary>
        private void AddToTestSuiteHistory(string testSuiteName, NotificationLogEntry logEntry)
        {
            try
            {
                var key = testSuiteName.ToLowerInvariant();
                
                _testSuiteHistory.AddOrUpdate(
                    key,
                    new List<NotificationLogEntry> { logEntry },
                    (k, existingList) =>
                    {
                        lock (_lockObject)
                        {
                            var newList = existingList.ToList();
                            newList.Add(logEntry);
                            
                            // Keep only the most recent 1000 entries per test suite
                            if (newList.Count > 1000)
                            {
                                newList = newList
                                    .OrderByDescending(e => e.AttemptTime)
                                    .Take(1000)
                                    .ToList();
                            }
                            
                            return newList;
                        }
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add entry to test suite history: {TestSuiteName}", testSuiteName);
            }
        }

        /// <summary>
        /// Logs an email validation error
        /// </summary>
        /// <param name="emailAddress">Email address that failed validation</param>
        /// <param name="errorType">Type of validation error</param>
        /// <param name="errorMessage">Detailed error message</param>
        public async Task LogValidationErrorAsync(string emailAddress, string errorType, string errorMessage)
        {
            try
            {
                var logEntry = new NotificationLogEntry
                {
                    NotificationId = $"validation-error-{Guid.NewGuid()}",
                    NotificationType = NotificationType.TestFailure, // Use TestFailure as a general error type
                    Recipients = new List<string> { emailAddress ?? "unknown" },
                    Subject = $"Email Validation Error: {errorType}",
                    TestSuiteName = "EmailValidation",
                    AttemptTime = DateTime.UtcNow,
                    CompletionTime = DateTime.UtcNow,
                    Status = NotificationStatus.Failed,
                    ErrorMessage = $"Validation Error - {errorType}: {errorMessage}"
                };

                _notificationHistory.TryAdd(logEntry.NotificationId, logEntry);
                AddToTestSuiteHistory("EmailValidation", logEntry);

                _logger.LogWarning("Email validation error logged: {EmailAddress} - {ErrorType}: {ErrorMessage}", 
                    emailAddress, errorType, errorMessage);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log email validation error for: {EmailAddress}", emailAddress);
            }
        }
    }
}