using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace CsPlaywrightXun.Services.Notifications
{
    /// <summary>
    /// Error recovery strategy implementation for the notification system
    /// </summary>
    public class ErrorRecoveryStrategy : IErrorRecoveryStrategy
    {
        private readonly ILogger<ErrorRecoveryStrategy> _logger;
        private readonly NotificationExceptionHandler _exceptionHandler;
        private readonly ConcurrentDictionary<string, CircuitBreakerState> _circuitBreakers;
        private readonly object _lockObject = new();

        private static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan DefaultRecoveryTimeout = TimeSpan.FromMinutes(5);

        public ErrorRecoveryStrategy(
            ILogger<ErrorRecoveryStrategy> logger,
            NotificationExceptionHandler exceptionHandler)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
            _circuitBreakers = new ConcurrentDictionary<string, CircuitBreakerState>();
        }

        /// <summary>
        /// Attempts to recover from an error by retrying the operation
        /// </summary>
        /// <param name="operation">The operation to retry</param>
        /// <param name="maxAttempts">Maximum number of retry attempts</param>
        /// <param name="delayBetweenAttempts">Delay between retry attempts</param>
        /// <param name="onRetry">Callback executed on each retry attempt</param>
        /// <returns>Task representing the async operation</returns>
        public async Task<T> RetryAsync<T>(
            Func<Task<T>> operation,
            int maxAttempts = 3,
            TimeSpan? delayBetweenAttempts = null,
            Func<int, Exception, Task>? onRetry = null)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            if (maxAttempts <= 0)
                throw new ArgumentException("Max attempts must be greater than 0", nameof(maxAttempts));

            var delay = delayBetweenAttempts ?? DefaultRetryDelay;
            Exception? lastException = null;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    _logger.LogDebug("Executing operation attempt {Attempt} of {MaxAttempts}", attempt, maxAttempts);
                    var result = await operation();
                    
                    if (attempt > 1)
                    {
                        _logger.LogInformation("Operation succeeded on attempt {Attempt}", attempt);
                    }
                    
                    return result;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    
                    _logger.LogWarning(ex, "Operation failed on attempt {Attempt} of {MaxAttempts}", attempt, maxAttempts);
                    
                    // Handle the exception
                    await _exceptionHandler.HandleExceptionAsync(ex, $"Retry Attempt {attempt}");
                    
                    // Call retry callback if provided
                    if (onRetry != null)
                    {
                        await onRetry(attempt, ex);
                    }
                    
                    // Don't delay after the last attempt
                    if (attempt < maxAttempts)
                    {
                        _logger.LogDebug("Waiting {Delay} before next retry attempt", delay);
                        await Task.Delay(delay);
                        
                        // Exponential backoff: double the delay for next attempt
                        delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 2, 30000)); // Max 30 seconds
                    }
                }
            }

            _logger.LogError(lastException, "Operation failed after {MaxAttempts} attempts", maxAttempts);
            throw new InvalidOperationException($"Operation failed after {maxAttempts} attempts", lastException);
        }

        /// <summary>
        /// Attempts to recover from an error by retrying the operation (void return)
        /// </summary>
        /// <param name="operation">The operation to retry</param>
        /// <param name="maxAttempts">Maximum number of retry attempts</param>
        /// <param name="delayBetweenAttempts">Delay between retry attempts</param>
        /// <param name="onRetry">Callback executed on each retry attempt</param>
        /// <returns>Task representing the async operation</returns>
        public async Task RetryAsync(
            Func<Task> operation,
            int maxAttempts = 3,
            TimeSpan? delayBetweenAttempts = null,
            Func<int, Exception, Task>? onRetry = null)
        {
            await RetryAsync(async () =>
            {
                await operation();
                return true; // Dummy return value
            }, maxAttempts, delayBetweenAttempts, onRetry);
        }

        /// <summary>
        /// Executes an operation with circuit breaker pattern
        /// </summary>
        /// <param name="operation">The operation to execute</param>
        /// <param name="circuitBreakerKey">Unique key for the circuit breaker</param>
        /// <param name="failureThreshold">Number of failures before opening circuit</param>
        /// <param name="recoveryTimeout">Time to wait before attempting to close circuit</param>
        /// <returns>Task representing the async operation</returns>
        public async Task<T> ExecuteWithCircuitBreakerAsync<T>(
            Func<Task<T>> operation,
            string circuitBreakerKey,
            int failureThreshold = 5,
            TimeSpan? recoveryTimeout = null)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            if (string.IsNullOrWhiteSpace(circuitBreakerKey))
                throw new ArgumentException("Circuit breaker key cannot be null or empty", nameof(circuitBreakerKey));

            var timeout = recoveryTimeout ?? DefaultRecoveryTimeout;
            var circuitBreaker = GetOrCreateCircuitBreaker(circuitBreakerKey, failureThreshold, timeout);

            // Check circuit breaker state
            lock (_lockObject)
            {
                var now = DateTime.UtcNow;

                switch (circuitBreaker.Status)
                {
                    case CircuitBreakerStatus.Open:
                        // Check if recovery timeout has passed
                        if (now - circuitBreaker.LastFailureTime >= circuitBreaker.RecoveryTimeout)
                        {
                            _logger.LogInformation("Circuit breaker {Key} transitioning to HalfOpen", circuitBreakerKey);
                            circuitBreaker.Status = CircuitBreakerStatus.HalfOpen;
                        }
                        else
                        {
                            _logger.LogWarning("Circuit breaker {Key} is Open, failing fast", circuitBreakerKey);
                            throw new InvalidOperationException($"Circuit breaker '{circuitBreakerKey}' is open");
                        }
                        break;

                    case CircuitBreakerStatus.HalfOpen:
                        _logger.LogDebug("Circuit breaker {Key} is HalfOpen, testing operation", circuitBreakerKey);
                        break;

                    case CircuitBreakerStatus.Closed:
                        _logger.LogDebug("Circuit breaker {Key} is Closed, executing normally", circuitBreakerKey);
                        break;
                }
            }

            try
            {
                var result = await operation();
                
                // Operation succeeded
                lock (_lockObject)
                {
                    if (circuitBreaker.Status != CircuitBreakerStatus.Closed)
                    {
                        _logger.LogInformation("Circuit breaker {Key} closing after successful operation", circuitBreakerKey);
                        circuitBreaker.Status = CircuitBreakerStatus.Closed;
                        circuitBreaker.FailureCount = 0;
                        circuitBreaker.LastSuccessTime = DateTime.UtcNow;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                // Operation failed
                lock (_lockObject)
                {
                    circuitBreaker.FailureCount++;
                    circuitBreaker.LastFailureTime = DateTime.UtcNow;

                    if (circuitBreaker.FailureCount >= circuitBreaker.FailureThreshold)
                    {
                        _logger.LogWarning("Circuit breaker {Key} opening after {FailureCount} failures", 
                            circuitBreakerKey, circuitBreaker.FailureCount);
                        circuitBreaker.Status = CircuitBreakerStatus.Open;
                    }
                }

                await _exceptionHandler.HandleExceptionAsync(ex, $"Circuit Breaker ({circuitBreakerKey})");
                throw;
            }
        }

        /// <summary>
        /// Executes an operation with fallback strategy
        /// </summary>
        /// <param name="primaryOperation">Primary operation to attempt</param>
        /// <param name="fallbackOperation">Fallback operation if primary fails</param>
        /// <param name="shouldUseFallback">Predicate to determine if fallback should be used</param>
        /// <returns>Task representing the async operation</returns>
        public async Task<T> ExecuteWithFallbackAsync<T>(
            Func<Task<T>> primaryOperation,
            Func<Task<T>> fallbackOperation,
            Func<Exception, bool>? shouldUseFallback = null)
        {
            if (primaryOperation == null)
                throw new ArgumentNullException(nameof(primaryOperation));

            if (fallbackOperation == null)
                throw new ArgumentNullException(nameof(fallbackOperation));

            try
            {
                _logger.LogDebug("Executing primary operation");
                return await primaryOperation();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Primary operation failed, checking fallback conditions");
                
                await _exceptionHandler.HandleExceptionAsync(ex, "Primary Operation");

                // Determine if we should use fallback
                var useFallback = shouldUseFallback?.Invoke(ex) ?? true;

                if (useFallback)
                {
                    try
                    {
                        _logger.LogInformation("Executing fallback operation");
                        return await fallbackOperation();
                    }
                    catch (Exception fallbackEx)
                    {
                        _logger.LogError(fallbackEx, "Fallback operation also failed");
                        await _exceptionHandler.HandleExceptionAsync(fallbackEx, "Fallback Operation");
                        
                        // Throw aggregate exception with both errors
                        throw new AggregateException("Both primary and fallback operations failed", ex, fallbackEx);
                    }
                }
                else
                {
                    _logger.LogInformation("Fallback not applicable for this error type, rethrowing original exception");
                    throw;
                }
            }
        }

        /// <summary>
        /// Gets the current status of a circuit breaker
        /// </summary>
        /// <param name="circuitBreakerKey">Circuit breaker key</param>
        /// <returns>Circuit breaker status</returns>
        public CircuitBreakerStatus GetCircuitBreakerStatus(string circuitBreakerKey)
        {
            if (string.IsNullOrWhiteSpace(circuitBreakerKey))
                return CircuitBreakerStatus.Closed;

            return _circuitBreakers.TryGetValue(circuitBreakerKey, out var state) 
                ? state.Status 
                : CircuitBreakerStatus.Closed;
        }

        /// <summary>
        /// Gets or creates a circuit breaker state
        /// </summary>
        /// <param name="key">Circuit breaker key</param>
        /// <param name="failureThreshold">Failure threshold</param>
        /// <param name="recoveryTimeout">Recovery timeout</param>
        /// <returns>Circuit breaker state</returns>
        private CircuitBreakerState GetOrCreateCircuitBreaker(
            string key, 
            int failureThreshold, 
            TimeSpan recoveryTimeout)
        {
            return _circuitBreakers.GetOrAdd(key, _ => new CircuitBreakerState
            {
                Key = key,
                Status = CircuitBreakerStatus.Closed,
                FailureCount = 0,
                FailureThreshold = failureThreshold,
                RecoveryTimeout = recoveryTimeout,
                LastFailureTime = DateTime.MinValue
            });
        }
    }
}