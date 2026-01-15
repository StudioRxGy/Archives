using System;
using System.Collections.Generic;

namespace CsPlaywrightXun.Services.Notifications.Models
{
    /// <summary>
    /// Context model for test start notification templates
    /// </summary>
    public class TestStartContext
    {
        public string TestSuiteName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public string Environment { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
        public string ProjectName => Metadata.TryGetValue("ProjectName", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
    }

    /// <summary>
    /// Context model for test success notification templates
    /// </summary>
    public class TestSuccessContext
    {
        public string TestSuiteName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
        public int SkippedTests { get; set; }
        public double PassRate { get; set; }
        public string Environment { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
        public string ProjectName => Metadata.TryGetValue("ProjectName", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
    }

    /// <summary>
    /// Context model for test failure notification templates
    /// </summary>
    public class TestFailureContext
    {
        public string TestSuiteName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
        public int SkippedTests { get; set; }
        public double PassRate { get; set; }
        public string Environment { get; set; } = string.Empty;
        public List<FailedTestContext> FailedTestCases { get; set; } = new();
        public bool HasCriticalFailures { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public string ProjectName => Metadata.TryGetValue("ProjectName", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
    }

    /// <summary>
    /// Context model for individual failed test cases
    /// </summary>
    public class FailedTestContext
    {
        public string TestName { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string StackTrace { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public string Category { get; set; } = string.Empty;
        public bool IsCritical { get; set; }
    }

    /// <summary>
    /// Context model for report generation notification templates
    /// </summary>
    public class ReportGeneratedContext
    {
        public string ReportName { get; set; } = string.Empty;
        public string ReportType { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public string ReportUrl { get; set; } = string.Empty;
        public string ReportPath { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string TestSuiteName { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
        public string ProjectName => Metadata.TryGetValue("ProjectName", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
        
        public string FileSizeFormatted
        {
            get
            {
                if (FileSizeBytes < 1024)
                    return $"{FileSizeBytes} B";
                if (FileSizeBytes < 1024 * 1024)
                    return $"{FileSizeBytes / 1024.0:F1} KB";
                if (FileSizeBytes < 1024 * 1024 * 1024)
                    return $"{FileSizeBytes / (1024.0 * 1024.0):F1} MB";
                return $"{FileSizeBytes / (1024.0 * 1024.0 * 1024.0):F1} GB";
            }
        }
    }
}