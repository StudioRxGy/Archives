using System;
using System.Collections.Generic;
using System.Linq;

namespace CsPlaywrightXun.Services.Notifications
{
    /// <summary>
    /// Test suite execution result for notification purposes
    /// </summary>
    public class TestSuiteResult
    {
        /// <summary>
        /// Name of the test suite
        /// </summary>
        public string TestSuiteName { get; set; } = string.Empty;

        /// <summary>
        /// Test execution start time
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Test execution end time
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Total execution duration
        /// </summary>
        public TimeSpan Duration => EndTime - StartTime;

        /// <summary>
        /// Total number of tests executed
        /// </summary>
        public int TotalTests { get; set; }

        /// <summary>
        /// Number of tests that passed
        /// </summary>
        public int PassedTests { get; set; }

        /// <summary>
        /// Number of tests that failed
        /// </summary>
        public int FailedTests { get; set; }

        /// <summary>
        /// Number of tests that were skipped
        /// </summary>
        public int SkippedTests { get; set; }

        /// <summary>
        /// Pass rate as a percentage
        /// </summary>
        public double PassRate => TotalTests > 0 ? (double)PassedTests / TotalTests * 100 : 0;

        /// <summary>
        /// List of failed test case results
        /// </summary>
        public List<TestCaseResult> FailedTestCases { get; set; } = new();

        /// <summary>
        /// Test execution environment
        /// </summary>
        public string Environment { get; set; } = string.Empty;

        /// <summary>
        /// Additional metadata for the test execution
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// Check if the test suite has any failures
        /// </summary>
        public bool HasFailures => FailedTests > 0;

        /// <summary>
        /// Check if the test suite has critical failures
        /// </summary>
        public bool HasCriticalFailures => FailedTestCases.Any(tc => tc.IsCritical);
    }
}