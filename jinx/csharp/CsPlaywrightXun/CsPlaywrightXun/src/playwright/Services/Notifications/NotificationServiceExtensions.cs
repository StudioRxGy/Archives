using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CsPlaywrightXun.src.playwright.Core.Utilities;

namespace CsPlaywrightXun.Services.Notifications
{
    /// <summary>
    /// Extension methods for registering notification services
    /// </summary>
    public static class NotificationServiceExtensions
    {
        /// <summary>
        /// Add notification services to the dependency injection container
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddNotificationServices(this IServiceCollection services)
        {
            // Register core notification services
            services.AddSingleton<INotificationEventBus, NotificationEventBus>();
            
            // Register the integrated test execution manager
            services.AddScoped<NotificationIntegratedTestExecutionManager>();
            
            // Register the base test execution manager if not already registered
            services.AddScoped<TestExecutionManager>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<TestExecutionManager>>();
                var strategyLogger = provider.GetRequiredService<ILogger<TestExecutionStrategy>>();
                var strategy = TestExecutionStrategy.CreateDefault(strategyLogger);
                return new TestExecutionManager(strategy, logger);
            });

            return services;
        }

        /// <summary>
        /// Add notification services with custom configuration
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configureOptions">Configuration action</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddNotificationServices(
            this IServiceCollection services,
            Action<NotificationServiceOptions> configureOptions)
        {
            var options = new NotificationServiceOptions();
            configureOptions(options);

            services.AddSingleton(options);
            return services.AddNotificationServices();
        }
    }

    /// <summary>
    /// Configuration options for notification services
    /// </summary>
    public class NotificationServiceOptions
    {
        /// <summary>
        /// Enable automatic event publishing for test execution
        /// </summary>
        public bool EnableAutoEventPublishing { get; set; } = true;

        /// <summary>
        /// Maximum number of concurrent event handlers
        /// </summary>
        public int MaxConcurrentHandlers { get; set; } = 10;

        /// <summary>
        /// Timeout for event handler execution
        /// </summary>
        public TimeSpan HandlerTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Enable detailed logging for event bus operations
        /// </summary>
        public bool EnableDetailedLogging { get; set; } = false;
    }
}