using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CsPlaywrightXun.Services.Notifications;
using CsPlaywrightXun.Core.Configuration;

namespace CsPlaywrightXun.src.playwright.Core.Configuration;

/// <summary>
/// Extension methods for configuring services in the dependency injection container
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add all framework services to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration instance</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddFrameworkServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add logging services
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        });

        // Add notification services
        services.AddNotificationServices(configuration);
        services.ValidateNotificationConfiguration();

        // Register notification event bus
        services.AddSingleton<INotificationEventBus, NotificationEventBus>();

        // Register test execution manager with notification support
        services.AddScoped<Utilities.TestExecutionManager>();

        return services;
    }

    /// <summary>
    /// Add notification services with custom configuration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configureOptions">Action to configure notification settings</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddNotificationServices(
        this IServiceCollection services,
        Action<NotificationSettings> configureOptions)
    {
        // Add logging services if not already registered
        services.AddLogging();

        // Configure notification settings
        services.Configure(configureOptions);

        // Register notification service interfaces
        services.AddScoped<IEmailNotificationService, EmailNotificationService>();
        services.AddScoped<ISmtpClient, SmtpClient>();
        services.AddScoped<IEmailTemplateEngine, EmailTemplateEngine>();
        services.AddScoped<IRecipientManager, RecipientManager>();

        // Register notification event bus
        services.AddSingleton<INotificationEventBus, NotificationEventBus>();

        return services;
    }

    /// <summary>
    /// Build a service provider with framework services configured
    /// </summary>
    /// <param name="configuration">Configuration instance</param>
    /// <returns>Configured service provider</returns>
    public static IServiceProvider BuildFrameworkServiceProvider(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddFrameworkServices(configuration);
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Build a service provider with notification services only
    /// </summary>
    /// <param name="configuration">Configuration instance</param>
    /// <returns>Configured service provider</returns>
    public static IServiceProvider BuildNotificationServiceProvider(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        });
        services.AddNotificationServices(configuration);
        services.ValidateNotificationConfiguration();
        services.AddSingleton<INotificationEventBus, NotificationEventBus>();
        return services.BuildServiceProvider();
    }
}
