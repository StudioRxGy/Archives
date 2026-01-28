using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CsPlaywrightXun.Services.Notifications
{
    /// <summary>
    /// Manages recipients and notification rules for email notifications
    /// </summary>
    public class RecipientManager : IRecipientManager
    {
        private readonly ILogger<RecipientManager> _logger;
        private readonly Dictionary<string, DateTime> _lastNotificationTimes;
        private readonly Dictionary<string, int> _consecutiveFailureCounts;
        private readonly object _lockObject = new();

        /// <summary>
        /// Initializes a new instance of the RecipientManager class
        /// </summary>
        /// <param name="logger">Logger instance</param>
        public RecipientManager(ILogger<RecipientManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _lastNotificationTimes = new Dictionary<string, DateTime>();
            _consecutiveFailureCounts = new Dictionary<string, int>();
        }

        /// <summary>
        /// Gets recipients for a specific notification type based on rules and context
        /// </summary>
        /// <param name="notificationType">Type of notification</param>
        /// <param name="rules">List of notification rules</param>
        /// <param name="context">Context for rule evaluation</param>
        /// <param name="defaultRecipients">Default recipients if no rules match</param>
        /// <returns>List of recipient email addresses</returns>
        public async Task<List<string>> GetRecipientsAsync(
            NotificationType notificationType,
            List<NotificationRule> rules,
            Dictionary<string, object> context,
            List<string>? defaultRecipients = null)
        {
            var recipients = new HashSet<string>();

            try
            {
                // Process notification rules
                var applicableRules = GetApplicableRules(notificationType, rules, context);
                
                foreach (var rule in applicableRules)
                {
                    // Check cooldown period
                    if (IsInCooldownPeriod(rule))
                    {
                        _logger.LogDebug("Rule {RuleId} is in cooldown period, skipping", rule.Id);
                        continue;
                    }

                    // Add recipients from this rule
                    foreach (var recipient in rule.Recipients)
                    {
                        if (!string.IsNullOrWhiteSpace(recipient))
                        {
                            recipients.Add(recipient.Trim().ToLowerInvariant());
                        }
                    }

                    // Update last notification time for this rule
                    UpdateLastNotificationTime(rule.Id);
                }

                // Add default recipients if no rules matched or as fallback
                if (recipients.Count == 0 && defaultRecipients != null)
                {
                    foreach (var recipient in defaultRecipients)
                    {
                        if (!string.IsNullOrWhiteSpace(recipient))
                        {
                            recipients.Add(recipient.Trim().ToLowerInvariant());
                        }
                    }
                }

                // Handle escalation for consecutive failures
                if (notificationType == NotificationType.TestFailure || notificationType == NotificationType.CriticalFailure)
                {
                    var escalationRecipients = await GetEscalationRecipientsAsync(context, rules);
                    foreach (var recipient in escalationRecipients)
                    {
                        recipients.Add(recipient);
                    }
                }

                var result = recipients.ToList();
                _logger.LogInformation("Found {Count} recipients for notification type {Type}", 
                    result.Count, notificationType);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recipients for notification type {Type}", notificationType);
                
                // Return default recipients as fallback
                return defaultRecipients?.Where(r => !string.IsNullOrWhiteSpace(r)).ToList() ?? new List<string>();
            }
        }

        /// <summary>
        /// Checks if a notification should be sent based on frequency limits
        /// </summary>
        /// <param name="rule">Notification rule to check</param>
        /// <param name="notificationType">Type of notification</param>
        /// <returns>True if notification should be sent, false if rate limited</returns>
        public bool ShouldSendNotification(NotificationRule rule, NotificationType notificationType)
        {
            if (rule == null)
                return true;

            try
            {
                lock (_lockObject)
                {
                    // Check if rule is enabled
                    if (!rule.IsEnabled)
                    {
                        _logger.LogDebug("Rule {RuleId} is disabled", rule.Id);
                        return false;
                    }

                    // Check cooldown period
                    if (IsInCooldownPeriod(rule))
                    {
                        _logger.LogDebug("Rule {RuleId} is in cooldown period", rule.Id);
                        return false;
                    }

                    // For critical failures, always send (bypass rate limiting)
                    if (notificationType == NotificationType.CriticalFailure)
                    {
                        _logger.LogDebug("Critical failure notification bypasses rate limiting");
                        return true;
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if notification should be sent for rule {RuleId}", rule?.Id);
                return true; // Default to sending on error
            }
        }

        /// <summary>
        /// Updates failure count for escalation logic
        /// </summary>
        /// <param name="testSuiteName">Name of the test suite</param>
        /// <param name="isFailure">Whether this is a failure</param>
        public void UpdateFailureCount(string testSuiteName, bool isFailure)
        {
            if (string.IsNullOrWhiteSpace(testSuiteName))
                return;

            try
            {
                lock (_lockObject)
                {
                    var key = testSuiteName.ToLowerInvariant();

                    if (isFailure)
                    {
                        _consecutiveFailureCounts[key] = _consecutiveFailureCounts.GetValueOrDefault(key, 0) + 1;
                        _logger.LogDebug("Updated failure count for {TestSuite} to {Count}", 
                            testSuiteName, _consecutiveFailureCounts[key]);
                    }
                    else
                    {
                        // Reset failure count on success
                        if (_consecutiveFailureCounts.ContainsKey(key))
                        {
                            _consecutiveFailureCounts.Remove(key);
                            _logger.LogDebug("Reset failure count for {TestSuite}", testSuiteName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating failure count for test suite {TestSuite}", testSuiteName);
            }
        }

        /// <summary>
        /// Gets the current consecutive failure count for a test suite
        /// </summary>
        /// <param name="testSuiteName">Name of the test suite</param>
        /// <returns>Number of consecutive failures</returns>
        public int GetConsecutiveFailureCount(string testSuiteName)
        {
            if (string.IsNullOrWhiteSpace(testSuiteName))
                return 0;

            try
            {
                lock (_lockObject)
                {
                    var key = testSuiteName.ToLowerInvariant();
                    return _consecutiveFailureCounts.GetValueOrDefault(key, 0);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting failure count for test suite {TestSuite}", testSuiteName);
                return 0;
            }
        }

        /// <summary>
        /// Validates recipient email addresses
        /// </summary>
        /// <param name="recipients">List of recipient email addresses</param>
        /// <returns>List of valid email addresses</returns>
        public List<string> ValidateRecipients(List<string> recipients)
        {
            if (recipients == null)
                return new List<string>();

            var validRecipients = new List<string>();

            foreach (var recipient in recipients)
            {
                if (string.IsNullOrWhiteSpace(recipient))
                    continue;

                try
                {
                    var emailAddress = new System.Net.Mail.MailAddress(recipient.Trim());
                    validRecipients.Add(emailAddress.Address);
                }
                catch (FormatException)
                {
                    _logger.LogWarning("Invalid email address format: {Email}", recipient);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error validating email address: {Email}", recipient);
                }
            }

            return validRecipients;
        }

        /// <summary>
        /// Gets applicable rules for the given notification type and context
        /// </summary>
        private List<NotificationRule> GetApplicableRules(
            NotificationType notificationType,
            List<NotificationRule> rules,
            Dictionary<string, object> context)
        {
            if (rules == null)
                return new List<NotificationRule>();

            var applicableRules = new List<NotificationRule>();

            foreach (var rule in rules)
            {
                try
                {
                    // Check if rule applies to this notification type
                    if (rule.Type != notificationType)
                        continue;

                    // Check if rule should apply based on context
                    if (!rule.ShouldApply(context ?? new Dictionary<string, object>()))
                        continue;

                    // Validate rule
                    if (!rule.IsValid())
                    {
                        _logger.LogWarning("Invalid rule {RuleId} skipped", rule.Id);
                        continue;
                    }

                    applicableRules.Add(rule);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error evaluating rule {RuleId}", rule?.Id);
                }
            }

            return applicableRules;
        }

        /// <summary>
        /// Checks if a rule is in its cooldown period
        /// </summary>
        private bool IsInCooldownPeriod(NotificationRule rule)
        {
            if (rule?.CooldownPeriod == null)
                return false;

            lock (_lockObject)
            {
                if (!_lastNotificationTimes.TryGetValue(rule.Id, out var lastNotificationTime))
                    return false;

                var timeSinceLastNotification = DateTime.UtcNow - lastNotificationTime;
                return timeSinceLastNotification < rule.CooldownPeriod.Value;
            }
        }

        /// <summary>
        /// Updates the last notification time for a rule
        /// </summary>
        private void UpdateLastNotificationTime(string ruleId)
        {
            if (string.IsNullOrWhiteSpace(ruleId))
                return;

            lock (_lockObject)
            {
                _lastNotificationTimes[ruleId] = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Gets escalation recipients for consecutive failures
        /// </summary>
        private Task<List<string>> GetEscalationRecipientsAsync(
            Dictionary<string, object> context,
            List<NotificationRule> rules)
        {
            var escalationRecipients = new List<string>();

            try
            {
                // Get test suite name from context
                if (!context.TryGetValue("TestSuiteName", out var testSuiteNameObj) || 
                    testSuiteNameObj is not string testSuiteName)
                {
                    return Task.FromResult(escalationRecipients);
                }

                var failureCount = GetConsecutiveFailureCount(testSuiteName);

                // Define escalation thresholds
                var escalationThresholds = new Dictionary<int, string>
                {
                    { 3, "manager" },      // 3 consecutive failures -> notify managers
                    { 5, "director" },     // 5 consecutive failures -> notify directors
                    { 10, "executive" }    // 10 consecutive failures -> notify executives
                };

                foreach (var threshold in escalationThresholds)
                {
                    if (failureCount >= threshold.Key)
                    {
                        // Find rules with escalation conditions
                        var escalationRules = rules?.Where(r => 
                            r.Conditions.ContainsKey("escalation_level") &&
                            r.Conditions["escalation_level"].ToString() == threshold.Value) ?? Enumerable.Empty<NotificationRule>();

                        foreach (var rule in escalationRules)
                        {
                            escalationRecipients.AddRange(rule.Recipients);
                        }

                        _logger.LogInformation("Escalating notification for {TestSuite} (failure count: {Count}) to {Level}", 
                            testSuiteName, failureCount, threshold.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting escalation recipients");
            }

            return Task.FromResult(escalationRecipients.Distinct().ToList());
        }
    }
}