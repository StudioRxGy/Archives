using System;
using System.Collections.Generic;
using System.Text.Json;
using CsPlaywrightXun.Services.Notifications;
using Xunit;

namespace CsPlaywrightXun.Tests.Unit.Notifications
{
    /// <summary>
    /// Tests for model serialization and deserialization
    /// </summary>
    public class SerializationTests
    {
        [Fact]
        public void EmailMessage_ShouldSerializeAndDeserializeCorrectly()
        {
            // Arrange
            var originalMessage = new EmailMessage
            {
                Id = "test-id",
                Subject = "Test Subject",
                HtmlBody = "<p>Test HTML</p>",
                PlainTextBody = "Test Plain Text",
                ToAddresses = { "test1@example.com", "test2@example.com" },
                CcAddresses = { "cc@example.com" },
                Priority = NotificationPriority.High,
                CreatedAt = DateTime.UtcNow
            };

            // Act
            var json = JsonSerializer.Serialize(originalMessage);
            var deserializedMessage = JsonSerializer.Deserialize<EmailMessage>(json);

            // Assert
            Assert.NotNull(deserializedMessage);
            Assert.Equal(originalMessage.Id, deserializedMessage.Id);
            Assert.Equal(originalMessage.Subject, deserializedMessage.Subject);
            Assert.Equal(originalMessage.HtmlBody, deserializedMessage.HtmlBody);
            Assert.Equal(originalMessage.PlainTextBody, deserializedMessage.PlainTextBody);
            Assert.Equal(originalMessage.ToAddresses.Count, deserializedMessage.ToAddresses.Count);
            Assert.Equal(originalMessage.CcAddresses.Count, deserializedMessage.CcAddresses.Count);
            Assert.Equal(originalMessage.Priority, deserializedMessage.Priority);
        }

        [Fact]
        public void TestExecutionResult_ShouldSerializeAndDeserializeCorrectly()
        {
            // Arrange
            var startTime = DateTime.UtcNow;
            var endTime = startTime.AddMinutes(5);
            var originalResult = new TestExecutionResult
            {
                TestSuiteName = "Integration Tests",
                StartTime = startTime,
                EndTime = endTime,
                TotalTests = 10,
                PassedTests = 8,
                FailedTests = 2,
                SkippedTests = 0,
                Environment = "Production",
                FailedTestCases = 
                {
                    new TestCaseResult 
                    { 
                        TestName = "Test1", 
                        ErrorMessage = "Error1",
                        StackTrace = "Stack1",
                        Duration = TimeSpan.FromSeconds(30),
                        Category = "Unit",
                        IsCritical = true
                    },
                    new TestCaseResult 
                    { 
                        TestName = "Test2", 
                        ErrorMessage = "Error2",
                        Duration = TimeSpan.FromSeconds(45),
                        Category = "Integration"
                    }
                },
                Metadata = { { "BuildNumber", "123" }, { "Branch", "main" } }
            };

            // Act
            var json = JsonSerializer.Serialize(originalResult);
            var deserializedResult = JsonSerializer.Deserialize<TestExecutionResult>(json);

            // Assert
            Assert.NotNull(deserializedResult);
            Assert.Equal(originalResult.TestSuiteName, deserializedResult.TestSuiteName);
            Assert.Equal(originalResult.StartTime, deserializedResult.StartTime);
            Assert.Equal(originalResult.EndTime, deserializedResult.EndTime);
            Assert.Equal(originalResult.TotalTests, deserializedResult.TotalTests);
            Assert.Equal(originalResult.PassedTests, deserializedResult.PassedTests);
            Assert.Equal(originalResult.FailedTests, deserializedResult.FailedTests);
            Assert.Equal(originalResult.SkippedTests, deserializedResult.SkippedTests);
            Assert.Equal(originalResult.Environment, deserializedResult.Environment);
            Assert.Equal(originalResult.FailedTestCases.Count, deserializedResult.FailedTestCases.Count);
            Assert.Equal(originalResult.Metadata.Count, deserializedResult.Metadata.Count);
            
            // Check calculated properties
            Assert.Equal(originalResult.PassRate, deserializedResult.PassRate);
            Assert.Equal(originalResult.HasFailures, deserializedResult.HasFailures);
            Assert.Equal(originalResult.HasCriticalFailures, deserializedResult.HasCriticalFailures);
        }

        [Fact]
        public void TestCaseResult_ShouldSerializeAndDeserializeCorrectly()
        {
            // Arrange
            var originalTestCase = new TestCaseResult
            {
                TestName = "Critical Test Case",
                ErrorMessage = "Critical error occurred",
                StackTrace = "at TestMethod() line 42",
                Duration = TimeSpan.FromSeconds(120),
                Category = "Integration",
                IsCritical = true
            };

            // Act
            var json = JsonSerializer.Serialize(originalTestCase);
            var deserializedTestCase = JsonSerializer.Deserialize<TestCaseResult>(json);

            // Assert
            Assert.NotNull(deserializedTestCase);
            Assert.Equal(originalTestCase.TestName, deserializedTestCase.TestName);
            Assert.Equal(originalTestCase.ErrorMessage, deserializedTestCase.ErrorMessage);
            Assert.Equal(originalTestCase.StackTrace, deserializedTestCase.StackTrace);
            Assert.Equal(originalTestCase.Duration, deserializedTestCase.Duration);
            Assert.Equal(originalTestCase.Category, deserializedTestCase.Category);
            Assert.Equal(originalTestCase.IsCritical, deserializedTestCase.IsCritical);
        }
    }
}