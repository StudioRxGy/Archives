using CsPlaywrightXun.Services.Notifications;
using Xunit;

namespace CsPlaywrightXun.Tests.Unit.Notifications
{
    /// <summary>
    /// Unit tests for notification data models
    /// </summary>
    public class NotificationModelsTests
    {
        [Fact]
        public void EmailMessage_ShouldInitializeWithDefaults()
        {
            // Act
            var message = new EmailMessage();

            // Assert
            Assert.NotNull(message.Id);
            Assert.NotEmpty(message.Id);
            Assert.NotNull(message.ToAddresses);
            Assert.Empty(message.ToAddresses);
            Assert.NotNull(message.CcAddresses);
            Assert.Empty(message.CcAddresses);
            Assert.Equal(string.Empty, message.Subject);
            Assert.Equal(string.Empty, message.HtmlBody);
            Assert.Equal(string.Empty, message.PlainTextBody);
            Assert.Equal(NotificationPriority.Normal, message.Priority);
            Assert.True(message.CreatedAt <= DateTime.UtcNow);
        }

        [Fact]
        public void EmailMessage_IsValid_ShouldReturnFalseForEmptyMessage()
        {
            // Arrange
            var message = new EmailMessage();

            // Act
            var isValid = message.IsValid();

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void EmailMessage_IsValid_ShouldReturnTrueForValidMessage()
        {
            // Arrange
            var message = new EmailMessage
            {
                Subject = "Test Subject",
                HtmlBody = "<p>Test content</p>",
                ToAddresses = { "test@example.com" }
            };

            // Act
            var isValid = message.IsValid();

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void EmailMessage_GetValidationErrors_ShouldReturnErrorsForInvalidMessage()
        {
            // Arrange
            var message = new EmailMessage
            {
                Subject = "",
                ToAddresses = { "invalid-email" }
            };

            // Act
            var errors = message.GetValidationErrors();

            // Assert
            Assert.Contains("Subject is required", errors);
            Assert.Contains("Either HTML body or plain text body is required", errors);
            Assert.Contains("TO address 'invalid-email' is not a valid email address", errors);
        }

        [Fact]
        public void EmailMessage_GetAllRecipients_ShouldReturnUniqueRecipients()
        {
            // Arrange
            var message = new EmailMessage
            {
                ToAddresses = { "test1@example.com", "test2@example.com" },
                CcAddresses = { "test2@example.com", "test3@example.com" }
            };

            // Act
            var recipients = message.GetAllRecipients();

            // Assert
            Assert.Equal(3, recipients.Count);
            Assert.Contains("test1@example.com", recipients);
            Assert.Contains("test2@example.com", recipients);
            Assert.Contains("test3@example.com", recipients);
        }

        [Fact]
        public void SmtpConfiguration_ShouldInitializeWithDefaults()
        {
            // Act
            var config = new SmtpConfiguration();

            // Assert
            Assert.Equal(string.Empty, config.Host);
            Assert.Equal(587, config.Port);
            Assert.True(config.EnableSsl);
            Assert.Equal(string.Empty, config.Username);
            Assert.Equal(string.Empty, config.Password);
            Assert.Equal(string.Empty, config.FromEmail);
            Assert.Equal(string.Empty, config.FromDisplayName);
            Assert.Equal(30, config.TimeoutSeconds);
            Assert.Equal(3, config.MaxRetryAttempts);
        }

        [Fact]
        public void SmtpConfiguration_IsValid_ShouldReturnFalseForEmptyConfig()
        {
            // Arrange
            var config = new SmtpConfiguration();

            // Act
            var isValid = config.IsValid();

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void SmtpConfiguration_IsValid_ShouldReturnTrueForValidConfig()
        {
            // Arrange
            var config = new SmtpConfiguration
            {
                Host = "smtp.example.com",
                Port = 587,
                FromEmail = "test@example.com"
            };

            // Act
            var isValid = config.IsValid();

            // Assert
            Assert.True(isValid);
        }

        [Theory]
        [InlineData("", false)]
        [InlineData("invalid-email", false)]
        [InlineData("test@example.com", true)]
        public void SmtpConfiguration_IsValid_ShouldValidateFromEmail(string fromEmail, bool expectedValid)
        {
            // Arrange
            var config = new SmtpConfiguration
            {
                Host = "smtp.example.com",
                Port = 587,
                FromEmail = fromEmail
            };

            // Act
            var isValid = config.IsValid();

            // Assert
            Assert.Equal(expectedValid, isValid);
        }

        [Fact]
        public void SmtpConfiguration_GetValidationErrors_ShouldReturnErrorsForInvalidConfig()
        {
            // Arrange
            var config = new SmtpConfiguration
            {
                Host = "",
                Port = 0,
                FromEmail = "invalid-email"
            };

            // Act
            var errors = config.GetValidationErrors();

            // Assert
            Assert.Contains("SMTP Host is required", errors);
            Assert.Contains("Port must be between 1 and 65535", errors);
            Assert.Contains("From Email must be a valid email address", errors);
        }

        [Fact]
        public void TestSuiteResult_ShouldCalculatePassRateCorrectly()
        {
            // Arrange
            var result = new TestSuiteResult
            {
                TotalTests = 10,
                PassedTests = 8,
                FailedTests = 2,
                SkippedTests = 0
            };

            // Act & Assert
            Assert.Equal(80.0, result.PassRate);
            Assert.True(result.HasFailures);
        }

        [Fact]
        public void TestSuiteResult_ShouldHandleZeroTests()
        {
            // Arrange
            var result = new TestSuiteResult
            {
                TotalTests = 0,
                PassedTests = 0,
                FailedTests = 0,
                SkippedTests = 0
            };

            // Act & Assert
            Assert.Equal(0.0, result.PassRate);
            Assert.False(result.HasFailures);
        }

        [Fact]
        public void TestSuiteResult_ShouldDetectCriticalFailures()
        {
            // Arrange
            var result = new TestSuiteResult();
            result.FailedTestCases.Add(new TestCaseResult
            {
                TestName = "CriticalTest",
                IsCritical = true,
                ErrorMessage = "Critical failure"
            });

            // Act & Assert
            Assert.True(result.HasCriticalFailures);
        }

        [Fact]
        public void TestSuiteResult_ShouldCalculateDuration()
        {
            // Arrange
            var startTime = DateTime.UtcNow;
            var endTime = startTime.AddMinutes(5);
            var result = new TestSuiteResult
            {
                StartTime = startTime,
                EndTime = endTime
            };

            // Act & Assert
            Assert.Equal(TimeSpan.FromMinutes(5), result.Duration);
        }

        [Fact]
        public void NotificationRule_ShouldInitializeWithDefaults()
        {
            // Act
            var rule = new NotificationRule();

            // Assert
            Assert.NotNull(rule.Id);
            Assert.NotEmpty(rule.Id);
            Assert.NotNull(rule.Recipients);
            Assert.Empty(rule.Recipients);
            Assert.True(rule.IsEnabled);
            Assert.Null(rule.CooldownPeriod);
            Assert.NotNull(rule.Conditions);
            Assert.Empty(rule.Conditions);
        }

        [Fact]
        public void NotificationRule_IsValid_ShouldReturnFalseForEmptyRule()
        {
            // Arrange
            var rule = new NotificationRule();

            // Act
            var isValid = rule.IsValid();

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void NotificationRule_IsValid_ShouldReturnTrueForValidRule()
        {
            // Arrange
            var rule = new NotificationRule
            {
                Type = NotificationType.TestFailure,
                Recipients = { "test@example.com" }
            };

            // Act
            var isValid = rule.IsValid();

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void NotificationRule_GetValidationErrors_ShouldReturnErrorsForInvalidRule()
        {
            // Arrange
            var rule = new NotificationRule
            {
                Recipients = { "invalid-email", "test@example.com", "invalid-email" }
            };

            // Act
            var errors = rule.GetValidationErrors();

            // Assert
            Assert.Contains("Recipient 'invalid-email' is not a valid email address", errors);
            Assert.Contains("Duplicate recipient found: invalid-email", errors);
        }

        [Fact]
        public void NotificationRule_ShouldApply_ShouldReturnTrueWhenEnabled()
        {
            // Arrange
            var rule = new NotificationRule
            {
                IsEnabled = true,
                Recipients = { "test@example.com" }
            };
            var context = new Dictionary<string, object>();

            // Act
            var shouldApply = rule.ShouldApply(context);

            // Assert
            Assert.True(shouldApply);
        }

        [Fact]
        public void NotificationRule_ShouldApply_ShouldReturnFalseWhenDisabled()
        {
            // Arrange
            var rule = new NotificationRule
            {
                IsEnabled = false,
                Recipients = { "test@example.com" }
            };
            var context = new Dictionary<string, object>();

            // Act
            var shouldApply = rule.ShouldApply(context);

            // Assert
            Assert.False(shouldApply);
        }

        [Fact]
        public void NotificationRule_ShouldApply_ShouldEvaluateConditions()
        {
            // Arrange
            var rule = new NotificationRule
            {
                IsEnabled = true,
                Recipients = { "test@example.com" },
                Conditions = { { "Environment", "Production" } }
            };
            var context = new Dictionary<string, object>
            {
                { "Environment", "Production" }
            };

            // Act
            var shouldApply = rule.ShouldApply(context);

            // Assert
            Assert.True(shouldApply);
        }

        [Theory]
        [InlineData(NotificationType.TestStart)]
        [InlineData(NotificationType.TestSuccess)]
        [InlineData(NotificationType.TestFailure)]
        [InlineData(NotificationType.CriticalFailure)]
        [InlineData(NotificationType.ReportGenerated)]
        public void NotificationType_ShouldHaveAllExpectedValues(NotificationType type)
        {
            // Act & Assert
            Assert.True(Enum.IsDefined(typeof(NotificationType), type));
        }

        [Theory]
        [InlineData(NotificationPriority.Low)]
        [InlineData(NotificationPriority.Normal)]
        [InlineData(NotificationPriority.High)]
        [InlineData(NotificationPriority.Critical)]
        public void NotificationPriority_ShouldHaveAllExpectedValues(NotificationPriority priority)
        {
            // Act & Assert
            Assert.True(Enum.IsDefined(typeof(NotificationPriority), priority));
        }

        [Fact]
        public void TestExecutionResult_ShouldInitializeWithDefaults()
        {
            // Act
            var result = new TestExecutionResult();

            // Assert
            Assert.Equal(string.Empty, result.TestSuiteName);
            Assert.Equal(default, result.StartTime);
            Assert.Equal(default, result.EndTime);
            Assert.Equal(0, result.TotalTests);
            Assert.Equal(0, result.PassedTests);
            Assert.Equal(0, result.FailedTests);
            Assert.Equal(0, result.SkippedTests);
            Assert.Equal(0.0, result.PassRate);
            Assert.NotNull(result.FailedTestCases);
            Assert.Empty(result.FailedTestCases);
            Assert.Equal(string.Empty, result.Environment);
            Assert.NotNull(result.Metadata);
            Assert.Empty(result.Metadata);
            Assert.False(result.HasFailures);
            Assert.False(result.HasCriticalFailures);
        }

        [Fact]
        public void TestExecutionResult_IsValid_ShouldReturnFalseForEmptyResult()
        {
            // Arrange
            var result = new TestExecutionResult();

            // Act
            var isValid = result.IsValid();

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void TestExecutionResult_IsValid_ShouldReturnTrueForValidResult()
        {
            // Arrange
            var startTime = DateTime.UtcNow;
            var endTime = startTime.AddMinutes(5);
            var result = new TestExecutionResult
            {
                TestSuiteName = "Test Suite",
                StartTime = startTime,
                EndTime = endTime,
                TotalTests = 10,
                PassedTests = 8,
                FailedTests = 2,
                SkippedTests = 0,
                FailedTestCases = 
                {
                    new TestCaseResult { TestName = "Test1", ErrorMessage = "Error1" },
                    new TestCaseResult { TestName = "Test2", ErrorMessage = "Error2" }
                }
            };

            // Act
            var isValid = result.IsValid();

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void TestExecutionResult_GetValidationErrors_ShouldReturnErrorsForInvalidResult()
        {
            // Arrange
            var result = new TestExecutionResult
            {
                TestSuiteName = "",
                TotalTests = 5,
                PassedTests = 3,
                FailedTests = 1,
                SkippedTests = 0 // This doesn't add up to TotalTests
            };

            // Act
            var errors = result.GetValidationErrors();

            // Assert
            Assert.Contains("Test suite name is required", errors);
            Assert.Contains("Test counts do not add up to total tests", errors);
            Assert.Contains("Failed test cases count does not match failed tests count", errors);
        }

        [Fact]
        public void TestExecutionResult_ShouldCalculatePassRateCorrectly()
        {
            // Arrange
            var result = new TestExecutionResult
            {
                TotalTests = 10,
                PassedTests = 8,
                FailedTests = 2,
                SkippedTests = 0
            };

            // Act & Assert
            Assert.Equal(80.0, result.PassRate);
            Assert.True(result.HasFailures);
        }

        [Fact]
        public void TestExecutionResult_ShouldHandleZeroTests()
        {
            // Arrange
            var result = new TestExecutionResult
            {
                TotalTests = 0,
                PassedTests = 0,
                FailedTests = 0,
                SkippedTests = 0
            };

            // Act & Assert
            Assert.Equal(0.0, result.PassRate);
            Assert.False(result.HasFailures);
        }

        [Fact]
        public void TestExecutionResult_ShouldDetectCriticalFailures()
        {
            // Arrange
            var result = new TestExecutionResult();
            result.FailedTestCases.Add(new TestCaseResult
            {
                TestName = "CriticalTest",
                IsCritical = true,
                ErrorMessage = "Critical failure"
            });

            // Act & Assert
            Assert.True(result.HasCriticalFailures);
        }

        [Fact]
        public void TestExecutionResult_ShouldCalculateDuration()
        {
            // Arrange
            var startTime = DateTime.UtcNow;
            var endTime = startTime.AddMinutes(5);
            var result = new TestExecutionResult
            {
                StartTime = startTime,
                EndTime = endTime
            };

            // Act & Assert
            Assert.Equal(TimeSpan.FromMinutes(5), result.Duration);
        }

        [Fact]
        public void TestCaseResult_ShouldInitializeWithDefaults()
        {
            // Act
            var testCase = new TestCaseResult();

            // Assert
            Assert.Equal(string.Empty, testCase.TestName);
            Assert.Equal(string.Empty, testCase.ErrorMessage);
            Assert.Equal(string.Empty, testCase.StackTrace);
            Assert.Equal(TimeSpan.Zero, testCase.Duration);
            Assert.Equal(string.Empty, testCase.Category);
            Assert.False(testCase.IsCritical);
        }

        [Fact]
        public void TestCaseResult_IsValid_ShouldReturnFalseForEmptyTestCase()
        {
            // Arrange
            var testCase = new TestCaseResult();

            // Act
            var isValid = testCase.IsValid();

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void TestCaseResult_IsValid_ShouldReturnTrueForValidTestCase()
        {
            // Arrange
            var testCase = new TestCaseResult
            {
                TestName = "Test Case 1",
                Duration = TimeSpan.FromSeconds(30),
                ErrorMessage = "Test failed",
                Category = "Unit"
            };

            // Act
            var isValid = testCase.IsValid();

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void TestCaseResult_GetValidationErrors_ShouldReturnErrorsForInvalidTestCase()
        {
            // Arrange
            var testCase = new TestCaseResult
            {
                TestName = "",
                Duration = TimeSpan.FromSeconds(-10)
            };

            // Act
            var errors = testCase.GetValidationErrors();

            // Assert
            Assert.Contains("Test name is required", errors);
            Assert.Contains("Duration must be non-negative", errors);
        }
    }
}