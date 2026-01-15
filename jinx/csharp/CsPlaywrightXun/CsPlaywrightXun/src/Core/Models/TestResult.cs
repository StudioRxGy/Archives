using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;

namespace CsPlaywrightXun.Services.Notifications
{
    /// <summary>
    /// Test execution result for notification purposes (represents a test suite)
    /// </summary>
    public class TestExecutionResult
    {
        /// <summary>
        /// Name of the test suite
        /// </summary>
        [Required(ErrorMessage = "Test suite name is required")]
        [JsonPropertyName("testSuiteName")]
        public string TestSuiteName { get; set; } = string.Empty;

        /// <summary>
        /// Test execution start time
        /// </summary>
        [JsonPropertyName("startTime")]
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Test execution end time
        /// </summary>
        [JsonPropertyName("endTime")]
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Total execution duration
        /// </summary>
        [JsonPropertyName("duration")]
        public TimeSpan Duration => EndTime - StartTime;

        /// <summary>
        /// Total number of tests executed
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Total tests must be non-negative")]
        [JsonPropertyName("totalTests")]
        public int TotalTests { get; set; }

        /// <summary>
        /// Number of tests that passed
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Passed tests must be non-negative")]
        [JsonPropertyName("passedTests")]
        public int PassedTests { get; set; }

        /// <summary>
        /// Number of tests that failed
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Failed tests must be non-negative")]
        [JsonPropertyName("failedTests")]
        public int FailedTests { get; set; }

        /// <summary>
        /// Number of tests that were skipped
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Skipped tests must be non-negative")]
        [JsonPropertyName("skippedTests")]
        public int SkippedTests { get; set; }

        /// <summary>
        /// Pass rate as a percentage
        /// </summary>
        [JsonPropertyName("passRate")]
        public double PassRate => TotalTests > 0 ? (double)PassedTests / TotalTests * 100 : 0;

        /// <summary>
        /// List of failed test case results
        /// </summary>
        [JsonPropertyName("failedTestCases")]
        public List<TestCaseResult> FailedTestCases { get; set; } = new();

        /// <summary>
        /// Test execution environment
        /// </summary>
        [JsonPropertyName("environment")]
        public string Environment { get; set; } = string.Empty;

        /// <summary>
        /// Additional metadata for the test execution
        /// </summary>
        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// Check if the test suite has any failures
        /// </summary>
        [JsonIgnore]
        public bool HasFailures => FailedTests > 0;

        /// <summary>
        /// Check if the test suite has critical failures
        /// </summary>
        [JsonIgnore]
        public bool HasCriticalFailures => FailedTestCases.Any(tc => tc.IsCritical);

        /// <summary>
        /// Validates the test result data
        /// </summary>
        /// <returns>True if valid, false otherwise</returns>
        public bool IsValid()
        {
            try
            {
                // Basic validation
                if (string.IsNullOrWhiteSpace(TestSuiteName))
                    return false;

                if (StartTime == default || EndTime == default)
                    return false;

                if (EndTime < StartTime)
                    return false;

                if (TotalTests < 0 || PassedTests < 0 || FailedTests < 0 || SkippedTests < 0)
                    return false;

                // Test counts should add up
                if (PassedTests + FailedTests + SkippedTests != TotalTests)
                    return false;

                // Failed test cases count should match FailedTests
                if (FailedTestCases.Count != FailedTests)
                    return false;

                // Validate each failed test case
                foreach (var testCase in FailedTestCases)
                {
                    if (!testCase.IsValid())
                        return false;
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Gets validation errors for the test result
        /// </summary>
        /// <returns>List of validation error messages</returns>
        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(TestSuiteName))
                errors.Add("Test suite name is required");

            if (StartTime == default)
                errors.Add("Start time is required");

            if (EndTime == default)
                errors.Add("End time is required");

            if (EndTime < StartTime)
                errors.Add("End time must be after start time");

            if (TotalTests < 0)
                errors.Add("Total tests must be non-negative");

            if (PassedTests < 0)
                errors.Add("Passed tests must be non-negative");

            if (FailedTests < 0)
                errors.Add("Failed tests must be non-negative");

            if (SkippedTests < 0)
                errors.Add("Skipped tests must be non-negative");

            if (PassedTests + FailedTests + SkippedTests != TotalTests)
                errors.Add("Test counts do not add up to total tests");

            if (FailedTestCases.Count != FailedTests)
                errors.Add("Failed test cases count does not match failed tests count");

            // Validate each failed test case
            for (int i = 0; i < FailedTestCases.Count; i++)
            {
                var testCase = FailedTestCases[i];
                var testCaseErrors = testCase.GetValidationErrors();
                foreach (var error in testCaseErrors)
                {
                    errors.Add($"Failed test case {i}: {error}");
                }
            }

            return errors;
        }
    }

    /// <summary>
    /// Individual test case result for notification purposes
    /// </summary>
    public class TestCaseResult
    {
        /// <summary>
        /// Name of the test case
        /// </summary>
        [Required(ErrorMessage = "Test name is required")]
        [JsonPropertyName("testName")]
        public string TestName { get; set; } = string.Empty;

        /// <summary>
        /// Error message if the test failed
        /// </summary>
        [JsonPropertyName("errorMessage")]
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Stack trace if the test failed
        /// </summary>
        [JsonPropertyName("stackTrace")]
        public string StackTrace { get; set; } = string.Empty;

        /// <summary>
        /// Test execution duration
        /// </summary>
        [JsonPropertyName("duration")]
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Test category or classification
        /// </summary>
        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Whether this is considered a critical test
        /// </summary>
        [JsonPropertyName("isCritical")]
        public bool IsCritical { get; set; }

        /// <summary>
        /// Validates the test case result data
        /// </summary>
        /// <returns>True if valid, false otherwise</returns>
        public bool IsValid()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TestName))
                    return false;

                if (Duration < TimeSpan.Zero)
                    return false;

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Gets validation errors for the test case result
        /// </summary>
        /// <returns>List of validation error messages</returns>
        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(TestName))
                errors.Add("Test name is required");

            if (Duration < TimeSpan.Zero)
                errors.Add("Duration must be non-negative");

            return errors;
        }
    }
}