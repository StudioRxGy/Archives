using System.Threading.Tasks;

namespace CsPlaywrightXun.Services.Notifications
{
    /// <summary>
    /// Email template engine interface for rendering HTML email content
    /// </summary>
    public interface IEmailTemplateEngine
    {
        /// <summary>
        /// Render an email template with the provided data model
        /// </summary>
        /// <param name="templateName">Name of the template to render</param>
        /// <param name="model">Data model for template binding</param>
        /// <returns>Rendered HTML content</returns>
        Task<string> RenderTemplateAsync(string templateName, object model);

        /// <summary>
        /// Validate that a template exists and is syntactically correct
        /// </summary>
        /// <param name="templateName">Name of the template to validate</param>
        /// <returns>True if template is valid, false otherwise</returns>
        Task<bool> ValidateTemplateAsync(string templateName);

        /// <summary>
        /// Register a new template with the engine
        /// </summary>
        /// <param name="name">Template name identifier</param>
        /// <param name="templateContent">Template content (HTML with placeholders)</param>
        void RegisterTemplate(string name, string templateContent);
    }
}