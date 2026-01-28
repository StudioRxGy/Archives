using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using EnterpriseAutomationFramework.Services.Api;

namespace EnterpriseAutomationFramework.Tests.Services;

/// <summary>
/// ApiPerformanceMonitor 单元测试
/// </summary>
public class ApiPerformanceMonitorTests
{
    private readonly Mock<ILogger<ApiPerformanceMonitor>> _loggerMock;
    private readonly ApiPerformanceMonitor _monitor;

    public ApiPerformanceMonitorTests()
    {
        _loggerMock = new Mock<ILogger<ApiPerformanceMonitor>>();
        _monitor = new ApiPerformanceMonitor(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new ApiPerformanceMonitor(null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void RecordMetric_WithValidData_ShouldStoreMetric()
    {
        // Arrange
        var endpoint = "/api/users";
        var method = "GET";
        var responseTime = TimeSpan.FromMilliseconds(250);
        var statusCode = 200;
        var requestSize = 1024L;
        var responseSize = 2048L;

        // Act
        _monitor.RecordMetric(endpoint, method, responseTime, statusCode, requestSize, responseSize);

        // Assert
        var statistics = _monitor.GetStatistics(endpoint, method);
        statistics.Should().NotBeNull();
        statistics!.TotalRequests.Should().Be(1);
        statistics.SuccessfulRequests.Should().Be(1);
        statistics.FailedRequests.Should().Be(0);
        statistics.AverageResponseTime.Should().Be(responseTime);
        statistics.TotalRequestSize.Should().Be(requestSize);
        statistics.TotalResponseSize.Should().Be(responseSize);
    }

    [Fact]
    public void RecordMetric_WithMultipleRequests_ShouldCalculateStatistics()
    {
        // Arrange
        var endpoint = "/api/users";
        var method = "GET";

        // Act
        _monitor.RecordMetric(endpoint, method, TimeSpan.FromMilliseconds(100), 200, 500, 1000);
        _monitor.RecordMetric(endpoint, method, TimeSpan.FromMilliseconds(200), 200, 600, 1200);
        _monitor.RecordMetric(endpoint, method, TimeSpan.FromMilliseconds(300), 500, 700, 800);

        // Assert
        var statistics = _monitor.GetStatistics(endpoint, method);
        statistics.Should().NotBeNull();
        statistics!.TotalRequests.Should().Be(3);
        statistics.SuccessfulRequests.Should().Be(2);
        statistics.FailedRequests.Should().Be(1);
        statistics.SuccessRate.Should().BeApproximately(66.67, 0.01);
        statistics.AverageResponseTime.Should().Be(TimeSpan.FromMilliseconds(200));
        statistics.MinResponseTime.Should().Be(TimeSpan.FromMilliseconds(100));
        statistics.MaxResponseTime.Should().Be(TimeSpan.FromMilliseconds(300));
        statistics.TotalRequestSize.Should().Be(1800);
        statistics.TotalResponseSize.Should().Be(3000);
    }

    [Fact]
    public void GetStatistics_WithNonExistentEndpoint_ShouldReturnNull()
    {
        // Act
        var statistics = _monitor.GetStatistics("/non-existent", "GET");

        // Assert
        statistics.Should().BeNull();
    }

    [Fact]
    public void GetAllStatistics_WithMultipleEndpoints_ShouldReturnAllStatistics()
    {
        // Arrange
        _monitor.RecordMetric("/api/users", "GET", TimeSpan.FromMilliseconds(100), 200);
        _monitor.RecordMetric("/api/users", "POST", TimeSpan.FromMilliseconds(150), 201);
        _monitor.RecordMetric("/api/orders", "GET", TimeSpan.FromMilliseconds(200), 200);

        // Act
        var allStatistics = _monitor.GetAllStatistics();

        // Assert
        allStatistics.Should().HaveCount(3);
        allStatistics.Should().Contain(s => s.EndpointKey == "GET /api/users");
        allStatistics.Should().Contain(s => s.EndpointKey == "POST /api/users");
        allStatistics.Should().Contain(s => s.EndpointKey == "GET /api/orders");
    }

    [Fact]
    public void ClearMetrics_WithSpecificEndpoint_ShouldClearOnlyThatEndpoint()
    {
        // Arrange
        _monitor.RecordMetric("/api/users", "GET", TimeSpan.FromMilliseconds(100), 200);
        _monitor.RecordMetric("/api/orders", "GET", TimeSpan.FromMilliseconds(150), 200);

        // Act
        _monitor.ClearMetrics("/api/users", "GET");

        // Assert
        _monitor.GetStatistics("/api/users", "GET").Should().BeNull();
        _monitor.GetStatistics("/api/orders", "GET").Should().NotBeNull();
    }

    [Fact]
    public void ClearAllMetrics_ShouldClearAllEndpoints()
    {
        // Arrange
        _monitor.RecordMetric("/api/users", "GET", TimeSpan.FromMilliseconds(100), 200);
        _monitor.RecordMetric("/api/orders", "GET", TimeSpan.FromMilliseconds(150), 200);

        // Act
        _monitor.ClearAllMetrics();

        // Assert
        _monitor.GetAllStatistics().Should().BeEmpty();
    }

    [Fact]
    public void GetPerformanceReport_WithNoData_ShouldReturnEmptyReport()
    {
        // Act
        var report = _monitor.GetPerformanceReport(24);

        // Assert
        report.Should().NotBeNull();
        report.TotalRequests.Should().Be(0);
        report.Statistics.Should().BeEmpty();
        report.TimeRangeHours.Should().Be(24);
    }

    [Fact]
    public void GetPerformanceReport_WithData_ShouldReturnCompleteReport()
    {
        // Arrange
        _monitor.RecordMetric("/api/users", "GET", TimeSpan.FromMilliseconds(100), 200);
        _monitor.RecordMetric("/api/users", "GET", TimeSpan.FromMilliseconds(200), 200);
        _monitor.RecordMetric("/api/users", "POST", TimeSpan.FromMilliseconds(300), 201);
        _monitor.RecordMetric("/api/orders", "GET", TimeSpan.FromMilliseconds(150), 500);

        // Act
        var report = _monitor.GetPerformanceReport(24);

        // Assert
        report.Should().NotBeNull();
        report.TotalRequests.Should().Be(4);
        report.AverageResponseTime.Should().Be(TimeSpan.FromMilliseconds(187.5));
        report.MinResponseTime.Should().Be(TimeSpan.FromMilliseconds(100));
        report.MaxResponseTime.Should().Be(TimeSpan.FromMilliseconds(300));
        report.SuccessRate.Should().Be(75.0); // 3 out of 4 successful
        report.Statistics.Should().HaveCount(3);
        report.TimeRangeHours.Should().Be(24);
    }

    [Fact]
    public void GetPerformanceReport_WithTimeRange_ShouldFilterByTime()
    {
        // Arrange
        // 这个测试需要模拟时间，在实际实现中可能需要依赖注入时间提供者
        _monitor.RecordMetric("/api/users", "GET", TimeSpan.FromMilliseconds(100), 200);

        // Act
        var report = _monitor.GetPerformanceReport(1); // 1 hour range

        // Assert
        report.Should().NotBeNull();
        report.TimeRangeHours.Should().Be(1);
        // 由于时间戳是当前时间，应该包含在1小时范围内
        report.TotalRequests.Should().Be(1);
    }

    [Fact]
    public void RecordMetric_WithDifferentStatusCodes_ShouldCalculateSuccessRateCorrectly()
    {
        // Arrange
        var endpoint = "/api/test";
        var method = "GET";

        // Act - Record various status codes
        _monitor.RecordMetric(endpoint, method, TimeSpan.FromMilliseconds(100), 200); // Success
        _monitor.RecordMetric(endpoint, method, TimeSpan.FromMilliseconds(150), 201); // Success
        _monitor.RecordMetric(endpoint, method, TimeSpan.FromMilliseconds(200), 204); // Success
        _monitor.RecordMetric(endpoint, method, TimeSpan.FromMilliseconds(250), 400); // Client Error
        _monitor.RecordMetric(endpoint, method, TimeSpan.FromMilliseconds(300), 500); // Server Error

        // Assert
        var statistics = _monitor.GetStatistics(endpoint, method);
        statistics.Should().NotBeNull();
        statistics!.TotalRequests.Should().Be(5);
        statistics.SuccessfulRequests.Should().Be(3);
        statistics.FailedRequests.Should().Be(2);
        statistics.SuccessRate.Should().Be(60.0);
    }

    [Fact]
    public void Statistics_ShouldCalculatePercentilesCorrectly()
    {
        // Arrange
        var endpoint = "/api/test";
        var method = "GET";
        var responseTimes = new[] { 100, 150, 200, 250, 300, 350, 400, 450, 500, 1000 };

        // Act
        foreach (var time in responseTimes)
        {
            _monitor.RecordMetric(endpoint, method, TimeSpan.FromMilliseconds(time), 200);
        }

        // Assert
        var statistics = _monitor.GetStatistics(endpoint, method);
        statistics.Should().NotBeNull();
        statistics!.MedianResponseTime.Should().Be(TimeSpan.FromMilliseconds(325)); // Median of 10 values
        statistics.P95ResponseTime.TotalMilliseconds.Should().BeGreaterThan(500); // 95th percentile
        statistics.P99ResponseTime.TotalMilliseconds.Should().BeGreaterThan(900); // 99th percentile
    }

    [Fact]
    public void Statistics_ShouldTrackFirstAndLastRequestTimes()
    {
        // Arrange
        var endpoint = "/api/test";
        var method = "GET";
        var startTime = DateTime.UtcNow;

        // Act
        _monitor.RecordMetric(endpoint, method, TimeSpan.FromMilliseconds(100), 200);
        Thread.Sleep(10); // Small delay to ensure different timestamps
        _monitor.RecordMetric(endpoint, method, TimeSpan.FromMilliseconds(150), 200);

        // Assert
        var statistics = _monitor.GetStatistics(endpoint, method);
        statistics.Should().NotBeNull();
        statistics!.FirstRequestTime.Should().BeCloseTo(startTime, TimeSpan.FromSeconds(1));
        statistics.LastRequestTime.Should().BeAfter(statistics.FirstRequestTime);
    }

    [Fact]
    public void RecordMetric_WithZeroSizes_ShouldHandleCorrectly()
    {
        // Arrange
        var endpoint = "/api/test";
        var method = "GET";

        // Act
        _monitor.RecordMetric(endpoint, method, TimeSpan.FromMilliseconds(100), 200, 0, 0);

        // Assert
        var statistics = _monitor.GetStatistics(endpoint, method);
        statistics.Should().NotBeNull();
        statistics!.TotalRequestSize.Should().Be(0);
        statistics.TotalResponseSize.Should().Be(0);
    }

    [Fact]
    public void GetPerformanceReport_WithEmptyTimeRange_ShouldReturnEmptyReport()
    {
        // Arrange
        _monitor.RecordMetric("/api/test", "GET", TimeSpan.FromMilliseconds(100), 200);

        // Act
        var report = _monitor.GetPerformanceReport(0); // 0 hours - should exclude all data

        // Assert
        report.Should().NotBeNull();
        report.TotalRequests.Should().Be(0);
        report.Statistics.Should().BeEmpty();
        report.TimeRangeHours.Should().Be(0);
    }
}