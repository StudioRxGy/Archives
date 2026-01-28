# Notification Event Bus

The Notification Event Bus provides a decoupled way to handle test execution events and trigger email notifications in the CsPlaywrightXun framework.

## Overview

The event bus system consists of:

- **NotificationEventBus**: Core event bus implementation
- **NotificationIntegratedTestExecutionManager**: Test execution manager with integrated event publishing
- **Event Classes**: Strongly-typed event data classes
- **Service Extensions**: Dependency injection configuration

## Key Components

### Event Bus Interface

```csharp
public interface INotificationEventBus : IDisposable
{
    void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : class;
    Task PublishAsync<TEvent>(TEvent eventData, CancellationToken cancellationToken = default) where TEvent : class;
    void UnsubscribeAll();
    int GetHandlerCount<TEvent>() where TEvent : class;
}
```

### Event Types

1. **TestStartedEvent**: Published when test execution begins
2. **TestCompletedEvent**: Published when test execution completes (success or failure)
3. **ReportGeneratedEvent**: Published when test reports are generated

### Default Event Handlers

The event bus automatically registers handlers for:
- Test start notifications → Email notification service
- Test completion notifications → Email notification service (success/failure)
- Report generation notifications → Email notification service

## Usage

### Basic Setup

```csharp
// In your DI container setup
services.AddNotificationServices(options =>
{
    options.EnableAutoEventPublishing = true;
    options.EnableDetailedLogging = true;
    options.MaxConcurrentHandlers = 10;
});
```

### Using the Integrated Test Manager

```csharp
// Inject the integrated test manager
var testManager = serviceProvider.GetRequiredService<NotificationIntegratedTestExecutionManager>();

// Execute tests - events are published automatically
var result = await testManager.ExecuteUITestsOnlyAsync();

// Publish report events manually
await testManager.PublishReportGeneratedEventAsync("Report Name", "/path/to/report.html");
```

### Custom Event Handlers

```csharp
// Subscribe to events with custom logic
eventBus.Subscribe<TestStartedEvent>(async (evt, token) =>
{
    // Custom logic when tests start
    Console.WriteLine($"Test started: {evt.TestSuiteName}");
    await SomeCustomLogicAsync();
});

eventBus.Subscribe<TestCompletedEvent>(async (evt, token) =>
{
    // Custom logic when tests complete
    if (!evt.IsSuccess)
    {
        await HandleTestFailureAsync(evt.Result);
    }
});
```

### Manual Event Publishing

```csharp
// Publish events manually
var testStartedEvent = new TestStartedEvent
{
    TestSuiteName = "My Test Suite",
    StartTime = DateTime.UtcNow,
    Environment = "Production",
    Metadata = new Dictionary<string, object>
    {
        ["ProjectPath"] = "/my/project",
        ["ExecutionId"] = Guid.NewGuid().ToString()
    }
};

await eventBus.PublishAsync(testStartedEvent);
```

## Integration with Test Execution

The `NotificationIntegratedTestExecutionManager` wraps the existing `TestExecutionManager` and automatically publishes events:

1. **Before test execution**: Publishes `TestStartedEvent`
2. **After test execution**: Publishes `TestCompletedEvent` with results
3. **On report generation**: Publishes `ReportGeneratedEvent`

## Error Handling

The event bus includes robust error handling:

- Individual handler failures don't affect other handlers
- Errors are logged but don't stop event processing
- Concurrent handler execution with configurable limits
- Timeout protection for long-running handlers

## Configuration Options

```csharp
public class NotificationServiceOptions
{
    public bool EnableAutoEventPublishing { get; set; } = true;
    public int MaxConcurrentHandlers { get; set; } = 10;
    public TimeSpan HandlerTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool EnableDetailedLogging { get; set; } = false;
}
```

## Thread Safety

The event bus is thread-safe and supports:
- Concurrent event publishing
- Concurrent handler execution
- Safe subscription/unsubscription during event processing

## Performance Considerations

- Events are processed asynchronously
- Handlers run concurrently (up to configured limit)
- Memory-efficient event storage
- Automatic cleanup of completed handlers

## Examples

See `EventBusUsageExample.cs` for complete working examples including:
- Basic event publishing and handling
- Integration with test execution
- Error handling scenarios
- Custom event handlers

## Requirements Validation

This implementation satisfies the following requirements:

- **1.1, 1.2, 1.3**: Automatic test execution event publishing and email notifications
- **Integration**: Seamless integration with existing test execution flow
- **Decoupling**: Event-driven architecture separates concerns
- **Extensibility**: Easy to add new event types and handlers
- **Reliability**: Robust error handling and thread safety