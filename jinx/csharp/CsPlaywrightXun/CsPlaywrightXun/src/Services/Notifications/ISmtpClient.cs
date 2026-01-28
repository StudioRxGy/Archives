using System.Threading.Tasks;

namespace CsPlaywrightXun.Services.Notifications
{
    /// <summary>
    /// SMTP client interface for sending emails
    /// </summary>
    public interface ISmtpClient
    {
        /// <summary>
        /// Send an email message via SMTP
        /// </summary>
        /// <param name="message">Email message to send</param>
        /// <returns>True if email was sent successfully, false otherwise</returns>
        Task<bool> SendEmailAsync(EmailMessage message);

        /// <summary>
        /// Test SMTP server connection
        /// </summary>
        /// <returns>True if connection is successful, false otherwise</returns>
        Task<bool> TestConnectionAsync();

        /// <summary>
        /// Configure SMTP client with connection settings
        /// </summary>
        /// <param name="config">SMTP configuration</param>
        void Configure(SmtpConfiguration config);
    }
}