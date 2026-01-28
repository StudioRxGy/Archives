using System;
using System.Threading.Tasks;
using CsPlaywrightXun.Services.Notifications;
using Xunit;

namespace CsPlaywrightXun.Tests.Unit.Notifications
{
    /// <summary>
    /// Unit tests for EmailTemplateEngine
    /// </summary>
    public class EmailTemplateEngineTests
    {
        [Fact]
        public async Task RenderTemplateAsync_WithValidTemplate_ReturnsRenderedContent()
        {
            // Arrange
            var engine = new EmailTemplateEngine();
            var testModel = new { TestSuiteName = "Sample Test Suite", StartTime = DateTime.Now, Environment = "Development" };

            // Act
            var result = await engine.RenderTemplateAsync("test-start", testModel);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Sample Test Suite", result);
            Assert.Contains("Development", result);
        }

        [Fact]
        public async Task ValidateTemplateAsync_WithValidTemplate_ReturnsTrue()
        {
            // Arrange
            var engine = new EmailTemplateEngine();

            // Act
            var result = await engine.ValidateTemplateAsync("test-start");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ValidateTemplateAsync_WithInvalidTemplate_ReturnsFalse()
        {
            // Arrange
            var engine = new EmailTemplateEngine();

            // Act
            var result = await engine.ValidateTemplateAsync("non-existent-template");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void RegisterTemplate_WithValidTemplate_RegistersSuccessfully()
        {
            // Arrange
            var engine = new EmailTemplateEngine();
            var templateContent = "<html><body>Hello {{Name}}</body></html>";

            // Act & Assert - Should not throw
            engine.RegisterTemplate("custom-template", templateContent);
        }

        [Fact]
        public async Task RenderTemplateAsync_WithCustomTemplate_ReturnsRenderedContent()
        {
            // Arrange
            var engine = new EmailTemplateEngine();
            var templateContent = "<html><body>Hello {{Name}}, your score is {{Score}}</body></html>";
            var testModel = new { Name = "John", Score = 95 };

            engine.RegisterTemplate("custom-template", templateContent);

            // Act
            var result = await engine.RenderTemplateAsync("custom-template", testModel);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Hello John", result);
            Assert.Contains("your score is 95", result);
        }

        [Fact]
        public async Task RenderTemplateAsync_WithNestedProperties_ReturnsRenderedContent()
        {
            // Arrange
            var engine = new EmailTemplateEngine();
            var templateContent = "<html><body>Project: {{Metadata.ProjectName}}</body></html>";
            var testModel = new { Metadata = new { ProjectName = "Test Project" } };

            engine.RegisterTemplate("nested-template", templateContent);

            // Act
            var result = await engine.RenderTemplateAsync("nested-template", testModel);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Project: Test Project", result);
        }

        [Fact]
        public void RegisterTemplate_WithInvalidSyntax_ThrowsException()
        {
            // Arrange
            var engine = new EmailTemplateEngine();
            var invalidTemplate = "<html><body>Hello {{Name</body></html>"; // Missing closing brace

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => 
                engine.RegisterTemplate("invalid-template", invalidTemplate));
        }

        [Fact]
        public async Task RenderTemplateAsync_WithNullModel_ThrowsArgumentNullException()
        {
            // Arrange
            var engine = new EmailTemplateEngine();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                engine.RenderTemplateAsync("test-start", null));
        }

        [Fact]
        public async Task RenderTemplateAsync_WithEmptyTemplateName_ThrowsArgumentException()
        {
            // Arrange
            var engine = new EmailTemplateEngine();
            var testModel = new { Name = "Test" };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                engine.RenderTemplateAsync("", testModel));
        }
    }
}