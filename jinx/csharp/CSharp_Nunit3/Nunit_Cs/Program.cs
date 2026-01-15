using System;
using NUnit.Framework;
using Nunit_Cs.Config;
using Nunit_Cs.TestCase.API;
using Nunit_Cs.TestCase.UI;
using System.IO;
using Nunit_Cs.Common;
using Nunit_Cs.Tools;
using System.Collections.Generic;
using System.Text;
using System.Linq;

// 全局应用测试日志监听器
[assembly: TestLogListener]

namespace Nunit_Cs
{
    // 修改为只在独立执行时才作为入口点
    public class Program
    {
#if !NUNIT
        // 添加条件编译标记，确保在测试框架运行时不会被识别为入口点
        public static void Main(string[] args)
        {
            try
            {
                // 配置日志系统
                ConfigureLogging();
                
                try
                {
                    // 检查测试数据
                    CheckTestData();
                    
                    // 解析命令行参数或显示选择菜单
                    string testType = ParseTestType(args);
                    
                    // 测试开始时间
                    DateTime startTime = DateTime.Now;
                    Dictionary<string, TestResultInfo> testResults = new Dictionary<string, TestResultInfo>();
                    
                    if (testType.Equals("ui", StringComparison.OrdinalIgnoreCase))
                    {
                        // 仅执行UI测试
                        Console.WriteLine("\n========== 执行UI测试 ==========");
                        var uiTests = new UiTests();
                        // 调用UI测试的执行方法
                        testResults = uiTests.RunTests();
                        LogTool.Log("UI测试执行完成", LogLevel.Success);
                    }
                    else if (testType.Equals("api", StringComparison.OrdinalIgnoreCase))
                    {
                        // 仅执行API测试
                        Console.WriteLine("\n========== 执行API测试 ==========");
                        var apiTests = new ApiTests();
                        // 调用API测试的执行方法
                        testResults = apiTests.RunTests();
                        LogTool.Log("API测试执行完成", LogLevel.Success);
                    }
                    else
                    {
                        // 未指定或指定了全部，执行所有测试
                        Console.WriteLine("\n========== 执行所有测试 ==========");
                        
                        // 先执行API测试
                        Console.WriteLine("\n----- 执行API测试部分 -----");
                        var apiTests = new ApiTests();
                        var apiResults = apiTests.RunTests();
                        foreach (var result in apiResults)
                        {
                            testResults[result.Key] = result.Value;
                        }
                        LogTool.Log("API测试执行完成", LogLevel.Success);
                        
                        // 再执行UI测试
                        Console.WriteLine("\n----- 执行UI测试部分 -----");
                        var uiTests = new UiTests();
                        var uiResults = uiTests.RunTests();
                        foreach (var result in uiResults)
                        {
                            testResults[result.Key] = result.Value;
                        }
                        LogTool.Log("UI测试执行完成", LogLevel.Success);
                        
                        LogTool.Log("所有测试执行完成", LogLevel.Success);
                    }
                    
                    // 测试结束时间
                    DateTime endTime = DateTime.Now;
                    TimeSpan duration = endTime - startTime;
                    
                    // 生成测试报告
                    GenerateReport(testResults, testType, startTime, endTime, duration);
                }
                catch (Exception ex)
                {
                    LogTool.LogException(ex, "测试执行");
                }
                
                LogTool.Log($"测试结束时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", LogLevel.Info);
                
                // 如果是在控制台运行，等待用户输入退出
                if (args.Length == 0)
                {
                    Console.WriteLine("\n按任意键退出...");
                    try
                    {
                        Console.ReadKey(true);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"等待用户输入时发生错误: {ex.Message}");
                        System.Threading.Thread.Sleep(2000); // 等待2秒后退出
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"初始化日志系统异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 配置日志系统
        /// </summary>
        private static void ConfigureLogging()
        {
            Console.WriteLine("\n=== 日志系统配置 ===");
            
            // 重置日志路径，确保重新生成
            AppSettings.ResetLogPath();
            
            // 1. 选择日志级别
            Console.WriteLine("\n请选择日志级别：");
            Console.WriteLine("1. Debug - 显示所有日志信息");
            Console.WriteLine("2. Info - 显示一般信息及以上");
            Console.WriteLine("3. Warning - 只显示警告和错误");
            Console.WriteLine("4. Error - 只显示错误信息");
            Console.Write("请输入选项（1-4，默认1）：");
            
            string logLevelInput = Console.ReadLine();
            LogLevel logLevel = logLevelInput switch
            {
                "2" => LogLevel.Info,
                "3" => LogLevel.Warning,
                "4" => LogLevel.Error,
                _ => LogLevel.Debug
            };
            Tools.LogTool.LogConfig.LogLevel = logLevel;
            
            // 2. 选择日志文件记录模式
            Console.WriteLine("\n请选择日志文件记录模式：");
            Console.WriteLine("1. 每次运行生成新的日志文件");
            Console.WriteLine("2. 使用单个日志文件（追加模式）");
            Console.WriteLine("3. 叠加使用单个日志文件（覆盖模式）");
            Console.Write("请输入选项（1-3，默认1）：");
            
            string logModeInput = Console.ReadLine();
            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            string singleLogFile = Path.Combine(logDir, "test.log");
            
            // 确保日志目录存在
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
            
            switch (logModeInput)
            {
                case "2": // 使用单个日志文件（追加模式）
                    Tools.LogTool.LogConfig.TimeStampInFileName = false;
                    Tools.LogTool.LogConfig.OverwriteMode = false;
                    Tools.LogTool.LogConfig.MaxLogFiles = 1;
                    // 如果文件不存在，创建空文件
                    if (!File.Exists(singleLogFile))
                    {
                        File.WriteAllText(singleLogFile, string.Empty);
                    }
                    break;
                    
                case "3": // 叠加使用单个日志文件（覆盖模式）
                    Tools.LogTool.LogConfig.TimeStampInFileName = false;
                    Tools.LogTool.LogConfig.OverwriteMode = true;
                    Tools.LogTool.LogConfig.MaxLogFiles = 1;
                    // 清空或创建文件
                    File.WriteAllText(singleLogFile, string.Empty);
                    break;
                    
                default: // 每次运行生成新的日志文件
                    Tools.LogTool.LogConfig.TimeStampInFileName = true;
                    Tools.LogTool.LogConfig.OverwriteMode = false;
                    Tools.LogTool.LogConfig.MaxLogFiles = 0;  // 不限制文件数量
                    // 清理旧的日志文件（可选，保留最近30天的）
                    try
                    {
                        var oldFiles = Directory.GetFiles(logDir, "*.log")
                            .Select(f => new FileInfo(f))
                            .Where(f => f.LastWriteTime < DateTime.Now.AddDays(-30));
                        foreach (var file in oldFiles)
                        {
                            file.Delete();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"清理旧日志文件时出错: {ex.Message}");
                    }
                    break;
            }
            
            // 3. 选择控制台日志显示模式
            Console.WriteLine("\n请选择控制台日志显示模式：");
            Console.WriteLine("1. 简单模式 - 只显示基本信息和消息");
            Console.WriteLine("2. 详细模式 - 显示完整日志信息");
            Console.WriteLine("3. 彩色模式 - 使用不同颜色显示不同级别的日志");
            Console.Write("请输入选项（1-3，默认1）：");
            
            string consoleModeInput = Console.ReadLine();
            ConsoleLogMode consoleMode = consoleModeInput switch
            {
                "2" => ConsoleLogMode.Detailed,
                "3" => ConsoleLogMode.Colored,
                _ => ConsoleLogMode.Simple
            };
            Tools.LogTool.LogConfig.ConsoleLogMode = consoleMode;
            
            // 4. 选择是否显示调用者信息
            Console.WriteLine("\n是否在日志中显示调用者信息（文件名、方法名、行号）：");
            Console.WriteLine("1. 是");
            Console.WriteLine("2. 否");
            Console.Write("请输入选项（1-2，默认1）：");
            
            string callerInfoInput = Console.ReadLine();
            Tools.LogTool.LogConfig.IncludeCallerInfo = callerInfoInput != "2";
            
            // 确保日志系统初始化
            LogTool.Log("程序启动 - 主动初始化日志系统", LogLevel.Info);
            
            // 输出日志配置信息
            LogTool.Log($"日志配置已更新：", LogLevel.Info);
            LogTool.Log($"- 日志级别：{logLevel}", LogLevel.Info);
            LogTool.Log($"- 日志文件模式：{GetLogModeDescription(logModeInput)}", LogLevel.Info);
            LogTool.Log($"- 日志文件路径：{singleLogFile}", LogLevel.Info);
            LogTool.Log($"- 最大文件大小：10MB", LogLevel.Info);
            LogTool.Log($"- 保留文件数：{Tools.LogTool.LogConfig.MaxLogFiles}", LogLevel.Info);
            LogTool.Log($"- 控制台日志模式：{consoleMode}", LogLevel.Info);
            LogTool.Log($"- 显示调用者信息：{(Tools.LogTool.LogConfig.IncludeCallerInfo ? "是" : "否")}", LogLevel.Info);
            
            Console.WriteLine("\n日志系统配置完成！");
            Console.WriteLine("----------------------------------------");
        }

        /// <summary>
        /// 检查测试数据是否存在
        /// </summary>
        private static void CheckTestData()
        {
            // 如果配置设置了删除旧文件，删除报告目录
            if (EnvironmentVars.DELETE_ON_OFF.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                if (Directory.Exists(AppSettings.ReportPath))
                {
                    try
                    {
                        Directory.Delete(AppSettings.ReportPath, true);
                        Directory.CreateDirectory(AppSettings.ReportPath);
                        Directory.CreateDirectory(Path.Combine(AppSettings.ReportPath, "img"));
                        LogTool.Log("历史报告数据清理完成", LogLevel.Info);
                    }
                    catch (Exception ex)
                    {
                        LogTool.LogException(ex, "删除历史报告数据");
                    }
                }
            }
            
            // 检查API测试文件
            if (!File.Exists(AppSettings.ApiExcelFile))
            {
                LogTool.Log($"警告：API Excel测试数据文件不存在: {AppSettings.ApiExcelFile}", LogLevel.Warning);
            }
            
            if (!File.Exists(AppSettings.ApiYamlPath))
            {
                LogTool.Log($"警告：API YAML测试数据文件不存在: {AppSettings.ApiYamlPath}", LogLevel.Warning);
            }
            
            // 检查UI测试文件
            if (!File.Exists(AppSettings.UiYamlPath))
            {
                LogTool.Log($"警告：UI YAML测试数据文件不存在: {AppSettings.UiYamlPath}", LogLevel.Warning);
            }
            
            if (!File.Exists(AppSettings.UiLoginCsvFile))
            {
                LogTool.Log($"警告：UI登录测试数据文件不存在: {AppSettings.UiLoginCsvFile}", LogLevel.Warning);
            }
            
            if (!File.Exists(AppSettings.UiRegisterCsvFile))
            {
                LogTool.Log($"警告：UI注册测试数据文件不存在: {AppSettings.UiRegisterCsvFile}", LogLevel.Warning);
            }
        }
        
        /// <summary>
        /// 解析测试类型参数，如果没有提供则显示选择菜单
        /// </summary>
        /// <param name="args">命令行参数</param>
        /// <returns>测试类型：ui、api或all</returns>
        private static string ParseTestType(string[] args)
        {
            // 如果提供了命令行参数，直接使用
            if (args.Length > 0)
            {
                return args[0].ToLower();
            }
            
            // 显示选择菜单
            Console.WriteLine("\n请选择要执行的测试类型：");
            Console.WriteLine("1. UI测试");
            Console.WriteLine("2. API测试");
            Console.WriteLine("3. 所有测试");
            Console.Write("请输入选项（1-3）：");
            
            string choice = Console.ReadLine();
            return choice switch
            {
                "1" => "ui",
                "2" => "api",
                "3" => "all",
                _ => "all"  // 默认执行所有测试
            };
        }
        
        /// <summary>
        /// 生成测试报告
        /// </summary>
        private static void GenerateReport(Dictionary<string, TestResultInfo> testResults, string testType, 
                                          DateTime startTime, DateTime endTime, TimeSpan duration)
        {
            try
            {
                LogTool.Log("开始生成测试报告...", LogLevel.Info);
                
                // 创建报告目录
                if (!Directory.Exists(AppSettings.ReportPath))
                {
                    Directory.CreateDirectory(AppSettings.ReportPath);
                }
                
                // 报告文件路径
                string reportFile = Path.Combine(AppSettings.ReportPath, $"TestReport_{DateTime.Now:yyyyMMdd_HHmmss}.html");
                
                // 计算测试结果统计
                int total = testResults.Count;
                int passed = testResults.Count(r => r.Value.Status == "PASS");
                int failed = testResults.Count(r => r.Value.Status == "FAIL");
                int skipped = testResults.Count(r => r.Value.Status == "SKIP");
                double successRate = total > 0 ? (double)passed / total * 100 : 0;
                
                // 构建HTML报告
                StringBuilder html = new StringBuilder();
                html.AppendLine("<!DOCTYPE html>");
                html.AppendLine("<html lang=\"zh-CN\">");
                html.AppendLine("<head>");
                html.AppendLine("<meta charset=\"UTF-8\">");
                html.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
                html.AppendLine("<title>自动化测试报告</title>");
                html.AppendLine("<style>");
                html.AppendLine("body { font-family: 'Microsoft YaHei', Arial, sans-serif; margin: 0; padding: 20px; color: #333; }");
                html.AppendLine("h1, h2 { color: #0066cc; }");
                html.AppendLine(".container { max-width: 1200px; margin: 0 auto; background: #fff; padding: 20px; border-radius: 5px; box-shadow: 0 0 10px rgba(0,0,0,0.1); }");
                html.AppendLine(".summary { background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin-bottom: 20px; }");
                html.AppendLine(".summary-row { display: flex; flex-wrap: wrap; }");
                html.AppendLine(".summary-item { flex: 1; min-width: 200px; margin: 10px; }");
                html.AppendLine(".summary-label { font-weight: bold; color: #555; }");
                html.AppendLine(".summary-value { font-size: 1.2em; margin-top: 5px; }");
                html.AppendLine(".status-bar { height: 20px; background: #e9ecef; border-radius: 10px; overflow: hidden; margin-top: 10px; }");
                html.AppendLine(".status-pass { height: 100%; background-color: #28a745; float: left; }");
                html.AppendLine(".status-fail { height: 100%; background-color: #dc3545; float: left; }");
                html.AppendLine(".status-skip { height: 100%; background-color: #ffc107; float: left; }");
                html.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 20px; }");
                html.AppendLine("th, td { padding: 12px 15px; text-align: left; border-bottom: 1px solid #ddd; }");
                html.AppendLine("th { background-color: #0066cc; color: white; }");
                html.AppendLine("tr:hover { background-color: #f5f5f5; }");
                html.AppendLine(".pass { color: #28a745; }");
                html.AppendLine(".fail { color: #dc3545; }");
                html.AppendLine(".skip { color: #ffc107; }");
                html.AppendLine("</style>");
                html.AppendLine("</head>");
                html.AppendLine("<body>");
                html.AppendLine("<div class=\"container\">");
                html.AppendLine($"<h1>自动化测试报告</h1>");
                html.AppendLine("<div class=\"summary\">");
                html.AppendLine("<div class=\"summary-row\">");
                html.AppendLine("<div class=\"summary-item\">");
                html.AppendLine("<div class=\"summary-label\">测试类型</div>");
                html.AppendLine($"<div class=\"summary-value\">{testType.ToUpper()}</div>");
                html.AppendLine("</div>");
                html.AppendLine("<div class=\"summary-item\">");
                html.AppendLine("<div class=\"summary-label\">测试环境</div>");
                html.AppendLine($"<div class=\"summary-value\">{EnvironmentVars.ENVIRONMENT}</div>");
                html.AppendLine("</div>");
                html.AppendLine("<div class=\"summary-item\">");
                html.AppendLine("<div class=\"summary-label\">测试人员</div>");
                html.AppendLine($"<div class=\"summary-value\">{EnvironmentVars.TESTER}</div>");
                html.AppendLine("</div>");
                html.AppendLine("</div>");
                html.AppendLine("<div class=\"summary-row\">");
                html.AppendLine("<div class=\"summary-item\">");
                html.AppendLine("<div class=\"summary-label\">开始时间</div>");
                html.AppendLine($"<div class=\"summary-value\">{startTime:yyyy-MM-dd HH:mm:ss}</div>");
                html.AppendLine("</div>");
                html.AppendLine("<div class=\"summary-item\">");
                html.AppendLine("<div class=\"summary-label\">结束时间</div>");
                html.AppendLine($"<div class=\"summary-value\">{endTime:yyyy-MM-dd HH:mm:ss}</div>");
                html.AppendLine("</div>");
                html.AppendLine("<div class=\"summary-item\">");
                html.AppendLine("<div class=\"summary-label\">持续时间</div>");
                html.AppendLine($"<div class=\"summary-value\">{duration.TotalMinutes:F1}分钟</div>");
                html.AppendLine("</div>");
                html.AppendLine("</div>");
                html.AppendLine("<div class=\"summary-row\">");
                html.AppendLine("<div class=\"summary-item\">");
                html.AppendLine("<div class=\"summary-label\">测试用例总数</div>");
                html.AppendLine($"<div class=\"summary-value\">{total}</div>");
                html.AppendLine("</div>");
                html.AppendLine("<div class=\"summary-item\">");
                html.AppendLine("<div class=\"summary-label\">通过率</div>");
                html.AppendLine($"<div class=\"summary-value\">{successRate:F2}%</div>");
                html.AppendLine("<div class=\"status-bar\">");
                html.AppendLine($"<div class=\"status-pass\" style=\"width: {(passed * 100.0 / (total > 0 ? total : 1)):F2}%\"></div>");
                html.AppendLine($"<div class=\"status-fail\" style=\"width: {(failed * 100.0 / (total > 0 ? total : 1)):F2}%\"></div>");
                html.AppendLine($"<div class=\"status-skip\" style=\"width: {(skipped * 100.0 / (total > 0 ? total : 1)):F2}%\"></div>");
                html.AppendLine("</div>");
                html.AppendLine("</div>");
                html.AppendLine("<div class=\"summary-item\">");
                html.AppendLine("<div class=\"summary-label\">结果明细</div>");
                html.AppendLine($"<div class=\"summary-value\">通过: {passed} | 失败: {failed} | 跳过: {skipped}</div>");
                html.AppendLine("</div>");
                html.AppendLine("</div>");
                html.AppendLine("</div>");

                html.AppendLine("<h2>测试用例详情</h2>");
                html.AppendLine("<table>");
                html.AppendLine("<tr>");
                html.AppendLine("<th>#</th>");
                html.AppendLine("<th>测试用例</th>");
                html.AppendLine("<th>描述</th>");
                html.AppendLine("<th>状态</th>");
                html.AppendLine("<th>持续时间</th>");
                html.AppendLine("<th>错误信息</th>");
                html.AppendLine("</tr>");

                int index = 1;
                foreach (var testResult in testResults)
                {
                    string statusClass = testResult.Value.Status == "PASS" ? "pass" : 
                                         testResult.Value.Status == "FAIL" ? "fail" : "skip";
                    
                    html.AppendLine("<tr>");
                    html.AppendLine($"<td>{index++}</td>");
                    html.AppendLine($"<td>{testResult.Key}</td>");
                    html.AppendLine($"<td>{testResult.Value.Description}</td>");
                    html.AppendLine($"<td class=\"{statusClass}\">{testResult.Value.Status}</td>");
                    html.AppendLine($"<td>{testResult.Value.Duration}ms</td>");
                    html.AppendLine($"<td>{testResult.Value.ErrorMessage}</td>");
                    html.AppendLine("</tr>");
                }

                html.AppendLine("</table>");
                html.AppendLine("</div>");
                html.AppendLine("</body>");
                html.AppendLine("</html>");

                // 写入报告文件
                File.WriteAllText(reportFile, html.ToString());
                
                LogTool.Log($"测试报告已生成: {reportFile}", LogLevel.Success);
                Console.WriteLine($"测试报告已生成: {reportFile}");
            }
            catch (Exception ex)
            {
                LogTool.LogException(ex, "生成测试报告");
                Console.WriteLine($"生成测试报告时发生错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 测试结果信息类
        /// </summary>
        public class TestResultInfo
        {
            public string Status { get; set; } = "SKIP";
            public string Description { get; set; } = "";
            public long Duration { get; set; } = 0;
            public string ErrorMessage { get; set; } = "";
        }

        /// <summary>
        /// 获取日志模式的描述文本
        /// </summary>
        private static string GetLogModeDescription(string mode)
        {
            return mode switch
            {
                "2" => "使用单个日志文件（追加模式）",
                "3" => "叠加使用单个日志文件（覆盖模式）",
                _ => "每次运行生成新的日志文件"
            };
        }
#endif
    }
} 
