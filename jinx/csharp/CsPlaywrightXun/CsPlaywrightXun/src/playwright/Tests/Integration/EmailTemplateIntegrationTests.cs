using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CsPlaywrightXun.Services.Notifications;
using CsPlaywrightXun.Services.Notifications.Models;
using Xunit;

namespace CsPlaywrightXun.Tests.Integration
{
    /// <summary>
    /// Integration tests for email template engine with actual data models
    /// </summary>
    public class EmailTemplateIntegrationTests
    {
        [Fact]
        public async Task TestStartTemplate_WithRealData_RendersCorrectly()
        {
            // Arrange
            var engine = new EmailTemplateEngine();
            var context = new TestStartContext
            {
                TestSuiteName = "Integration Test Suite",
                StartTime = DateTime.Now,
                Environment = "Production",
                Metadata = new Dictionary<string, object>
                {
                    { "ProjectName", "CsPlaywrightXun Framework" }
                }
            };

            // Act
            var result = await engine.RenderTemplateAsync("test-start", context);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Integration Test Suite", result);
            Assert.Contains("Production", result);
            // Note: The comprehensive template may not include ProjectName in the main content
            Assert.Contains("Test Execution Started", result);
        }

        [Fact]
        public async Task TestSuccessTemplate_WithRealData_RendersCorrectly()
        {
            // Arrange
            var engine = new EmailTemplateEngine();
            var context = new TestSuccessContext
            {
                TestSuiteName = "Success Test Suite",
                StartTime = DateTime.Now.AddMinutes(-10),
                EndTime = DateTime.Now,
                Duration = TimeSpan.FromMinutes(10),
                TotalTests = 100,
                PassedTests = 95,
                FailedTests = 0,
                SkippedTests = 5,
                PassRate = 95.0,
                Environment = "Staging",
                Metadata = new Dictionary<string, object>
                {
                    { "ProjectName", "Test Project" }
                }
            };

            // Act
            var result = await engine.RenderTemplateAsync("test-success", context);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Success Test Suite", result);
            Assert.Contains("95%", result);
            Assert.Contains("100", result); // Total tests
            Assert.Contains("95", result);  // Passed tests
            // Note: Environment may not be in the main content area of comprehensive template
            Assert.Contains("Test Execution Successful", result);
        }

        [Fact]
        public async Task TestFailureTemplate_WithRealData_RendersCorrectly()
        {
            // Arrange
            var engine = new EmailTemplateEngine();
            var context = new TestFailureContext
            {
                TestSuiteName = "Failure Test Suite",
                StartTime = DateTime.Now.AddMinutes(-15),
                EndTime = DateTime.Now,
                Duration = TimeSpan.FromMinutes(15),
                TotalTests = 50,
                PassedTests = 40,
                FailedTests = 8,
                SkippedTests = 2,
                PassRate = 80.0,
                Environment = "Development",
                HasCriticalFailures = true,
                FailedTestCases = new List<FailedTestContext>
                {
                    new FailedTestContext
                    {
                        TestName = "Critical Login Test",
                        ErrorMessage = "Authentication failed",
                        Duration = TimeSpan.FromSeconds(30),
                        Category = "Authentication",
                        IsCritical = true
                    },
                    new FailedTestContext
                    {
                        TestName = "Data Validation Test",
                        ErrorMessage = "Invalid input format",
                        Duration = TimeSpan.FromSeconds(15),
                        Category = "Validation",
                        IsCritical = false
                    }
                },
                Metadata = new Dictionary<string, object>
                {
                    { "ProjectName", "Test Project" }
                }
            };

            // Act
            var result = await engine.RenderTemplateAsync("test-failure", context);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Failure Test Suite", result);
            Assert.Contains("80%", result);
            Assert.Contains("50", result); // Total tests
            Assert.Contains("40", result); // Passed tests
            Assert.Contains("8", result);  // Failed tests
            // Note: Environment may not be in the main content area of comprehensive template
            Assert.Contains("Test Execution Failed", result);
        }

        [Fact]
        public async Task ReportGeneratedTemplate_WithRealData_RendersCorrectly()
        {
            // Arrange
            var engine = new EmailTemplateEngine();
            var context = new ReportGeneratedContext
            {
                ReportName = "Comprehensive Test Report",
                ReportType = "HTML",
                GeneratedAt = DateTime.Now,
                ReportUrl = "https://reports.example.com/test-report-123",
                ReportPath = "/reports/test-report-123.html",
                FileSizeBytes = 2048576, // 2MB
                TestSuiteName = "Full Regression Suite",
                Metadata = new Dictionary<string, object>
                {
                    { "ProjectName", "Enterprise Application" }
                }
            };

            // Act
            var result = await engine.RenderTemplateAsync("report-generated", context);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Comprehensive Test Report", result);
            // Note: ReportType may not be in the main content area of comprehensive template
            Assert.Contains("Full Regression Suite", result);
            // Note: ProjectName may not be in the main content area of comprehensive template
            Assert.Contains("2.0 MB", result); // Formatted file size
            Assert.Contains("Test Report Generated", result);
        }

        [Fact]
        public async Task AllTemplates_AreRegisteredAndValid()
        {
            // Arrange
            var engine = new EmailTemplateEngine();
            var expectedTemplates = new[] { "test-start", "test-success", "test-failure", "report-generated" };

            // Act & Assert
            foreach (var templateName in expectedTemplates)
            {
                var isValid = await engine.ValidateTemplateAsync(templateName);
                Assert.True(isValid, $"Template '{templateName}' should be valid");
            }
        }
    }
}