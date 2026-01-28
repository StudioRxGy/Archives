using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using CsPlaywrightXun.src.playwright.Core.Utilities;
using CsPlaywrightXun.Services.Notifications;
using TestExecutionResult = CsPlaywrightXun.src.playwright.Core.Utilities.TestExecutionResult;

namespace CsPlaywrightXun.Services.Notifications
{
    /// <summary>
    /// Test execution manager with integrated notification event publishing
    /// </summary>
    public class NotificationIntegratedTestExecutionManager
    {
        private readonly TestExecutionManager _baseManager;
        private readonly INotificationEventBus _eventBus;
        private readonly ILogger<NotificationIntegratedTestExecutionManager> _logger;

        public NotificationIntegratedTestExecutionManager(
            TestExecutionManager baseManager,
            INotificationEventBus eventBus,
            ILogger<NotificationIntegratedTestExecutionManager> logger)
        {
            _baseManager = baseManager ?? throw new ArgumentNullException(nameof(baseManager));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Execute UI tests with notification events
        /// </summary>
        /// <param name="projectPath">Project path</param>
        /// <returns>Execution result</returns>
        public async Task<CsPlaywrightXun.src.playwright.Core.Utilities.TestExecutionResult> ExecuteUITestsOnlyAsync(string? projectPath = null)
        {
            return await ExecuteTestsWithNotificationsAsync(
                () => _baseManager.ExecuteUITestsOnlyAsync(projectPath),
                "UI Tests Only",
                projectPath);
        }

        /// <summary>
        /// Execute API tests with notification events
        /// </summary>
        /// <param name="projectPath">Project path</param>
        /// <returns>Execution result</returns>
        public async Task<CsPlaywrightXun.src.playwright.Core.Utilities.TestExecutionResult> ExecuteAPITestsOnlyAsync(string? projectPath = null)
        {
            return await ExecuteTestsWithNotificationsAsync(
                () => _baseManager.ExecuteAPITestsOnlyAsync(projectPath),
                "API Tests Only",
                projectPath);
        }

        /// <summary>
        /// Execute mixed tests with notification events
        /// </summary>
        /// <param name="projectPath">Project path</param>
        /// <returns>Execution result</returns>
        public async Task<CsPlaywrightXun.src.playwright.Core.Utilities.TestExecutionResult> ExecuteMixedTestsAsync(string? projectPath = null)
        {
            return await ExecuteTestsWithNotificationsAsync(
                () => _baseManager.ExecuteMixedTestsAsync(projectPath),
                "Mixed Tests (UI + API)",
                projectPath);
        }

        /// <summary>
        /// Execute integration tests with notification events
        /// </summary>
        /// <param name="projectPath">Project path</param>
        /// <returns>Execution result</returns>
        public async Task<CsPlaywrightXun.src.playwright.Core.Utilities.TestExecutionResult> ExecuteIntegrationTestsAsync(string? projectPath = null)
        {
            return await ExecuteTestsWithNotificationsAsync(
                () => _baseManager.ExecuteIntegrationTestsAsync(projectPath),
                "Integration Tests",
                projectPath);
        }

        /// <summary>
        /// Execute fast tests with notification events
        /// </summary>
        /// <param name="projectPath">Project path</param>
        /// <returns>Execution result</returns>
        public async Task<CsPlaywrightXun.src.playwright.Core.Utilities.TestExecutionResult> ExecuteFastTestsAsync(string? projectPath = null)
        {
            return await ExecuteTestsWithNotificationsAsync(
                () => _baseManager.ExecuteFastTestsAsync(projectPath),
                "Fast Tests",
                projectPath);
        }

        /// <summary>
        /// Execute smoke tests with notification events
        /// </summary>
        /// <param name="projectPath">Project path</param>
        /// <returns>Execution result</returns>
        public async Task<CsPlaywrightXun.src.playwright.Core.Utilities.TestExecutionResult> ExecuteSmokeTestsAsync(string? projectPath = null)
        {
            return await ExecuteTestsWithNotificationsAsync(
                () => _baseManager.ExecuteSmokeTestsAsync(projectPath),
                "Smoke Tests",
                projectPath);
        }

        /// <summary>
        /// Execute regression tests with notification events
        /// </summary>
        /// <param name="projectPath">Project path</param>
        /// <returns>Execution result</returns>
        public async Task<CsPlaywrightXun.src.playwright.Core.Utilities.TestExecutionResult> ExecuteRegressionTestsAsync(string? projectPath = null)
        {
            return await ExecuteTestsWithNotificationsAsync(
                () => _baseManager.ExecuteRegressionTestsAsync(projectPath),
                "Regression Tests",
                projectPath);
        }

        /// <summary>
        /// Execute tests by strategy name with notification events
        /// </summary>
        /// <param name="strategyName">Strategy name</param>
        /// <param name="projectPath">Project path</param>
        /// <returns>Execution result</returns>
        public async Task<CsPlaywrightXun.src.playwright.Core.Utilities.TestExecutionResult> ExecuteTestsByStrategyNameAsync(string strategyName, string? projectPath = null)
        {
            return await ExecuteTestsWithNotificationsAsync(
                () => _baseManager.ExecuteTestsByStrategyNameAsync(strategyName, projectPath),
                strategyName,
                projectPath);
        }

        /// <summary>
        /// Execute tests with notification event publishing
        /// </summary>
        /// <param name="testExecution">Test execution function</param>
        /// <param name="testSuiteName">Test suite name</param>
        /// <param name="projectPath">Project path</param>
        /// <returns>Execution result</returns>
        private async Task<CsPlaywrightXun.src.playwright.Core.Utilities.TestExecutionResult> ExecuteTestsWithNotificationsAsync(
            Func<Task<CsPlaywrightXun.src.playwright.Core.Utilities.TestExecutionResult>> testExecution,
            string testSuiteName,
            string? projectPath)
        {
            var startTime = DateTime.UtcNow;
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            try
            {
                _logger.LogInformation("Starting test execution with notifications: {TestSuiteName}", testSuiteName);

                // Publish test started event
                var testStartedEvent = new TestStartedEvent
                {
                    TestSuiteName = testSuiteName,
                    StartTime = startTime,
                    Environment = environment,
                    Metadata = new System.Collections.Generic.Dictionary<string, object>
                    {
                        ["ProjectPath"] = projectPath ?? "Default",
                        ["ExecutionId"] = Guid.NewGuid().ToString(),
                        ["MachineName"] = Environment.MachineName,
                        ["UserName"] = Environment.UserName
                    }
                };

                await _eventBus.PublishAsync(testStartedEvent);

                // Execute the actual tests
                var result = await testExecution();

                // Convert TestExecutionResult to TestSuiteResult for notifications
                var testSuiteResult = ConvertToTestSuiteResult(result, testSuiteName, environment);

                // Publish test completed event
                var testCompletedEvent = new TestCompletedEvent
                {
                    Result = testSuiteResult,
                    IsSuccess = result.Success,
                    CompletedAt = result.EndTime
                };

                await _eventBus.PublishAsync(testCompletedEvent);

                _logger.LogInformation("Test execution completed with notifications: {TestSuiteName}, Success: {Success}", 
                    testSuiteName, result.Success);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Test execution failed with notifications: {TestSuiteName}", testSuiteName);

                // Create a failed result for notification
                var failedResult = new CsPlaywrightXun.src.playwright.Core.Utilities.TestExecutionResult
                {
                    StartTime = startTime,
                    EndTime = DateTime.UtcNow,
                    Success = false,
                    ErrorMessage = ex.Message,
                    Command = "N/A",
                    Filter = "N/A"
                };

                var failedTestSuiteResult = ConvertToTestSuiteResult(failedResult, testSuiteName, environment);

                // Publish failure event
                var testCompletedEvent = new TestCompletedEvent
                {
                    Result = failedTestSuiteResult,
                    IsSuccess = false,
                    CompletedAt = DateTime.UtcNow
                };

                await _eventBus.PublishAsync(testCompletedEvent);

                return failedResult;
            }
        }

        /// <summary>
        /// Convert TestExecutionResult to TestSuiteResult for notifications
        /// </summary>
        /// <param name="executionResult">Test execution result</param>
        /// <param name="testSuiteName">Test suite name</param>
        /// <param name="environment">Environment name</param>
        /// <returns>Test suite result</returns>
        private static TestSuiteResult ConvertToTestSuiteResult(CsPlaywrightXun.src.playwright.Core.Utilities.TestExecutionResult executionResult, string testSuiteName, string environment)
        {
            var testSuiteResult = new TestSuiteResult
            {
                TestSuiteName = testSuiteName,
                StartTime = executionResult.StartTime,
                EndTime = executionResult.EndTime,
                TotalTests = executionResult.TotalTests,
                PassedTests = executionResult.PassedTests,
                FailedTests = executionResult.FailedTests,
                SkippedTests = executionResult.SkippedTests,
                Environment = environment,
                Metadata = new System.Collections.Generic.Dictionary<string, object>
                {
                    ["Command"] = executionResult.Command ?? "N/A",
                    ["Filter"] = executionResult.Filter ?? "N/A",
                    ["Success"] = executionResult.Success,
                    ["ErrorMessage"] = executionResult.ErrorMessage ?? string.Empty
                }
            };

            // Convert test case results
            foreach (var testCase in executionResult.TestResults)
            {
                if (!testCase.Passed && !testCase.Skipped)
                {
                    // Determine if test is critical based on categories or tags
                    var isCritical = testCase.Categories.Any(c => c.ToLowerInvariant().Contains("critical")) ||
                                   testCase.Tags.Any(t => t.ToLowerInvariant().Contains("critical"));

                    testSuiteResult.FailedTestCases.Add(new TestCaseResult
                    {
                        TestName = testCase.TestName,
                        ErrorMessage = testCase.ErrorMessage ?? "Test failed",
                        StackTrace = testCase.StackTrace ?? string.Empty,
                        Duration = testCase.Duration,
                        Category = testCase.Categories.FirstOrDefault() ?? "General",
                        IsCritical = isCritical
                    });
                }
            }

            return testSuiteResult;
        }

        /// <summary>
        /// Publish report generated event
        /// </summary>
        /// <param name="reportName">Report name</param>
        /// <param name="reportPath">Report path</param>
        /// <param name="reportType">Report type</param>
        /// <returns>Task</returns>
        public async Task PublishReportGeneratedEventAsync(string reportName, string reportPath, string reportType = "HTML")
        {
            try
            {
                var reportGeneratedEvent = new ReportGeneratedEvent
                {
                    ReportInfo = new ReportInfo
                    {
                        ReportName = reportName,
                        ReportPath = reportPath,
                        GeneratedAt = DateTime.UtcNow,
                        ReportType = reportType,
                        Metadata = new System.Collections.Generic.Dictionary<string, object>
                        {
                            ["MachineName"] = Environment.MachineName,
                            ["UserName"] = Environment.UserName
                        }
                    },
                    GeneratedAt = DateTime.UtcNow
                };

                await _eventBus.PublishAsync(reportGeneratedEvent);
                _logger.LogInformation("Published report generated event: {ReportName}", reportName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish report generated event: {ReportName}", reportName);
            }
        }

        /// <summary>
        /// Get supported execution strategies
        /// </summary>
        /// <returns>List of supported strategies</returns>
        public System.Collections.Generic.List<string> GetSupportedExecutionStrategies()
        {
            return _baseManager.GetSupportedExecutionStrategies();
        }
    }
}