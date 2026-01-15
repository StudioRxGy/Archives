using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace Nunit_Cs.Tools
{
    /// <summary>
    /// 测试执行监听器，用于自动记录测试结果
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
    public class TestLogListener : Attribute, ITestAction
    {
        private Stopwatch _stopwatch;
        private string _testName;
        private string _testLogFile;

        /// <summary>
        /// 测试执行前触发
        /// </summary>
        /// <param name="test">测试信息</param>
        public void BeforeTest(ITest test)
        {
            try
            {
                _testName = test.FullName;
                _stopwatch = Stopwatch.StartNew();
                
                // 确保日志目录存在
                string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "TestLogs");
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                    Console.WriteLine($"创建测试日志目录: {logDir}");
                }
                
                // 为每个测试创建单独的日志文件
                string safeTestName = _testName.Replace(".", "_").Replace("(", "_").Replace(")", "_").Replace(",", "_");
                _testLogFile = Path.Combine(logDir, $"Test_{safeTestName}_{DateTime.Now:yyyyMMdd_HHmmss}.log");
                
                // 写入测试开始信息
                string startInfo = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] 开始执行测试: {_testName}\n";
                File.WriteAllText(_testLogFile, startInfo, Encoding.UTF8);
                
                // 写入测试信息
                StringBuilder testInfo = new StringBuilder();
                testInfo.AppendLine($"[测试信息]");
                testInfo.AppendLine($"完整名称: {test.FullName}");
                testInfo.AppendLine($"类名: {test.ClassName}");
                testInfo.AppendLine($"方法名: {test.MethodName}");
                testInfo.AppendLine($"运行ID: {test.RunState}");
                testInfo.AppendLine($"执行时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                testInfo.AppendLine($"工作目录: {AppDomain.CurrentDomain.BaseDirectory}");
                testInfo.AppendLine("----------------------------------------------------------");
                
                File.AppendAllText(_testLogFile, testInfo.ToString(), Encoding.UTF8);
                
                // 使用LogTool记录测试开始
                LogTool.LogTestStart(_testName);
                
                // 输出简短信息到控制台
                Console.WriteLine($"[TEST-START] {_testName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TestLogListener BeforeTest异常: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        /// <summary>
        /// 测试执行后触发
        /// </summary>
        /// <param name="test">测试信息</param>
        public void AfterTest(ITest test)
        {
            try
            {
                _stopwatch.Stop();
                
                var result = TestContext.CurrentContext.Result;
                var testStatus = result.Outcome.Status;
                var message = result.Message;
                
                // 记录测试结果到单独的日志文件
                if (!string.IsNullOrEmpty(_testLogFile) && File.Exists(_testLogFile))
                {
                    StringBuilder resultInfo = new StringBuilder();
                    resultInfo.AppendLine("----------------------------------------------------------");
                    resultInfo.AppendLine($"[测试结果]");
                    resultInfo.AppendLine($"结果状态: {testStatus}");
                    resultInfo.AppendLine($"执行时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                    resultInfo.AppendLine($"持续时间: {_stopwatch.ElapsedMilliseconds}ms");
                    
                    if (!string.IsNullOrEmpty(message))
                    {
                        resultInfo.AppendLine($"结果消息: {message}");
                    }
                    
                    if (!string.IsNullOrEmpty(result.StackTrace))
                    {
                        resultInfo.AppendLine($"堆栈跟踪: \n{result.StackTrace}");
                    }
                    
                    resultInfo.AppendLine("----------------------------------------------------------");
                    File.AppendAllText(_testLogFile, resultInfo.ToString(), Encoding.UTF8);
                }
                
                // 使用LogTool记录测试结束
                LogTool.LogTestEnd(_testName, testStatus, _stopwatch.ElapsedMilliseconds, message);
                
                // 如果测试失败且有异常，记录异常详情
                if (testStatus == TestStatus.Failed && result.FailCount > 0)
                {
                    if (result.StackTrace != null)
                    {
                        // 输出错误信息到控制台
                        Console.WriteLine($"[TEST-FAIL] {_testName}");
                        Console.WriteLine($"失败原因: {result.Message}");
                        Console.WriteLine("堆栈跟踪:");
                        Console.WriteLine(result.StackTrace);
                        
                        // 不能直接设置StackTrace属性，所以将它作为单独的信息记录
                        LogTool.Log($"测试失败: {_testName}", LogLevel.Error);
                        LogTool.Log($"错误信息: {result.Message}", LogLevel.Error);
                        LogTool.Log($"堆栈跟踪: \n{result.StackTrace}", LogLevel.Error);
                    }
                }
                else
                {
                    // 输出简短信息到控制台
                    Console.WriteLine($"[TEST-END] {_testName} - 状态: {testStatus}, 耗时: {_stopwatch.ElapsedMilliseconds}ms");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TestLogListener AfterTest异常: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        /// <summary>
        /// 指定监听器的作用目标
        /// </summary>
        public ActionTargets Targets => ActionTargets.Test;
    }
} 