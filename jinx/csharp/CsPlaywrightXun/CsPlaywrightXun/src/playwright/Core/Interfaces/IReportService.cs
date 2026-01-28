using CsPlaywrightXun.src.playwright.Core.Models;
using CsPlaywrightXun.src.playwright.Services.Reporting;

namespace CsPlaywrightXun.src.playwright.Core.Interfaces;

/// <summary>
/// 报告服务接口
/// </summary>
public interface IReportService
{
    /// <summary>
    /// 生成报告
    /// </summary>
    /// <param name="testReport">测试报告</param>
    /// <param name="outputPath">输出路径</param>
    /// <param name="format">报告格式</param>
    /// <returns>生成的报告文件路径</returns>
    Task<string> GenerateReportAsync(TestReport testReport, string outputPath, string format = "html");

    /// <summary>
    /// 生成多格式报告
    /// </summary>
    /// <param name="testReport">测试报告</param>
    /// <param name="outputDirectory">输出目录</param>
    /// <param name="formats">报告格式列表</param>
    /// <returns>生成的报告文件路径列表</returns>
    Task<List<string>> GenerateMultipleReportsAsync(TestReport testReport, string outputDirectory, params string[] formats);

    /// <summary>
    /// 获取支持的报告格式
    /// </summary>
    /// <returns>支持的格式列表</returns>
    List<string> GetSupportedFormats();

    /// <summary>
    /// 注册报告生成器
    /// </summary>
    /// <param name="format">格式名称</param>
    /// <param name="generator">报告生成器</param>
    void RegisterGenerator(string format, IReportGenerator generator);

    /// <summary>
    /// 生成报告摘要
    /// </summary>
    /// <param name="testReport">测试报告</param>
    /// <returns>报告摘要</returns>
    ReportSummary GenerateReportSummary(TestReport testReport);
}