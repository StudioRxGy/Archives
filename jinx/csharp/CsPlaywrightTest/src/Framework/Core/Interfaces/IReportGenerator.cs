using EnterpriseAutomationFramework.Core.Models;

namespace EnterpriseAutomationFramework.Core.Interfaces;

/// <summary>
/// 报告生成器接口
/// </summary>
public interface IReportGenerator
{
    /// <summary>
    /// 生成报告
    /// </summary>
    /// <param name="testReport">测试报告</param>
    /// <param name="outputPath">输出路径</param>
    /// <returns>生成的报告文件路径</returns>
    Task<string> GenerateReportAsync(TestReport testReport, string outputPath);
}