using EnterpriseAutomationFramework.Services.Api;

namespace EnterpriseAutomationFramework.Core.Interfaces;

/// <summary>
/// API 性能监控器接口
/// </summary>
public interface IApiPerformanceMonitor
{
    /// <summary>
    /// 记录API性能指标
    /// </summary>
    /// <param name="endpoint">API端点</param>
    /// <param name="method">HTTP方法</param>
    /// <param name="responseTime">响应时间</param>
    /// <param name="statusCode">状态码</param>
    /// <param name="requestSize">请求大小（字节）</param>
    /// <param name="responseSize">响应大小（字节）</param>
    void RecordMetric(string endpoint, string method, TimeSpan responseTime, int statusCode, 
        long requestSize = 0, long responseSize = 0);

    /// <summary>
    /// 获取指定端点的性能统计
    /// </summary>
    /// <param name="endpoint">API端点</param>
    /// <param name="method">HTTP方法</param>
    /// <returns>性能统计</returns>
    ApiPerformanceStatistics? GetStatistics(string endpoint, string method);

    /// <summary>
    /// 获取所有端点的性能统计
    /// </summary>
    /// <returns>所有性能统计</returns>
    List<ApiPerformanceStatistics> GetAllStatistics();

    /// <summary>
    /// 清除指定端点的性能数据
    /// </summary>
    /// <param name="endpoint">API端点</param>
    /// <param name="method">HTTP方法</param>
    void ClearMetrics(string endpoint, string method);

    /// <summary>
    /// 清除所有性能数据
    /// </summary>
    void ClearAllMetrics();

    /// <summary>
    /// 获取性能报告
    /// </summary>
    /// <param name="timeRange">时间范围（小时）</param>
    /// <returns>性能报告</returns>
    ApiPerformanceReport GetPerformanceReport(int timeRange = 24);
}