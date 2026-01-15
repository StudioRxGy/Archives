using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CsPlaywrightXun.Services.Notifications
{
    /// <summary>
    /// Event bus for handling notification events in the test execution pipeline
    /// </summary>
    public class NotificationEventBus : INotificationEventBus
    {
        private readonly IEmailNotificationService _notificationService;
        private readonly ILogger<NotificationEventBus> _logger;
        private readonly ConcurrentDictionary<Type, List<Func<object, CancellationToken, Task>>> _eventHandlers;
        private readonly SemaphoreSlim _semaphore;

        public NotificationEventBus(
            IEmailNotificationService notificationService,
            ILogger<NotificationEventBus> logger)
        {
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventHandlers = new ConcurrentDictionary<Type, List<Func<object, CancellationToken, Task>>>();
            _semaphore = new SemaphoreSlim(1, 1);

            // Register default event handlers
            RegisterDefaultHandlers();
        }

        /// <summary>
        /// Subscribe to a specific event type
        /// </summary>
        /// <typeparam name="TEvent">Event type</typeparam>
        /// <param name="handler">Event handler function</param>
        public void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : class
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var eventType = typeof(TEvent);
            var wrappedHandler = new Func<object, CancellationToken, Task>((evt, token) => 
                handler((TEvent)evt, token));

            _eventHandlers.AddOrUpdate(
                eventType,
                new List<Func<object, CancellationToken, Task>> { wrappedHandler },
                (key, existing) =>
                {
                    existing.Add(wrappedHandler);
                    return existing;
                });

            _logger.LogDebug("Subscribed handler for event type: {EventType}", eventType.Name);
        }

        /// <summary>
        /// Publish an event to all subscribers
        /// </summary>
        /// <typeparam name="TEvent">Event type</typeparam>
        /// <param name="eventData">Event data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task PublishAsync<TEvent>(TEvent eventData, CancellationToken cancellationToken = default) where TEvent : class
        {
            if (eventData == null)
                throw new ArgumentNullException(nameof(eventData));

            var eventType = typeof(TEvent);
            _logger.LogDebug("Publishing event of type: {EventType}", eventType.Name);

            if (!_eventHandlers.TryGetValue(eventType, out var handlers) || handlers.Count == 0)
            {
                _logger.LogWarning("No handlers registered for event type: {EventType}", eventType.Name);
                return;
            }

            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var tasks = new List<Task>();
                foreach (var handler in handlers)
                {
                    tasks.Add(ExecuteHandlerSafely(handler, eventData, cancellationToken));
                }

                await Task.WhenAll(tasks);
                _logger.LogDebug("Successfully published event to {HandlerCount} handlers", handlers.Count);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Unsubscribe from all events (cleanup)
        /// </summary>
        public void UnsubscribeAll()
        {
            _eventHandlers.Clear();
            _logger.LogInformation("Unsubscribed from all events");
        }

        /// <summary>
        /// Get the count of handlers for a specific event type
        /// </summary>
        /// <typeparam name="TEvent">Event type</typeparam>
        /// <returns>Number of registered handlers</returns>
        public int GetHandlerCount<TEvent>() where TEvent : class
        {
            var eventType = typeof(TEvent);
            return _eventHandlers.TryGetValue(eventType, out var handlers) ? handlers.Count : 0;
        }

        /// <summary>
        /// Register default notification handlers
        /// </summary>
        private void RegisterDefaultHandlers()
        {
            // Test start event handler
            Subscribe<TestStartedEvent>(async (evt, token) =>
            {
                try
                {
                    var context = new TestExecutionContext
                    {
                        TestSuiteName = evt.TestSuiteName,
                        StartTime = evt.StartTime,
                        Environment = evt.Environment,
                        Metadata = evt.Metadata
                    };
                    await _notificationService.SendTestStartNotificationAsync(context);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to handle TestStartedEvent for suite: {TestSuiteName}", evt.TestSuiteName);
                }
            });

            // Test success event handler
            Subscribe<TestCompletedEvent>(async (evt, token) =>
            {
                try
                {
                    if (evt.IsSuccess)
                    {
                        await _notificationService.SendTestSuccessNotificationAsync(evt.Result);
                    }
                    else
                    {
                        await _notificationService.SendTestFailureNotificationAsync(evt.Result);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to handle TestCompletedEvent for suite: {TestSuiteName}", evt.Result.TestSuiteName);
                }
            });

            // Report generated event handler
            Subscribe<ReportGeneratedEvent>(async (evt, token) =>
            {
                try
                {
                    await _notificationService.SendReportGeneratedNotificationAsync(evt.ReportInfo);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to handle ReportGeneratedEvent for report: {ReportName}", evt.ReportInfo.ReportName);
                }
            });

            _logger.LogInformation("Default notification event handlers registered");
        }

        /// <summary>
        /// Execute event handler with error handling
        /// </summary>
        /// <param name="handler">Event handler</param>
        /// <param name="eventData">Event data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        private async Task ExecuteHandlerSafely(Func<object, CancellationToken, Task> handler, object eventData, CancellationToken cancellationToken)
        {
            try
            {
                await handler(eventData, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing event handler for event type: {EventType}", eventData.GetType().Name);
                // Continue processing other handlers even if one fails
            }
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            _semaphore?.Dispose();
            UnsubscribeAll();
        }
    }

    /// <summary>
    /// Interface for the notification event bus
    /// </summary>
    public interface INotificationEventBus : IDisposable
    {
        /// <summary>
        /// Subscribe to a specific event type
        /// </summary>
        /// <typeparam name="TEvent">Event type</typeparam>
        /// <param name="handler">Event handler function</param>
        void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : class;

        /// <summary>
        /// Publish an event to all subscribers
        /// </summary>
        /// <typeparam name="TEvent">Event type</typeparam>
        /// <param name="eventData">Event data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task PublishAsync<TEvent>(TEvent eventData, CancellationToken cancellationToken = default) where TEvent : class;

        /// <summary>
        /// Unsubscribe from all events
        /// </summary>
        void UnsubscribeAll();

        /// <summary>
        /// Get the count of handlers for a specific event type
        /// </summary>
        /// <typeparam name="TEvent">Event type</typeparam>
        /// <returns>Number of registered handlers</returns>
        int GetHandlerCount<TEvent>() where TEvent : class;
    }

    /// <summary>
    /// Event raised when test execution starts
    /// </summary>
    public class TestStartedEvent
    {
        public string TestSuiteName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public string Environment { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Event raised when test execution completes
    /// </summary>
    public class TestCompletedEvent
    {
        public TestSuiteResult Result { get; set; } = new();
        public bool IsSuccess { get; set; }
        public DateTime CompletedAt { get; set; }
    }

    /// <summary>
    /// Event raised when a test report is generated
    /// </summary>
    public class ReportGeneratedEvent
    {
        public ReportInfo ReportInfo { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }
}