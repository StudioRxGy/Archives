using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CsPlaywrightXun.Services.Notifications.Examples
{
    /// <summary>
    /// Example demonstrating how to use the notification event bus
    /// </summary>
    public class EventBusUsageExample
    {
        /// <summary>
        /// Example of setting up and using the notification event bus
        /// </summary>
        public static async Task RunExampleAsync()
        {
            // Setup dependency injection
            var services = new ServiceCollection();
            
            // Add logging
            services.AddLogging(builder => builder.AddConsole());
            
            // Add notification services (this includes the event bus)
            services.AddNotificationServices(options =>
            {
                options.EnableAutoEventPublishing = true;
                options.EnableDetailedLogging = true;
                options.MaxConcurrentHandlers = 5;
            });

            // Build service provider
            var serviceProvider = services.BuildServiceProvider();

            // Get the event bus
            var eventBus = serviceProvider.GetRequiredService<INotificationEventBus>();
            var logger = serviceProvider.GetRequiredService<ILogger<EventBusUsageExample>>();

            logger.LogInformation("Starting event bus usage example");

            // Subscribe to custom events (in addition to the default notification handlers)
            eventBus.Subscribe<TestStartedEvent>(async (evt, token) =>
            {
                logger.LogInformation("Custom handler: Test started - {TestSuiteName} at {StartTime}", 
                    evt.TestSuiteName, evt.StartTime);
                await Task.Delay(100, token); // Simulate some work
            });

            eventBus.Subscribe<TestCompletedEvent>(async (evt, token) =>
            {
                logger.LogInformation("Custom handler: Test completed - {TestSuiteName}, Success: {IsSuccess}", 
                    evt.Result.TestSuiteName, evt.IsSuccess);
                await Task.Delay(100, token); // Simulate some work
            });

            // Publish test events manually (normally these would be published by the test execution manager)
            await PublishSampleEventsAsync(eventBus, logger);

            // Example of using the integrated test execution manager
            await RunIntegratedTestExecutionExampleAsync(serviceProvider, logger);

            logger.LogInformation("Event bus usage example completed");
        }

        /// <summary>
        /// Publish sample events to demonstrate event bus functionality
        /// </summary>
        private static async Task PublishSampleEventsAsync(INotificationEventBus eventBus, ILogger logger)
        {
            logger.LogInformation("Publishing sample events...");

            // Publish test started event
            var testStartedEvent = new TestStartedEvent
            {
                TestSuiteName = "Sample Test Suite",
                StartTime = DateTime.UtcNow,
                Environment = "Development",
                Metadata = new System.Collections.Generic.Dictionary<string, object>
                {
                    ["ProjectPath"] = "/sample/project",
                    ["ExecutionId"] = Guid.NewGuid().ToString()
                }
            };

            await eventBus.PublishAsync(testStartedEvent);

            // Simulate test execution delay
            await Task.Delay(1000);

            // Publish test completed event
            var testCompletedEvent = new TestCompletedEvent
            {
                Result = new TestSuiteResult
                {
                    TestSuiteName = "Sample Test Suite",
                    StartTime = testStartedEvent.StartTime,
                    EndTime = DateTime.UtcNow,
                    TotalTests = 10,
                    PassedTests = 8,
                    FailedTests = 2,
                    SkippedTests = 0,
                    Environment = "Development"
                },
                IsSuccess = false, // Has failures
                CompletedAt = DateTime.UtcNow
            };

            await eventBus.PublishAsync(testCompletedEvent);

            // Publish report generated event
            var reportGeneratedEvent = new ReportGeneratedEvent
            {
                ReportInfo = new ReportInfo
                {
                    ReportName = "Sample Test Report",
                    ReportPath = "/reports/sample-report.html",
                    GeneratedAt = DateTime.UtcNow,
                    ReportType = "HTML"
                },
                GeneratedAt = DateTime.UtcNow
            };

            await eventBus.PublishAsync(reportGeneratedEvent);

            logger.LogInformation("Sample events published successfully");
        }

        /// <summary>
        /// Example of using the integrated test execution manager
        /// </summary>
        private static async Task RunIntegratedTestExecutionExampleAsync(IServiceProvider serviceProvider, ILogger logger)
        {
            logger.LogInformation("Running integrated test execution example...");

            try
            {
                // Get the integrated test execution manager
                var testManager = serviceProvider.GetRequiredService<NotificationIntegratedTestExecutionManager>();

                // This would normally execute real tests and publish events automatically
                // For this example, we'll just demonstrate the API
                logger.LogInformation("Integrated test execution manager is ready");
                logger.LogInformation("Supported strategies: {Strategies}", 
                    string.Join(", ", testManager.GetSupportedExecutionStrategies()));

                // In a real scenario, you would call:
                // var result = await testManager.ExecuteUITestsOnlyAsync();
                // This would automatically publish TestStartedEvent and TestCompletedEvent

                // Example of publishing a report generated event
                await testManager.PublishReportGeneratedEventAsync(
                    "Integration Test Report", 
                    "/reports/integration-report.html", 
                    "HTML");

                logger.LogInformation("Integrated test execution example completed");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in integrated test execution example");
            }
        }

        /// <summary>
        /// Example of custom event handling with error scenarios
        /// </summary>
        public static async Task RunErrorHandlingExampleAsync()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            services.AddNotificationServices();

            var serviceProvider = services.BuildServiceProvider();
            var eventBus = serviceProvider.GetRequiredService<INotificationEventBus>();
            var logger = serviceProvider.GetRequiredService<ILogger<EventBusUsageExample>>();

            logger.LogInformation("Starting error handling example");

            // Subscribe to events with error handling
            eventBus.Subscribe<TestStartedEvent>(async (evt, token) =>
            {
                logger.LogInformation("Handler 1: Processing test started event");
                await Task.Delay(100, token);
            });

            eventBus.Subscribe<TestStartedEvent>(async (evt, token) =>
            {
                logger.LogInformation("Handler 2: Processing test started event");
                // Simulate an error in this handler
                await Task.FromException(new InvalidOperationException("Simulated handler error"));
            });

            eventBus.Subscribe<TestStartedEvent>(async (evt, token) =>
            {
                logger.LogInformation("Handler 3: Processing test started event");
                await Task.Delay(100, token);
            });

            // Publish event - should continue processing even if one handler fails
            var testEvent = new TestStartedEvent
            {
                TestSuiteName = "Error Handling Test",
                StartTime = DateTime.UtcNow,
                Environment = "Development"
            };

            await eventBus.PublishAsync(testEvent);

            logger.LogInformation("Error handling example completed - all handlers except the failing one should have executed");
        }
    }
}