using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CsPlaywrightXun.Services.Notifications
{
    /// <summary>
    /// Global exception handler for notification system
    /// </summary>
    public class NotificationExceptionHandler
    {
        private readonly ILogger<NotificationExceptionHandler> _logger;
        private readonly INotificationLogger _notificationLogger;

        public NotificationExceptionHandler(
            ILogger<NotificationExceptionHandler> logger,
            INotificationLogger notificationLogger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _notificationLogger = notificationLogger ?? throw new ArgumentNullException(nameof(notificationLogger));
        }

        /// <summary>
        /// Handles exceptions that occur during notification operations
        /// </summary>
        /// <param name="exception">The exception that occurred</param>
        /// <param name="context">Context information about the operation</param>
        /// <param name="notificationId">Optional notification ID if available</param>
        /// <returns>Task representing the async operation</returns>
        public async Task HandleExceptionAsync(
            Exception exception, 
            string context, 
            string? notificationId = null)
        {
            try
            {
                var errorDetails = ExtractErrorDetails(exception);
                
                _logger.LogError(exception, 
                    "Notification system error in {Context}: {ErrorType} - {ErrorMessage}", 
                    context, errorDetails.ErrorType, errorDetails.Message);

                // Log to notification logger if we have a notification ID
                if (!string.IsNullOrWhiteSpace(notificationId))
                {
                    await _notificationLogger.UpdateNotificationStatusAsync(
                        notificationId, 
                        NotificationStatus.Failed, 
                        $"{errorDetails.ErrorType}: {errorDetails.Message}");
                }

                // Log validation errors separately
                if (IsValidationError(exception))
                {
                    await LogValidationErrorAsync(exception, context);
                }

                // Determine if this is a recoverable error
                var isRecoverable = IsRecoverableError(exception);
                
                if (isRecoverable)
                {
                    _logger.LogInformation("Error is potentially recoverable: {ErrorType}", errorDetails.ErrorType);
                }
                else
                {
                    _logger.LogWarning("Error is not recoverable: {ErrorType}", errorDetails.ErrorType);
                }
            }
            catch (Exception handlerException)
            {
                // Prevent exception handler from throwing
                _logger.LogCritical(handlerException, 
                    "Critical error in notification exception handler while processing: {OriginalContext}", 
                    context);
            }
        }

        /// <summary>
        /// Handles SMTP-specific exceptions with retry logic
        /// </summary>
        /// <param name="exception">SMTP exception</param>
        /// <param name="smtpHost">SMTP server host</param>
        /// <param name="attemptNumber">Current attempt number</param>
        /// <param name="notificationId">Notification ID</param>
        /// <returns>Task representing the async operation</returns>
        public async Task HandleSmtpExceptionAsync(
            Exception exception, 
            string smtpHost, 
            int attemptNumber, 
            string? notificationId = null)
        {
            try
            {
                var errorDetails = ExtractErrorDetails(exception);
                var context = $"SMTP Operation (Host: {smtpHost}, Attempt: {attemptNumber})";

                _logger.LogError(exception, 
                    "SMTP error on attempt {AttemptNumber} to {SmtpHost}: {ErrorType} - {ErrorMessage}", 
                    attemptNumber, smtpHost, errorDetails.ErrorType, errorDetails.Message);

                // Update notification status to retrying if this is a retry attempt
                if (!string.IsNullOrWhiteSpace(notificationId))
                {
                    var status = attemptNumber > 1 ? NotificationStatus.Retrying : NotificationStatus.Failed;
                    await _notificationLogger.UpdateNotificationStatusAsync(
                        notificationId, 
                        status, 
                        $"SMTP Attempt {attemptNumber}: {errorDetails.Message}");
                }

                // Log specific SMTP error types
                await LogSmtpErrorDetailsAsync(exception, smtpHost, attemptNumber);
            }
            catch (Exception handlerException)
            {
                _logger.LogCritical(handlerException, 
                    "Critical error handling SMTP exception for host: {SmtpHost}", smtpHost);
            }
        }

        /// <summary>
        /// Handles template rendering exceptions
        /// </summary>
        /// <param name="exception">Template exception</param>
        /// <param name="templateName">Name of the template</param>
        /// <param name="notificationId">Notification ID</param>
        /// <returns>Task representing the async operation</returns>
        public async Task HandleTemplateExceptionAsync(
            Exception exception, 
            string templateName, 
            string? notificationId = null)
        {
            try
            {
                var errorDetails = ExtractErrorDetails(exception);
                var context = $"Template Rendering (Template: {templateName})";

                _logger.LogError(exception, 
                    "Template rendering error for {TemplateName}: {ErrorType} - {ErrorMessage}", 
                    templateName, errorDetails.ErrorType, errorDetails.Message);

                if (!string.IsNullOrWhiteSpace(notificationId))
                {
                    await _notificationLogger.UpdateNotificationStatusAsync(
                        notificationId, 
                        NotificationStatus.Failed, 
                        $"Template Error ({templateName}): {errorDetails.Message}");
                }

                // Suggest fallback template if available
                _logger.LogInformation("Consider using fallback template for: {TemplateName}", templateName);
            }
            catch (Exception handlerException)
            {
                _logger.LogCritical(handlerException, 
                    "Critical error handling template exception for: {TemplateName}", templateName);
            }
        }

        /// <summary>
        /// Extracts structured error details from an exception
        /// </summary>
        /// <param name="exception">Exception to analyze</param>
        /// <returns>Structured error details</returns>
        private static ErrorDetails ExtractErrorDetails(Exception exception)
        {
            var errorType = exception.GetType().Name;
            var message = exception.Message;

            // Extract more specific error information based on exception type
            switch (exception)
            {
                case System.Net.Mail.SmtpException smtpEx:
                    return new ErrorDetails
                    {
                        ErrorType = "SMTP_ERROR",
                        Message = $"SMTP Status: {smtpEx.StatusCode}, Message: {smtpEx.Message}",
                        IsRecoverable = IsRecoverableSmtpError(smtpEx)
                    };

                case System.Net.Sockets.SocketException socketEx:
                    return new ErrorDetails
                    {
                        ErrorType = "NETWORK_ERROR",
                        Message = $"Socket Error: {socketEx.SocketErrorCode}, Message: {socketEx.Message}",
                        IsRecoverable = true // Network errors are often temporary
                    };

                case TimeoutException timeoutEx:
                    return new ErrorDetails
                    {
                        ErrorType = "TIMEOUT_ERROR",
                        Message = timeoutEx.Message,
                        IsRecoverable = true // Timeouts can be retried
                    };

                case FormatException formatEx:
                    return new ErrorDetails
                    {
                        ErrorType = "FORMAT_ERROR",
                        Message = formatEx.Message,
                        IsRecoverable = false // Format errors are usually permanent
                    };

                case ArgumentException argEx:
                    return new ErrorDetails
                    {
                        ErrorType = "ARGUMENT_ERROR",
                        Message = argEx.Message,
                        IsRecoverable = false // Argument errors are usually permanent
                    };

                case UnauthorizedAccessException authEx:
                    return new ErrorDetails
                    {
                        ErrorType = "AUTHENTICATION_ERROR",
                        Message = authEx.Message,
                        IsRecoverable = false // Auth errors need configuration fix
                    };

                default:
                    return new ErrorDetails
                    {
                        ErrorType = errorType,
                        Message = message,
                        IsRecoverable = false // Unknown errors are assumed non-recoverable
                    };
            }
        }

        /// <summary>
        /// Determines if an error is recoverable (can be retried)
        /// </summary>
        /// <param name="exception">Exception to check</param>
        /// <returns>True if recoverable, false otherwise</returns>
        private static bool IsRecoverableError(Exception exception)
        {
            return exception switch
            {
                System.Net.Sockets.SocketException => true,
                TimeoutException => true,
                System.Net.Mail.SmtpException smtpEx => IsRecoverableSmtpError(smtpEx),
                _ => false
            };
        }

        /// <summary>
        /// Determines if an SMTP error is recoverable
        /// </summary>
        /// <param name="smtpException">SMTP exception to check</param>
        /// <returns>True if recoverable, false otherwise</returns>
        private static bool IsRecoverableSmtpError(System.Net.Mail.SmtpException smtpException)
        {
            return smtpException.StatusCode switch
            {
                System.Net.Mail.SmtpStatusCode.MailboxBusy => true,
                System.Net.Mail.SmtpStatusCode.InsufficientStorage => true,
                System.Net.Mail.SmtpStatusCode.LocalErrorInProcessing => true,
                System.Net.Mail.SmtpStatusCode.TransactionFailed => true,
                _ => false
            };
        }

        /// <summary>
        /// Checks if an exception is related to validation
        /// </summary>
        /// <param name="exception">Exception to check</param>
        /// <returns>True if validation error, false otherwise</returns>
        private static bool IsValidationError(Exception exception)
        {
            return exception is FormatException || 
                   exception is ArgumentException ||
                   exception.Message.Contains("validation", StringComparison.OrdinalIgnoreCase) ||
                   exception.Message.Contains("invalid", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Logs validation errors to the notification logger
        /// </summary>
        /// <param name="exception">Validation exception</param>
        /// <param name="context">Context where validation failed</param>
        /// <returns>Task representing the async operation</returns>
        private async Task LogValidationErrorAsync(Exception exception, string context)
        {
            try
            {
                await _notificationLogger.LogValidationErrorAsync(
                    context,
                    exception.GetType().Name,
                    exception.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log validation error for context: {Context}", context);
            }
        }

        /// <summary>
        /// Logs detailed SMTP error information
        /// </summary>
        /// <param name="exception">SMTP exception</param>
        /// <param name="smtpHost">SMTP host</param>
        /// <param name="attemptNumber">Attempt number</param>
        /// <returns>Task representing the async operation</returns>
        private async Task LogSmtpErrorDetailsAsync(Exception exception, string smtpHost, int attemptNumber)
        {
            try
            {
                var errorContext = $"SMTP-{smtpHost}-Attempt{attemptNumber}";
                
                if (exception is System.Net.Mail.SmtpException smtpEx)
                {
                    await _notificationLogger.LogValidationErrorAsync(
                        errorContext,
                        $"SMTP_{smtpEx.StatusCode}",
                        $"SMTP Error on attempt {attemptNumber}: {smtpEx.Message}");
                }
                else
                {
                    await _notificationLogger.LogValidationErrorAsync(
                        errorContext,
                        exception.GetType().Name,
                        $"Network error on attempt {attemptNumber}: {exception.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log SMTP error details for host: {SmtpHost}", smtpHost);
            }
        }

        /// <summary>
        /// Structured error details
        /// </summary>
        private class ErrorDetails
        {
            public string ErrorType { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public bool IsRecoverable { get; set; }
        }
    }
}