using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CsPlaywrightXun.Services.Notifications
{
    /// <summary>
    /// Core email notification service implementation with enhanced error handling
    /// </summary>
    public class EmailNotificationService : IEmailNotificationService
    {
        private readonly ISmtpClient _smtpClient;
        private readonly IEmailTemplateEngine _templateEngine;
        private readonly IRecipientManager _recipientManager;
        private readonly INotificationLogger _notificationLogger;
        private readonly ILogger<EmailNotificationService> _logger;
        private readonly NotificationExceptionHandler _exceptionHandler;
        private readonly IErrorRecoveryStrategy _errorRecoveryStrategy;
        private readonly IEmailValidator _emailValidator;
        private readonly Dictionary<string, NotificationStatus> _notificationStatuses;
        private readonly List<NotificationRule> _notificationRules;

        public EmailNotificationService(
            ISmtpClient smtpClient,
            IEmailTemplateEngine templateEngine,
            IRecipientManager recipientManager,
            INotificationLogger notificationLogger,
            ILogger<EmailNotificationService> logger,
            NotificationExceptionHandler exceptionHandler,
            IErrorRecoveryStrategy errorRecoveryStrategy,
            IEmailValidator emailValidator,
            List<NotificationRule>? notificationRules = null)
        {
            _smtpClient = smtpClient ?? throw new ArgumentNullException(nameof(smtpClient));
            _templateEngine = templateEngine ?? throw new ArgumentNullException(nameof(templateEngine));
            _recipientManager = recipientManager ?? throw new ArgumentNullException(nameof(recipientManager));
            _notificationLogger = notificationLogger ?? throw new ArgumentNullException(nameof(notificationLogger));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
            _errorRecoveryStrategy = errorRecoveryStrategy ?? throw new ArgumentNullException(nameof(errorRecoveryStrategy));
            _emailValidator = emailValidator ?? throw new ArgumentNullException(nameof(emailValidator));
            _notificationStatuses = new Dictionary<string, NotificationStatus>();
            _notificationRules = notificationRules ?? new List<NotificationRule>();
        }

        /// <summary>
        /// Send notification when test execution starts
        /// </summary>
        public async Task SendTestStartNotificationAsync(TestExecutionContext context)
        {
            try
            {
                _logger.LogInformation("Sending test start notification for suite: {TestSuiteName}", context.TestSuiteName);

                var contextDict = new Dictionary<string, object>
                {
                    ["TestSuiteName"] = context.TestSuiteName,
                    ["Environment"] = context.Environment
                };

                var recipients = await _recipientManager.GetRecipientsAsync(
                    NotificationType.TestStart, 
                    _notificationRules, 
                    contextDict);

                if (recipients == null || recipients.Count == 0)
                {
                    _logger.LogWarning("No recipients configured for test start notifications");
                    return;
                }

                var templateModel = new
                {
                    TestSuiteName = context.TestSuiteName,
                    StartTime = context.StartTime,
                    Environment = context.Environment,
                    Metadata = context.Metadata
                };

                // Use fallback strategy for template rendering
                var htmlContent = await _errorRecoveryStrategy.ExecuteWithFallbackAsync(
                    // Primary: render with template
                    async () => await _templateEngine.RenderTemplateAsync("TestStartTemplate", templateModel),
                    // Fallback: use simple HTML
                    async () => await Task.FromResult(CreateFallbackTestStartHtml(templateModel)),
                    // Use fallback for template errors
                    ex => ex.Message.Contains("template", StringComparison.OrdinalIgnoreCase));
                
                var message = new EmailMessage
                {
                    ToAddresses = recipients,
                    Subject = $"Test Execution Started: {context.TestSuiteName}",
                    HtmlBody = htmlContent,
                    Priority = NotificationPriority.Normal
                };

                await SendEmailWithStatusTrackingAsync(message, context.TestSuiteName);
            }
            catch (Exception ex)
            {
                await _exceptionHandler.HandleExceptionAsync(ex, "SendTestStartNotification", null);
                _logger.LogError(ex, "Failed to send test start notification for suite: {TestSuiteName}", context.TestSuiteName);
                throw;
            }
        }

        /// <summary>
        /// Send notification when test execution completes successfully
        /// </summary>
        public async Task SendTestSuccessNotificationAsync(TestSuiteResult result)
        {
            try
            {
                _logger.LogInformation("Sending test success notification for suite: {TestSuiteName}", result.TestSuiteName);

                var contextDict = new Dictionary<string, object>
                {
                    ["TestSuiteName"] = result.TestSuiteName,
                    ["Environment"] = result.Environment,
                    ["PassRate"] = result.PassRate
                };

                var recipients = await _recipientManager.GetRecipientsAsync(
                    NotificationType.TestSuccess, 
                    _notificationRules, 
                    contextDict);

                if (recipients == null || recipients.Count == 0)
                {
                    _logger.LogWarning("No recipients configured for test success notifications");
                    return;
                }

                var templateModel = new
                {
                    TestSuiteName = result.TestSuiteName,
                    StartTime = result.StartTime,
                    EndTime = result.EndTime,
                    Duration = result.Duration,
                    TotalTests = result.TotalTests,
                    PassedTests = result.PassedTests,
                    FailedTests = result.FailedTests,
                    SkippedTests = result.SkippedTests,
                    PassRate = result.PassRate,
                    Environment = result.Environment,
                    Metadata = result.Metadata
                };

                var htmlContent = await _templateEngine.RenderTemplateAsync("TestSuccessTemplate", templateModel);
                
                var message = new EmailMessage
                {
                    ToAddresses = recipients,
                    Subject = $"Test Execution Successful: {result.TestSuiteName} ({result.PassRate:F1}% Pass Rate)",
                    HtmlBody = htmlContent,
                    Priority = NotificationPriority.Normal
                };

                await SendEmailWithStatusTrackingAsync(message, result.TestSuiteName);

                // Update failure count (reset on success)
                _recipientManager.UpdateFailureCount(result.TestSuiteName, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send test success notification for suite: {TestSuiteName}", result.TestSuiteName);
                throw;
            }
        }

        /// <summary>
        /// Send notification when test execution fails
        /// </summary>
        public async Task SendTestFailureNotificationAsync(TestSuiteResult result)
        {
            try
            {
                _logger.LogInformation("Sending test failure notification for suite: {TestSuiteName}", result.TestSuiteName);

                var notificationType = result.HasCriticalFailures ? NotificationType.CriticalFailure : NotificationType.TestFailure;
                
                var contextDict = new Dictionary<string, object>
                {
                    ["TestSuiteName"] = result.TestSuiteName,
                    ["Environment"] = result.Environment,
                    ["PassRate"] = result.PassRate,
                    ["HasCriticalFailures"] = result.HasCriticalFailures,
                    ["FailedTests"] = result.FailedTests
                };

                var recipients = await _recipientManager.GetRecipientsAsync(
                    notificationType, 
                    _notificationRules, 
                    contextDict);
                
                if (recipients == null || recipients.Count == 0)
                {
                    _logger.LogWarning("No recipients configured for test failure notifications");
                    return;
                }

                var templateModel = new
                {
                    TestSuiteName = result.TestSuiteName,
                    StartTime = result.StartTime,
                    EndTime = result.EndTime,
                    Duration = result.Duration,
                    TotalTests = result.TotalTests,
                    PassedTests = result.PassedTests,
                    FailedTests = result.FailedTests,
                    SkippedTests = result.SkippedTests,
                    PassRate = result.PassRate,
                    FailedTestCases = result.FailedTestCases,
                    HasCriticalFailures = result.HasCriticalFailures,
                    Environment = result.Environment,
                    Metadata = result.Metadata
                };

                var htmlContent = await _templateEngine.RenderTemplateAsync("TestFailureTemplate", templateModel);
                
                var priority = result.HasCriticalFailures ? NotificationPriority.Critical : NotificationPriority.High;
                var subjectPrefix = result.HasCriticalFailures ? "CRITICAL FAILURE" : "Test Execution Failed";
                
                var message = new EmailMessage
                {
                    ToAddresses = recipients,
                    Subject = $"{subjectPrefix}: {result.TestSuiteName} ({result.FailedTests} failures)",
                    HtmlBody = htmlContent,
                    Priority = priority
                };

                await SendEmailWithStatusTrackingAsync(message, result.TestSuiteName);

                // Update failure count
                _recipientManager.UpdateFailureCount(result.TestSuiteName, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send test failure notification for suite: {TestSuiteName}", result.TestSuiteName);
                throw;
            }
        }

        /// <summary>
        /// Send notification when test report is generated
        /// </summary>
        public async Task SendReportGeneratedNotificationAsync(ReportInfo reportInfo)
        {
            try
            {
                _logger.LogInformation("Sending report generated notification for report: {ReportName}", reportInfo.ReportName);

                var contextDict = new Dictionary<string, object>
                {
                    ["ReportName"] = reportInfo.ReportName,
                    ["ReportType"] = reportInfo.ReportType
                };

                var recipients = await _recipientManager.GetRecipientsAsync(
                    NotificationType.ReportGenerated, 
                    _notificationRules, 
                    contextDict);

                if (recipients == null || recipients.Count == 0)
                {
                    _logger.LogWarning("No recipients configured for report generated notifications");
                    return;
                }

                var templateModel = new
                {
                    ReportName = reportInfo.ReportName,
                    ReportPath = reportInfo.ReportPath,
                    GeneratedAt = reportInfo.GeneratedAt,
                    ReportType = reportInfo.ReportType,
                    Metadata = reportInfo.Metadata
                };

                var htmlContent = await _templateEngine.RenderTemplateAsync("ReportGeneratedTemplate", templateModel);
                
                var message = new EmailMessage
                {
                    ToAddresses = recipients,
                    Subject = $"Test Report Generated: {reportInfo.ReportName}",
                    HtmlBody = htmlContent,
                    Priority = NotificationPriority.Normal
                };

                await SendEmailWithStatusTrackingAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send report generated notification for report: {ReportName}", reportInfo.ReportName);
                throw;
            }
        }

        /// <summary>
        /// Validate SMTP configuration settings
        /// </summary>
        public async Task<bool> ValidateSmtpConfigurationAsync()
        {
            try
            {
                _logger.LogInformation("Validating SMTP configuration");
                return await _smtpClient.TestConnectionAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SMTP configuration validation failed");
                return false;
            }
        }

        /// <summary>
        /// Get the status of a specific notification
        /// </summary>
        public async Task<NotificationStatus> GetNotificationStatusAsync(string notificationId)
        {
            if (string.IsNullOrWhiteSpace(notificationId))
            {
                throw new ArgumentException("Notification ID cannot be null or empty", nameof(notificationId));
            }

            // First check the notification logger
            var loggedStatus = await _notificationLogger.GetNotificationStatusAsync(notificationId);
            if (loggedStatus.HasValue)
            {
                return loggedStatus.Value;
            }

            // Fallback to in-memory status tracking
            return _notificationStatuses.TryGetValue(notificationId, out var status) 
                ? status 
                : NotificationStatus.Pending;
        }

        /// <summary>
        /// Send email with status tracking and enhanced error handling
        /// </summary>
        private async Task SendEmailWithStatusTrackingAsync(EmailMessage message, string? testSuiteName = null)
        {
            ArgumentNullException.ThrowIfNull(message);

            try
            {
                // Validate email message
                if (!message.IsValid())
                {
                    var errors = message.GetValidationErrors();
                    var errorMessage = $"Invalid email message: {string.Join(", ", errors)}";
                    _logger.LogError(errorMessage);
                    throw new ArgumentException(errorMessage);
                }

                // Validate and filter recipient email addresses
                var validRecipients = _emailValidator.GetValidEmails(message.ToAddresses);
                if (validRecipients.Count == 0)
                {
                    var errorMessage = "No valid recipient email addresses found";
                    _logger.LogError(errorMessage);
                    throw new ArgumentException(errorMessage);
                }

                // Update message with validated recipients
                message.ToAddresses = validRecipients;

                // Log the notification attempt
                await _notificationLogger.LogNotificationAttemptAsync(
                    message.Id,
                    GetNotificationTypeFromSubject(message.Subject),
                    message.ToAddresses,
                    message.Subject,
                    testSuiteName);

                _notificationStatuses[message.Id] = NotificationStatus.Pending;
                _logger.LogDebug("Sending email with ID: {MessageId}", message.Id);

                // Use fallback strategy for email sending
                var success = await _errorRecoveryStrategy.ExecuteWithFallbackAsync(
                    // Primary operation: send email normally
                    async () => await _smtpClient.SendEmailAsync(message),
                    // Fallback operation: try with plain text only if HTML fails
                    async () => await SendPlainTextFallbackAsync(message),
                    // Use fallback for template/format errors
                    ex => ex is FormatException || 
                          ex.Message.Contains("template", StringComparison.OrdinalIgnoreCase) ||
                          ex.Message.Contains("html", StringComparison.OrdinalIgnoreCase));
                
                var finalStatus = success ? NotificationStatus.Sent : NotificationStatus.Failed;
                _notificationStatuses[message.Id] = finalStatus;

                // Update the notification logger
                await _notificationLogger.UpdateNotificationStatusAsync(
                    message.Id,
                    finalStatus,
                    success ? null : "Email send operation failed");
                
                if (success)
                {
                    _logger.LogInformation("Email sent successfully with ID: {MessageId}", message.Id);
                }
                else
                {
                    _logger.LogError("Failed to send email with ID: {MessageId}", message.Id);
                }
            }
            catch (Exception ex)
            {
                _notificationStatuses[message.Id] = NotificationStatus.Failed;
                
                // Handle the exception with enhanced error handling
                await _exceptionHandler.HandleExceptionAsync(ex, "SendEmailWithStatusTracking", message.Id);
                
                // Update the notification logger with error
                await _notificationLogger.UpdateNotificationStatusAsync(
                    message.Id,
                    NotificationStatus.Failed,
                    ex.Message);

                _logger.LogError(ex, "Exception occurred while sending email with ID: {MessageId}", message.Id);
                throw;
            }
        }

        /// <summary>
        /// Fallback method to send email as plain text only
        /// </summary>
        /// <param name="originalMessage">Original email message</param>
        /// <returns>True if fallback send was successful</returns>
        private async Task<bool> SendPlainTextFallbackAsync(EmailMessage originalMessage)
        {
            try
            {
                _logger.LogInformation("Attempting plain text fallback for email: {MessageId}", originalMessage.Id);

                var fallbackMessage = new EmailMessage
                {
                    Id = $"{originalMessage.Id}-fallback",
                    ToAddresses = originalMessage.ToAddresses,
                    CcAddresses = originalMessage.CcAddresses,
                    Subject = $"[Plain Text] {originalMessage.Subject}",
                    PlainTextBody = !string.IsNullOrWhiteSpace(originalMessage.PlainTextBody) 
                        ? originalMessage.PlainTextBody 
                        : "This message was sent as plain text due to formatting issues.",
                    HtmlBody = string.Empty, // Clear HTML body for plain text fallback
                    Priority = originalMessage.Priority,
                    CreatedAt = originalMessage.CreatedAt
                };

                return await _smtpClient.SendEmailAsync(fallbackMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Plain text fallback also failed for email: {MessageId}", originalMessage.Id);
                return false;
            }
        }

        /// <summary>
        /// Determines notification type from email subject
        /// </summary>
        private static NotificationType GetNotificationTypeFromSubject(string subject)
        {
            if (string.IsNullOrWhiteSpace(subject))
                return NotificationType.TestStart;

            var lowerSubject = subject.ToLowerInvariant();
            
            if (lowerSubject.Contains("critical failure"))
                return NotificationType.CriticalFailure;
            
            if (lowerSubject.Contains("failed") || lowerSubject.Contains("failure"))
                return NotificationType.TestFailure;
            
            if (lowerSubject.Contains("successful") || lowerSubject.Contains("success"))
                return NotificationType.TestSuccess;
            
            if (lowerSubject.Contains("report generated"))
                return NotificationType.ReportGenerated;
            
            if (lowerSubject.Contains("started"))
                return NotificationType.TestStart;

            return NotificationType.TestStart;
        }

        /// <summary>
        /// Creates fallback HTML for test start notifications
        /// </summary>
        private static string CreateFallbackTestStartHtml(dynamic templateModel)
        {
            return $@"
                <html>
                <body>
                    <h2>Test Execution Started</h2>
                    <p><strong>Test Suite:</strong> {templateModel.TestSuiteName}</p>
                    <p><strong>Start Time:</strong> {templateModel.StartTime}</p>
                    <p><strong>Environment:</strong> {templateModel.Environment}</p>
                    <p>This notification was generated using a fallback template due to template rendering issues.</p>
                </body>
                </html>";
        }

        /// <summary>
        /// Creates fallback HTML for test success notifications
        /// </summary>
        private static string CreateFallbackTestSuccessHtml(dynamic templateModel)
        {
            return $@"
                <html>
                <body>
                    <h2 style='color: green;'>Test Execution Successful</h2>
                    <p><strong>Test Suite:</strong> {templateModel.TestSuiteName}</p>
                    <p><strong>Duration:</strong> {templateModel.Duration}</p>
                    <p><strong>Pass Rate:</strong> {templateModel.PassRate:F1}%</p>
                    <p><strong>Total Tests:</strong> {templateModel.TotalTests}</p>
                    <p><strong>Passed:</strong> {templateModel.PassedTests}</p>
                    <p><strong>Failed:</strong> {templateModel.FailedTests}</p>
                    <p><strong>Skipped:</strong> {templateModel.SkippedTests}</p>
                    <p>This notification was generated using a fallback template due to template rendering issues.</p>
                </body>
                </html>";
        }

        /// <summary>
        /// Creates fallback HTML for test failure notifications
        /// </summary>
        private static string CreateFallbackTestFailureHtml(dynamic templateModel)
        {
            return $@"
                <html>
                <body>
                    <h2 style='color: red;'>Test Execution Failed</h2>
                    <p><strong>Test Suite:</strong> {templateModel.TestSuiteName}</p>
                    <p><strong>Duration:</strong> {templateModel.Duration}</p>
                    <p><strong>Pass Rate:</strong> {templateModel.PassRate:F1}%</p>
                    <p><strong>Total Tests:</strong> {templateModel.TotalTests}</p>
                    <p><strong>Passed:</strong> {templateModel.PassedTests}</p>
                    <p><strong>Failed:</strong> {templateModel.FailedTests}</p>
                    <p><strong>Skipped:</strong> {templateModel.SkippedTests}</p>
                    <p>This notification was generated using a fallback template due to template rendering issues.</p>
                </body>
                </html>";
        }

        /// <summary>
        /// Creates fallback HTML for report generated notifications
        /// </summary>
        private static string CreateFallbackReportGeneratedHtml(dynamic templateModel)
        {
            return $@"
                <html>
                <body>
                    <h2>Test Report Generated</h2>
                    <p><strong>Report Name:</strong> {templateModel.ReportName}</p>
                    <p><strong>Generated At:</strong> {templateModel.GeneratedAt}</p>
                    <p><strong>Report Type:</strong> {templateModel.ReportType}</p>
                    <p>This notification was generated using a fallback template due to template rendering issues.</p>
                </body>
                </html>";
        }
    }
}