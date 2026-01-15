using System;
using System.Threading.Tasks;

namespace CsPlaywrightXun.Services.Notifications
{
    /// <summary>
    /// Interface for error recovery strategies in the notification system
    /// </summary>
    public interface IErrorRecoveryStrategy
    {
        /// <summary>
        /// Attempts to recover from an error by retrying the operation
        /// </summary>
        /// <param name="operation">The operation to retry</param>
        /// <param name="maxAttempts">Maximum number of retry attempts</param>
        /// <param name="delayBetweenAttempts">Delay between retry attempts</param>
        /// <param name="onRetry">Callback executed on each retry attempt</param>
        /// <returns>Task representing the async operation</returns>
        Task<T> RetryAsync<T>(
            Func<Task<T>> operation,
            int maxAttempts = 3,
            TimeSpan? delayBetweenAttempts = null,
            Func<int, Exception, Task>? onRetry = null);

        /// <summary>
        /// Attempts to recover from an error by retrying the operation (void return)
        /// </summary>
        /// <param name="operation">The operation to retry</param>
        /// <param name="maxAttempts">Maximum number of retry attempts</param>
        /// <param name="delayBetweenAttempts">Delay between retry attempts</param>
        /// <param name="onRetry">Callback executed on each retry attempt</param>
        /// <returns>Task representing the async operation</returns>
        Task RetryAsync(
            Func<Task> operation,
            int maxAttempts = 3,
            TimeSpan? delayBetweenAttempts = null,
            Func<int, Exception, Task>? onRetry = null);

        /// <summary>
        /// Executes an operation with circuit breaker pattern
        /// </summary>
        /// <param name="operation">The operation to execute</param>
        /// <param name="circuitBreakerKey">Unique key for the circuit breaker</param>
        /// <param name="failureThreshold">Number of failures before opening circuit</param>
        /// <param name="recoveryTimeout">Time to wait before attempting to close circuit</param>
        /// <returns>Task representing the async operation</returns>
        Task<T> ExecuteWithCircuitBreakerAsync<T>(
            Func<Task<T>> operation,
            string circuitBreakerKey,
            int failureThreshold = 5,
            TimeSpan? recoveryTimeout = null);

        /// <summary>
        /// Executes an operation with fallback strategy
        /// </summary>
        /// <param name="primaryOperation">Primary operation to attempt</param>
        /// <param name="fallbackOperation">Fallback operation if primary fails</param>
        /// <param name="shouldUseFallback">Predicate to determine if fallback should be used</param>
        /// <returns>Task representing the async operation</returns>
        Task<T> ExecuteWithFallbackAsync<T>(
            Func<Task<T>> primaryOperation,
            Func<Task<T>> fallbackOperation,
            Func<Exception, bool>? shouldUseFallback = null);

        /// <summary>
        /// Gets the current status of a circuit breaker
        /// </summary>
        /// <param name="circuitBreakerKey">Circuit breaker key</param>
        /// <returns>Circuit breaker status</returns>
        CircuitBreakerStatus GetCircuitBreakerStatus(string circuitBreakerKey);
    }

    /// <summary>
    /// Circuit breaker status enumeration
    /// </summary>
    public enum CircuitBreakerStatus
    {
        Closed,    // Normal operation
        Open,      // Circuit is open, requests are failing fast
        HalfOpen   // Testing if service has recovered
    }

    /// <summary>
    /// Circuit breaker state information
    /// </summary>
    public class CircuitBreakerState
    {
        public string Key { get; set; } = string.Empty;
        public CircuitBreakerStatus Status { get; set; }
        public int FailureCount { get; set; }
        public DateTime LastFailureTime { get; set; }
        public DateTime? LastSuccessTime { get; set; }
        public int FailureThreshold { get; set; }
        public TimeSpan RecoveryTimeout { get; set; }
    }
}