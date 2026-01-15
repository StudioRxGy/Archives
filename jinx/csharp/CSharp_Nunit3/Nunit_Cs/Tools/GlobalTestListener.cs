using System;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace Nunit_Cs.Tools
{
    /// <summary>
    /// 全局测试执行监听器，自动应用到所有测试
    /// </summary>
    [SetUpFixture]
    public class GlobalTestListener
    {
        // 记录测试套件开始时间
        private DateTime _startTime;
        // 日志文件路径
        private string _setupLogFile;

        [OneTimeSetUp]
        public void GlobalSetup()
        {
            try
            {
                // 确保日志目录存在
                string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                    Console.WriteLine($"GlobalTestListener: 创建日志目录 {logDir}");
                }

                // 创建一个测试日志文件验证权限
                _setupLogFile = Path.Combine(logDir, $"Global_Setup_{DateTime.Now:yyyyMMdd_HHmmss}.log");
                File.WriteAllText(_setupLogFile, $"GlobalTestListener Setup: {DateTime.Now}\n", Encoding.UTF8);
                Console.WriteLine($"GlobalTestListener: 创建初始化文件 {_setupLogFile}");
                
                // 记录开始时间
                _startTime = DateTime.Now;
                
                // 输出一些诊断信息到控制台
                Console.WriteLine("========================================================");
                Console.WriteLine($"GlobalTestListener: 测试套件开始执行：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"GlobalTestListener: 操作系统：{Environment.OSVersion}");
                Console.WriteLine($"GlobalTestListener: .NET 版本：{Environment.Version}");
                Console.WriteLine($"GlobalTestListener: 工作目录：{AppDomain.CurrentDomain.BaseDirectory}");
                Console.WriteLine("========================================================");
                
                // 使用LogTool记录测试套件启动信息
                LogTool.Log($"=== 测试套件开始执行：{DateTime.Now:yyyy-MM-dd HH:mm:ss} ===", LogLevel.Test);
                LogTool.Log($"操作系统：{Environment.OSVersion}", LogLevel.Info);
                LogTool.Log($".NET 版本：{Environment.Version}", LogLevel.Info);
                LogTool.Log($"机器名称：{Environment.MachineName}", LogLevel.Info);
                LogTool.Log($"用户名称：{Environment.UserName}", LogLevel.Info);
                LogTool.Log($"工作目录：{AppDomain.CurrentDomain.BaseDirectory}", LogLevel.Info);
                LogTool.Log("========================================================", LogLevel.Info);
                
                // 验证LogTool是否正常工作
                File.AppendAllText(_setupLogFile, "LogTool初始化检查完成\n", Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GlobalTestListener Setup异常: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        [OneTimeTearDown]
        public void GlobalTearDown()
        {
            try
            {
                // 计算总耗时
                var duration = DateTime.Now - _startTime;
                var durationText = duration.TotalSeconds < 60 
                    ? $"{duration.TotalSeconds:F2} 秒"
                    : $"{duration.TotalMinutes:F2} 分钟";
                
                // 汇总测试结果
                var results = TestContext.CurrentContext.Result;
                
                // 直接输出信息到控制台
                Console.WriteLine("========================================================");
                Console.WriteLine($"GlobalTestListener: 测试套件执行完成：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"GlobalTestListener: 总耗时：{durationText}");
                if (results != null)
                {
                    Console.WriteLine($"GlobalTestListener: 总结果：{results.Outcome.Status}，通过数：{results.PassCount}，失败数：{results.FailCount}，跳过数：{results.SkipCount}");
                }
                Console.WriteLine("========================================================");
                
                // 记录测试套件结束信息
                LogTool.Log("========================================================", LogLevel.Info);
                LogTool.Log($"测试套件执行完成：{DateTime.Now:yyyy-MM-dd HH:mm:ss}", LogLevel.Test);
                LogTool.Log($"总耗时：{durationText}", LogLevel.Info);
                if (results != null)
                {
                    LogTool.Log($"总结果：{results.Outcome.Status}，通过数：{results.PassCount}，失败数：{results.FailCount}，跳过数：{results.SkipCount}", LogLevel.Info);
                }
                LogTool.Log("========================================================", LogLevel.Info);
                
                // 在初始化文件中添加结束标记
                if (!string.IsNullOrEmpty(_setupLogFile) && File.Exists(_setupLogFile))
                {
                    File.AppendAllText(_setupLogFile, $"GlobalTestListener TearDown: {DateTime.Now}\n", Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GlobalTestListener TearDown异常: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
} 