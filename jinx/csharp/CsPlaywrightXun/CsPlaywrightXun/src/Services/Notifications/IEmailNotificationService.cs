using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CsPlaywrightXun.Services.Notifications
{
    /// <summary>
    /// Core email notification service interface for sending test-related notifications
    /// </summary>
    public interface IEmailNotificationService
    {
        /// <summary>
        /// Send notification when test execution starts
        /// </summary>
        /// <param name="context">Test execution context</param>
        /// <returns>Task representing the async operation</returns>
        Task SendTestStartNotificationAsync(TestExecutionContext context);

        /// <summary>
        /// Send notification when test execution completes successfully
        /// </summary>
        /// <param name="result">Test execution result</param>
        /// <returns>Task representing the async operation</returns>
        Task SendTestSuccessNotificationAsync(TestSuiteResult result);

        /// <summary>
        /// Send notification when test execution fails
        /// </summary>
        /// <param name="result">Test execution result with failure details</param>
        /// <returns>Task representing the async operation</returns>
        Task SendTestFailureNotificationAsync(TestSuiteResult result);

        /// <summary>
        /// Send notification when test report is generated
        /// </summary>
        /// <param name="reportInfo">Report generation information</param>
        /// <returns>Task representing the async operation</returns>
        Task SendReportGeneratedNotificationAsync(ReportInfo reportInfo);

        /// <summary>
        /// Validate SMTP configuration settings
        /// </summary>
        /// <returns>True if configuration is valid, false otherwise</returns>
        Task<bool> ValidateSmtpConfigurationAsync();

        /// <summary>
        /// Get the status of a specific notification
        /// </summary>
        /// <param name="notificationId">Unique notification identifier</param>
        /// <returns>Current notification status</returns>
        Task<NotificationStatus> GetNotificationStatusAsync(string notificationId);
    }

    /// <summary>
    /// Test execution context for notification purposes
    /// </summary>
    public class TestExecutionContext
    {
        public string TestSuiteName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public string Environment { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Report generation information
    /// </summary>
    public class ReportInfo
    {
        public string ReportName { get; set; } = string.Empty;
        public string ReportPath { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public string ReportType { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Notification status enumeration
    /// </summary>
    public enum NotificationStatus
    {
        Pending,
        Sent,
        Failed,
        Retrying
    }
}