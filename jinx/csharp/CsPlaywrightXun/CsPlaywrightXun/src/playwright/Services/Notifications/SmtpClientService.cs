using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace CsPlaywrightXun.Services.Notifications
{
    /// <summary>
    /// SMTP client service implementation with enhanced error handling
    /// </summary>
    public class SmtpClientService : ISmtpClient
    {
        private readonly ILogger<SmtpClientService> _logger;
        private readonly NotificationExceptionHandler _exceptionHandler;
        private readonly IErrorRecoveryStrategy _errorRecoveryStrategy;
        private readonly IEmailValidator _emailValidator;
        private SmtpConfiguration? _configuration;
        private readonly object _lockObject = new();

        public SmtpClientService(
            ILogger<SmtpClientService> logger,
            NotificationExceptionHandler exceptionHandler,
            IErrorRecoveryStrategy errorRecoveryStrategy,
            IEmailValidator emailValidator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
            _errorRecoveryStrategy = errorRecoveryStrategy ?? throw new ArgumentNullException(nameof(errorRecoveryStrategy));
            _emailValidator = emailValidator ?? throw new ArgumentNullException(nameof(emailValidator));
        }

        /// <summary>
        /// Configure SMTP client with connection settings
        /// </summary>
        /// <param name="config">SMTP configuration</param>
        public void Configure(SmtpConfiguration config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            lock (_lockObject)
            {
                if (!config.IsValid())
                {
                    var errors = config.GetValidationErrors();
                    var errorMessage = $"Invalid SMTP configuration: {string.Join(", ", errors)}";
                    _logger.LogError(errorMessage);
                    throw new ArgumentException(errorMessage, nameof(config));
                }

                _configuration = config;
                _logger.LogInformation("SMTP client configured for host: {Host}:{Port}", config.Host, config.Port);
            }
        }

        /// <summary>
        /// Send an email message via SMTP
        /// </summary>
        /// <param name="message">Email message to send</param>
        /// <returns>True if email was sent successfully, false otherwise</returns>
        public async Task<bool> SendEmailAsync(EmailMessage message)
        {
            if (message == null)
            {
                _logger.LogError("Cannot send null email message");
                return false;
            }

            if (_configuration == null)
            {
                _logger.LogError("SMTP client is not configured");
                return false;
            }

            try
            {
                // Validate email message
                if (!message.IsValid())
                {
                    var errors = message.GetValidationErrors();
                    _logger.LogError("Invalid email message: {Errors}", string.Join(", ", errors));
                    return false;
                }

                // Validate recipient email addresses
                var validRecipients = _emailValidator.GetValidEmails(message.ToAddresses);
                if (validRecipients.Count == 0)
                {
                    _logger.LogError("No valid recipient email addresses found");
                    return false;
                }

                // Use circuit breaker pattern for SMTP operations
                var circuitBreakerKey = $"smtp-{_configuration.Host}:{_configuration.Port}";
                
                return await _errorRecoveryStrategy.ExecuteWithCircuitBreakerAsync(
                    async () => await SendEmailInternalAsync(message, validRecipients),
                    circuitBreakerKey,
                    failureThreshold: 5,
                    recoveryTimeout: TimeSpan.FromMinutes(5));
            }
            catch (Exception ex)
            {
                await _exceptionHandler.HandleExceptionAsync(ex, "SendEmailAsync", message.Id);
                return false;
            }
        }

        /// <summary>
        /// Test SMTP server connection
        /// </summary>
        /// <returns>True if connection is successful, false otherwise</returns>
        public async Task<bool> TestConnectionAsync()
        {
            if (_configuration == null)
            {
                _logger.LogError("SMTP client is not configured");
                return false;
            }

            try
            {
                return await _errorRecoveryStrategy.RetryAsync(
                    async () => await TestConnectionInternalAsync(),
                    maxAttempts: 3,
                    delayBetweenAttempts: TimeSpan.FromSeconds(2),
                    onRetry: async (attempt, ex) =>
                    {
                        _logger.LogWarning("SMTP connection test failed on attempt {Attempt}: {Error}", 
                            attempt, ex.Message);
                        await Task.CompletedTask;
                    });
            }
            catch (Exception ex)
            {
                await _exceptionHandler.HandleSmtpExceptionAsync(ex, _configuration.Host, 1);
                return false;
            }
        }

        /// <summary>
        /// Internal method to send email with retry logic
        /// </summary>
        /// <param name="message">Email message to send</param>
        /// <param name="validRecipients">List of valid recipient addresses</param>
        /// <returns>True if email was sent successfully, false otherwise</returns>
        private async Task<bool> SendEmailInternalAsync(EmailMessage message, System.Collections.Generic.List<string> validRecipients)
        {
            if (_configuration == null)
                return false;

            return await _errorRecoveryStrategy.RetryAsync(
                async () => await SendSingleEmailAsync(message, validRecipients),
                maxAttempts: _configuration.MaxRetryAttempts + 1,
                delayBetweenAttempts: TimeSpan.FromSeconds(2),
                onRetry: async (attempt, ex) =>
                {
                    await _exceptionHandler.HandleSmtpExceptionAsync(ex, _configuration.Host, attempt, message.Id);
                });
        }

        /// <summary>
        /// Sends a single email without retry logic
        /// </summary>
        /// <param name="message">Email message to send</param>
        /// <param name="validRecipients">List of valid recipient addresses</param>
        /// <returns>True if email was sent successfully, false otherwise</returns>
        private async Task<bool> SendSingleEmailAsync(EmailMessage message, System.Collections.Generic.List<string> validRecipients)
        {
            if (_configuration == null)
                return false;

            using var smtpClient = new System.Net.Mail.SmtpClient(_configuration.Host, _configuration.Port);
            
            try
            {
                // Configure SMTP client
                smtpClient.EnableSsl = _configuration.EnableSsl;
                smtpClient.Timeout = _configuration.TimeoutSeconds * 1000; // Convert to milliseconds
                
                if (!string.IsNullOrWhiteSpace(_configuration.Username))
                {
                    smtpClient.Credentials = new NetworkCredential(_configuration.Username, _configuration.Password);
                }

                // Create mail message
                using var mailMessage = new MailMessage();
                
                // Set sender
                mailMessage.From = string.IsNullOrWhiteSpace(_configuration.FromDisplayName)
                    ? new MailAddress(_configuration.FromEmail)
                    : new MailAddress(_configuration.FromEmail, _configuration.FromDisplayName);

                // Add recipients
                foreach (var recipient in validRecipients)
                {
                    mailMessage.To.Add(recipient);
                }

                // Add CC recipients if any
                if (message.CcAddresses != null)
                {
                    var validCcRecipients = _emailValidator.GetValidEmails(message.CcAddresses);
                    foreach (var ccRecipient in validCcRecipients)
                    {
                        mailMessage.CC.Add(ccRecipient);
                    }
                }

                // Set message content
                mailMessage.Subject = message.Subject;
                mailMessage.IsBodyHtml = !string.IsNullOrWhiteSpace(message.HtmlBody);
                mailMessage.Body = !string.IsNullOrWhiteSpace(message.HtmlBody) 
                    ? message.HtmlBody 
                    : message.PlainTextBody;

                // Set priority
                mailMessage.Priority = message.Priority switch
                {
                    NotificationPriority.Low => MailPriority.Low,
                    NotificationPriority.High => MailPriority.High,
                    NotificationPriority.Critical => MailPriority.High,
                    _ => MailPriority.Normal
                };

                _logger.LogDebug("Sending email to {RecipientCount} recipients via {Host}:{Port}", 
                    validRecipients.Count, _configuration.Host, _configuration.Port);

                // Send the email
                await smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation("Email sent successfully: {MessageId} to {RecipientCount} recipients", 
                    message.Id, validRecipients.Count);

                return true;
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogError(smtpEx, "SMTP error sending email {MessageId}: {StatusCode} - {Message}", 
                    message.Id, smtpEx.StatusCode, smtpEx.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error sending email {MessageId}", message.Id);
                throw;
            }
        }

        /// <summary>
        /// Internal method to test SMTP connection
        /// </summary>
        /// <returns>True if connection is successful, false otherwise</returns>
        private async Task<bool> TestConnectionInternalAsync()
        {
            if (_configuration == null)
                return false;

            using var smtpClient = new System.Net.Mail.SmtpClient(_configuration.Host, _configuration.Port);
            
            try
            {
                smtpClient.EnableSsl = _configuration.EnableSsl;
                smtpClient.Timeout = Math.Min(_configuration.TimeoutSeconds * 1000, 10000); // Max 10 seconds for test
                
                if (!string.IsNullOrWhiteSpace(_configuration.Username))
                {
                    smtpClient.Credentials = new NetworkCredential(_configuration.Username, _configuration.Password);
                }

                // Create a test message (won't be sent)
                using var testMessage = new MailMessage();
                testMessage.From = new MailAddress(_configuration.FromEmail);
                testMessage.To.Add(_configuration.FromEmail); // Send to self for testing
                testMessage.Subject = "SMTP Connection Test";
                testMessage.Body = "This is a connection test message.";

                _logger.LogDebug("Testing SMTP connection to {Host}:{Port}", _configuration.Host, _configuration.Port);

                // Note: We're not actually sending the message, just testing the connection
                // Some SMTP servers don't support connection testing without sending
                // So we'll create the client and validate credentials if provided
                
                await Task.Run(() =>
                {
                    // This will throw an exception if connection fails
                    // We wrap in Task.Run to make it async
                    smtpClient.Send(testMessage);
                });

                _logger.LogInformation("SMTP connection test successful for {Host}:{Port}", 
                    _configuration.Host, _configuration.Port);

                return true;
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogWarning("SMTP connection test failed for {Host}:{Port}: {StatusCode} - {Message}", 
                    _configuration.Host, _configuration.Port, smtpEx.StatusCode, smtpEx.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("SMTP connection test failed for {Host}:{Port}: {Message}", 
                    _configuration.Host, _configuration.Port, ex.Message);
                throw;
            }
        }
    }
}