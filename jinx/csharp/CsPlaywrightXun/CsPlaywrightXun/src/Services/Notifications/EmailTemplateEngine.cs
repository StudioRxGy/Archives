using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CsPlaywrightXun.Services.Notifications
{
    /// <summary>
    /// Email template engine implementation for rendering HTML email content
    /// </summary>
    public class EmailTemplateEngine : IEmailTemplateEngine
    {
        private readonly ConcurrentDictionary<string, string> _templates;
        private readonly Regex _placeholderRegex;

        /// <summary>
        /// Initializes a new instance of the EmailTemplateEngine
        /// </summary>
        public EmailTemplateEngine()
        {
            _templates = new ConcurrentDictionary<string, string>();
            _placeholderRegex = new Regex(@"\{\{(\w+(?:\.\w+)*)\}\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            
            // Load default templates
            LoadDefaultTemplates();
        }

        /// <summary>
        /// Render an email template with the provided data model
        /// </summary>
        /// <param name="templateName">Name of the template to render</param>
        /// <param name="model">Data model for template binding</param>
        /// <returns>Rendered HTML content</returns>
        public async Task<string> RenderTemplateAsync(string templateName, object model)
        {
            if (string.IsNullOrWhiteSpace(templateName))
                throw new ArgumentException("Template name cannot be null or empty", nameof(templateName));

            if (model == null)
                throw new ArgumentNullException(nameof(model));

            if (!_templates.TryGetValue(templateName, out var template))
                throw new InvalidOperationException($"Template '{templateName}' not found");

            try
            {
                var renderedContent = await Task.Run(() => RenderTemplate(template, model));
                return renderedContent;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to render template '{templateName}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validate that a template exists and is syntactically correct
        /// </summary>
        /// <param name="templateName">Name of the template to validate</param>
        /// <returns>True if template is valid, false otherwise</returns>
        public async Task<bool> ValidateTemplateAsync(string templateName)
        {
            if (string.IsNullOrWhiteSpace(templateName))
                return false;

            if (!_templates.TryGetValue(templateName, out var template))
                return false;

            try
            {
                // Validate template syntax by checking for balanced braces
                await Task.Run(() => ValidateTemplateSyntax(template));
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Register a new template with the engine
        /// </summary>
        /// <param name="name">Template name identifier</param>
        /// <param name="templateContent">Template content (HTML with placeholders)</param>
        public void RegisterTemplate(string name, string templateContent)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Template name cannot be null or empty", nameof(name));

            if (string.IsNullOrWhiteSpace(templateContent))
                throw new ArgumentException("Template content cannot be null or empty", nameof(templateContent));

            try
            {
                // Validate template syntax before registering
                ValidateTemplateSyntax(templateContent);
                _templates.AddOrUpdate(name, templateContent, (key, oldValue) => templateContent);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to register template '{name}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets all registered template names
        /// </summary>
        /// <returns>Collection of template names</returns>
        public IEnumerable<string> GetRegisteredTemplates()
        {
            return _templates.Keys;
        }

        /// <summary>
        /// Removes a template from the engine
        /// </summary>
        /// <param name="templateName">Name of the template to remove</param>
        /// <returns>True if template was removed, false if it didn't exist</returns>
        public bool RemoveTemplate(string templateName)
        {
            if (string.IsNullOrWhiteSpace(templateName))
                return false;

            return _templates.TryRemove(templateName, out _);
        }

        /// <summary>
        /// Renders a template with the provided model
        /// </summary>
        private string RenderTemplate(string template, object model)
        {
            if (string.IsNullOrEmpty(template))
                return string.Empty;

            var result = _placeholderRegex.Replace(template, match =>
            {
                var propertyPath = match.Groups[1].Value;
                var value = GetPropertyValue(model, propertyPath);
                return value?.ToString() ?? string.Empty;
            });

            return result;
        }

        /// <summary>
        /// Gets a property value from an object using dot notation
        /// </summary>
        private object? GetPropertyValue(object obj, string propertyPath)
        {
            if (obj == null || string.IsNullOrEmpty(propertyPath))
                return null;

            var properties = propertyPath.Split('.');
            var currentObject = obj;

            foreach (var property in properties)
            {
                if (currentObject == null)
                    return null;

                var type = currentObject.GetType();
                var propertyInfo = type.GetProperty(property, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                if (propertyInfo == null)
                {
                    // Try to get field if property doesn't exist
                    var fieldInfo = type.GetField(property, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    if (fieldInfo == null)
                        return null;

                    currentObject = fieldInfo.GetValue(currentObject);
                }
                else
                {
                    currentObject = propertyInfo.GetValue(currentObject);
                }
            }

            return currentObject;
        }

        /// <summary>
        /// Validates template syntax for balanced braces and valid placeholders
        /// </summary>
        private void ValidateTemplateSyntax(string template)
        {
            if (string.IsNullOrEmpty(template))
                return;

            var braceCount = 0;
            var inPlaceholder = false;
            var placeholderStart = -1;

            for (int i = 0; i < template.Length; i++)
            {
                var current = template[i];
                var next = i + 1 < template.Length ? template[i + 1] : '\0';

                if (current == '{' && next == '{')
                {
                    if (inPlaceholder)
                        throw new InvalidOperationException($"Nested placeholders are not allowed at position {i}");

                    inPlaceholder = true;
                    placeholderStart = i;
                    braceCount += 2;
                    i++; // Skip next brace
                }
                else if (current == '}' && next == '}')
                {
                    if (!inPlaceholder)
                        throw new InvalidOperationException($"Unexpected closing braces at position {i}");

                    // Validate placeholder content
                    var placeholderContent = template.Substring(placeholderStart + 2, i - placeholderStart - 2);
                    if (string.IsNullOrWhiteSpace(placeholderContent))
                        throw new InvalidOperationException($"Empty placeholder at position {placeholderStart}");

                    if (!IsValidPropertyPath(placeholderContent))
                        throw new InvalidOperationException($"Invalid property path '{placeholderContent}' at position {placeholderStart}");

                    inPlaceholder = false;
                    braceCount -= 2;
                    i++; // Skip next brace
                }
            }

            if (braceCount != 0)
                throw new InvalidOperationException("Unbalanced braces in template");

            if (inPlaceholder)
                throw new InvalidOperationException("Unclosed placeholder in template");
        }

        /// <summary>
        /// Validates if a property path is valid (contains only letters, numbers, dots, and underscores)
        /// </summary>
        private bool IsValidPropertyPath(string propertyPath)
        {
            if (string.IsNullOrWhiteSpace(propertyPath))
                return false;

            // Property path should contain only letters, numbers, dots, and underscores
            // Should not start or end with a dot
            // Should not have consecutive dots
            var regex = new Regex(@"^[a-zA-Z_][a-zA-Z0-9_]*(\.[a-zA-Z_][a-zA-Z0-9_]*)*$");
            return regex.IsMatch(propertyPath);
        }

        /// <summary>
        /// Loads default email templates from embedded resources or files
        /// </summary>
        private void LoadDefaultTemplates()
        {
            try
            {
                // Try to load templates from files first, then fall back to embedded templates
                LoadTemplateFromFileOrEmbedded("test-start", "TestStartTemplate.html", GetDefaultTestStartTemplate());
                LoadTemplateFromFileOrEmbedded("test-success", "TestSuccessTemplate.html", GetDefaultTestSuccessTemplate());
                LoadTemplateFromFileOrEmbedded("test-failure", "TestFailureTemplate.html", GetDefaultTestFailureTemplate());
                LoadTemplateFromFileOrEmbedded("report-generated", "ReportGeneratedTemplate.html", GetDefaultReportGeneratedTemplate());
            }
            catch (Exception)
            {
                // If loading fails, use simple fallback templates
                RegisterTemplate("test-start", GetFallbackTestStartTemplate());
                RegisterTemplate("test-success", GetFallbackTestSuccessTemplate());
                RegisterTemplate("test-failure", GetFallbackTestFailureTemplate());
                RegisterTemplate("report-generated", GetFallbackReportGeneratedTemplate());
            }
        }

        /// <summary>
        /// Loads a template from file or uses embedded fallback
        /// </summary>
        private void LoadTemplateFromFileOrEmbedded(string templateName, string fileName, string fallbackTemplate)
        {
            try
            {
                // Get the directory where the assembly is located
                var assemblyLocation = Assembly.GetExecutingAssembly().Location;
                var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
                
                if (!string.IsNullOrEmpty(assemblyDirectory))
                {
                    var templatesPath = Path.Combine(assemblyDirectory, "Services", "Notifications", "Templates", fileName);

                    // Try to read from file system first
                    if (File.Exists(templatesPath))
                    {
                        var templateContent = File.ReadAllText(templatesPath, Encoding.UTF8);
                        RegisterTemplate(templateName, templateContent);
                        return;
                    }
                }

                // Try relative path from current directory
                var relativePath = Path.Combine("CsPlaywrightXun", "src", "playwright", "Services", "Notifications", "Templates", fileName);
                if (File.Exists(relativePath))
                {
                    var templateContent = File.ReadAllText(relativePath, Encoding.UTF8);
                    RegisterTemplate(templateName, templateContent);
                    return;
                }

                // Fall back to embedded template
                RegisterTemplate(templateName, fallbackTemplate);
            }
            catch
            {
                // Use fallback template if file loading fails
                RegisterTemplate(templateName, fallbackTemplate);
            }
        }

        /// <summary>
        /// Gets the default test start template (comprehensive HTML)
        /// </summary>
        private string GetDefaultTestStartTemplate()
        {
            return @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Test Execution Started - {{TestSuiteName}}</title>
    <style>
        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f4f4f4; }
        .container { background-color: #ffffff; padding: 30px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
        .header { background-color: #007acc; color: white; padding: 20px; border-radius: 8px 8px 0 0; margin: -30px -30px 20px -30px; text-align: center; }
        .header h1 { margin: 0; font-size: 24px; }
        .info-section { background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 15px 0; }
        .info-row { display: flex; justify-content: space-between; margin: 8px 0; padding: 5px 0; border-bottom: 1px solid #e9ecef; }
        .info-row:last-child { border-bottom: none; }
        .label { font-weight: bold; color: #495057; }
        .value { color: #007acc; font-weight: 500; }
        .status-badge { display: inline-block; padding: 4px 12px; background-color: #28a745; color: white; border-radius: 20px; font-size: 12px; font-weight: bold; text-transform: uppercase; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🚀 Test Execution Started</h1>
            <div class=""status-badge"">In Progress</div>
        </div>
        <div class=""content"">
            <p>Test suite execution has been initiated and is now running.</p>
            <div class=""info-section"">
                <h3>Execution Details</h3>
                <div class=""info-row""><span class=""label"">Test Suite:</span><span class=""value"">{{TestSuiteName}}</span></div>
                <div class=""info-row""><span class=""label"">Start Time:</span><span class=""value"">{{StartTime}}</span></div>
                <div class=""info-row""><span class=""label"">Environment:</span><span class=""value"">{{Environment}}</span></div>
            </div>
        </div>
    </div>
</body>
</html>";
        }

        /// <summary>
        /// Gets the default test success template (comprehensive HTML)
        /// </summary>
        private string GetDefaultTestSuccessTemplate()
        {
            return @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Test Execution Successful - {{TestSuiteName}}</title>
    <style>
        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f4f4f4; }
        .container { background-color: #ffffff; padding: 30px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
        .header { background-color: #28a745; color: white; padding: 20px; border-radius: 8px 8px 0 0; margin: -30px -30px 20px -30px; text-align: center; }
        .header h1 { margin: 0; font-size: 24px; }
        .pass-rate { font-size: 36px; font-weight: bold; color: #28a745; text-align: center; margin: 20px 0; }
        .stats-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 15px; margin-top: 15px; }
        .stat-item { text-align: center; padding: 10px; background-color: white; border-radius: 5px; border: 1px solid #c3e6cb; }
        .stat-number { font-size: 24px; font-weight: bold; color: #155724; }
        .stat-label { font-size: 12px; color: #495057; text-transform: uppercase; margin-top: 5px; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>✅ Test Execution Successful</h1>
        </div>
        <div class=""content"">
            <p>Test suite <strong>{{TestSuiteName}}</strong> completed successfully!</p>
            <div class=""pass-rate"">{{PassRate}}% Pass Rate</div>
            <div class=""stats-grid"">
                <div class=""stat-item""><div class=""stat-number"">{{TotalTests}}</div><div class=""stat-label"">Total Tests</div></div>
                <div class=""stat-item""><div class=""stat-number"">{{PassedTests}}</div><div class=""stat-label"">Passed</div></div>
                <div class=""stat-item""><div class=""stat-number"">{{FailedTests}}</div><div class=""stat-label"">Failed</div></div>
                <div class=""stat-item""><div class=""stat-number"">{{SkippedTests}}</div><div class=""stat-label"">Skipped</div></div>
            </div>
            <p><strong>Duration:</strong> {{Duration}}</p>
        </div>
    </div>
</body>
</html>";
        }

        /// <summary>
        /// Gets the default test failure template (comprehensive HTML)
        /// </summary>
        private string GetDefaultTestFailureTemplate()
        {
            return @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Test Execution Failed - {{TestSuiteName}}</title>
    <style>
        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f4f4f4; }
        .container { background-color: #ffffff; padding: 30px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
        .header { background-color: #dc3545; color: white; padding: 20px; border-radius: 8px 8px 0 0; margin: -30px -30px 20px -30px; text-align: center; }
        .header h1 { margin: 0; font-size: 24px; }
        .pass-rate { font-size: 36px; font-weight: bold; color: #dc3545; text-align: center; margin: 20px 0; }
        .stats-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 15px; margin-top: 15px; }
        .stat-item { text-align: center; padding: 10px; background-color: white; border-radius: 5px; border: 1px solid #f5c6cb; }
        .stat-number { font-size: 24px; font-weight: bold; color: #721c24; }
        .stat-number.failed { color: #dc3545; }
        .stat-number.passed { color: #28a745; }
        .stat-label { font-size: 12px; color: #495057; text-transform: uppercase; margin-top: 5px; }
        .failed-test-item { background-color: white; border: 1px solid #f5c6cb; border-radius: 5px; padding: 15px; margin: 10px 0; }
        .test-name { font-weight: bold; color: #721c24; margin-bottom: 8px; }
        .error-message { color: #dc3545; font-family: 'Courier New', monospace; font-size: 12px; background-color: #f8f9fa; padding: 8px; border-radius: 3px; margin: 5px 0; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>❌ Test Execution Failed</h1>
        </div>
        <div class=""content"">
            <p>Test suite <strong>{{TestSuiteName}}</strong> has failures that require attention.</p>
            <div class=""pass-rate"">{{PassRate}}% Pass Rate</div>
            <div class=""stats-grid"">
                <div class=""stat-item""><div class=""stat-number"">{{TotalTests}}</div><div class=""stat-label"">Total Tests</div></div>
                <div class=""stat-item""><div class=""stat-number passed"">{{PassedTests}}</div><div class=""stat-label"">Passed</div></div>
                <div class=""stat-item""><div class=""stat-number failed"">{{FailedTests}}</div><div class=""stat-label"">Failed</div></div>
                <div class=""stat-item""><div class=""stat-number"">{{SkippedTests}}</div><div class=""stat-label"">Skipped</div></div>
            </div>
            <p><strong>Duration:</strong> {{Duration}}</p>
        </div>
    </div>
</body>
</html>";
        }

        /// <summary>
        /// Gets the default report generated template (comprehensive HTML)
        /// </summary>
        private string GetDefaultReportGeneratedTemplate()
        {
            return @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Test Report Generated - {{ReportName}}</title>
    <style>
        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f4f4f4; }
        .container { background-color: #ffffff; padding: 30px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
        .header { background-color: #6f42c1; color: white; padding: 20px; border-radius: 8px 8px 0 0; margin: -30px -30px 20px -30px; text-align: center; }
        .header h1 { margin: 0; font-size: 24px; }
        .info-section { background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 15px 0; }
        .info-row { display: flex; justify-content: space-between; margin: 8px 0; padding: 5px 0; border-bottom: 1px solid #e9ecef; }
        .info-row:last-child { border-bottom: none; }
        .label { font-weight: bold; color: #495057; }
        .value { color: #6f42c1; font-weight: 500; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>📊 Test Report Generated</h1>
        </div>
        <div class=""content"">
            <p>A new test report has been generated and is ready for review.</p>
            <div class=""info-section"">
                <h3>Report Information</h3>
                <div class=""info-row""><span class=""label"">Report Name:</span><span class=""value"">{{ReportName}}</span></div>
                <div class=""info-row""><span class=""label"">Test Suite:</span><span class=""value"">{{TestSuiteName}}</span></div>
                <div class=""info-row""><span class=""label"">Generated At:</span><span class=""value"">{{GeneratedAt}}</span></div>
                <div class=""info-row""><span class=""label"">File Size:</span><span class=""value"">{{FileSizeFormatted}}</span></div>
            </div>
        </div>
    </div>
</body>
</html>";
        }

        /// <summary>
        /// Gets a simple fallback test start template
        /// </summary>
        private string GetFallbackTestStartTemplate()
        {
            return @"<html><body><h2>Test Execution Started</h2><p>Test suite <strong>{{TestSuiteName}}</strong> has started execution.</p><p><strong>Start Time:</strong> {{StartTime}}</p><p><strong>Environment:</strong> {{Environment}}</p><p><strong>Project:</strong> {{Metadata.ProjectName}}</p></body></html>";
        }

        /// <summary>
        /// Gets a simple fallback test success template
        /// </summary>
        private string GetFallbackTestSuccessTemplate()
        {
            return @"<html><body><h2>Test Execution Successful</h2><p>Test suite <strong>{{TestSuiteName}}</strong> completed successfully.</p><p><strong>Pass Rate:</strong> {{PassRate}}%</p><p><strong>Total Tests:</strong> {{TotalTests}}</p><p><strong>Passed:</strong> {{PassedTests}}</p><p><strong>Failed:</strong> {{FailedTests}}</p><p><strong>Skipped:</strong> {{SkippedTests}}</p><p><strong>Duration:</strong> {{Duration}}</p><p><strong>Environment:</strong> {{Environment}}</p></body></html>";
        }

        /// <summary>
        /// Gets a simple fallback test failure template
        /// </summary>
        private string GetFallbackTestFailureTemplate()
        {
            return @"<html><body><h2>Test Execution Failed</h2><p>Test suite <strong>{{TestSuiteName}}</strong> has failures.</p><p><strong>Failed Tests:</strong> {{FailedTests}}</p><p><strong>Total Tests:</strong> {{TotalTests}}</p><p><strong>Pass Rate:</strong> {{PassRate}}%</p><p><strong>Duration:</strong> {{Duration}}</p><p><strong>Environment:</strong> {{Environment}}</p></body></html>";
        }

        /// <summary>
        /// Gets a simple fallback report generated template
        /// </summary>
        private string GetFallbackReportGeneratedTemplate()
        {
            return @"<html><body><h2>Test Report Generated</h2><p>Report <strong>{{ReportName}}</strong> has been generated.</p><p><strong>Type:</strong> {{ReportType}}</p><p><strong>Test Suite:</strong> {{TestSuiteName}}</p><p><strong>Generated At:</strong> {{GeneratedAt}}</p><p><strong>File Size:</strong> {{FileSizeFormatted}}</p></body></html>";
        }
    }
}