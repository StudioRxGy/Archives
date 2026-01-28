using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using OpenQA.Selenium;
using Nunit_Cs.BasePage;
using Nunit_Cs.Common;
using Nunit_Cs.Config;
using Nunit_Cs.Tools;

namespace Nunit_Cs.TestCase.UI
{
    /// <summary>
    /// UI测试初始化和收集类 - 对应Python的conftest.py
    /// </summary>
    [SetUpFixture]
    public class UiTestSetup
    {
        // 全局WebDriver实例
        public static IWebDriver Driver { get; set; }
        
        // 常量定义
        private static readonly string TestType = "UI";
        private static readonly string Environment = AppSettings.Configuration["environment:name"];
        private static readonly string Tester = AppSettings.Configuration["testers:tester"];
        
        [OneTimeSetUp]
        public void InitSession()
        {
            bool deleteOnOff = AppSettings.Configuration["common:delete_on_off"] == "True";
            if (deleteOnOff)
            {
                if (Directory.Exists(AppSettings.ReportPath))
                {
                    Directory.Delete(AppSettings.ReportPath, true);
                }
                TestContext.WriteLine("历史报告数据清理完成");
            }
            
            TestContext.WriteLine($"{AppSettings.Banner}\n" +
                               $"开始执行 {TestType} 自动化测试\n" +
                               $"测试环境: {Environment}\n" +
                               $"测试人员: {Tester}");
            
            if (Driver == null)
            {
                // 选择浏览器并初始化WebDriver
                string browserType = AppSettings.Configuration["browser:type"];
                Driver = Browser.SelectBrowser(browserType);
                
                // 最大化窗口
                new Nunit_Cs.BasePage.BasePage(Driver).MaxWindow();
                Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(30);
            }
            
            TestContext.WriteLine("UI自动化测试初始化完成");
            
            // 检查元素定位YAML文件是否存在
            if (File.Exists(AppSettings.UiYamlPath))
            {
                TestContext.WriteLine($"元素定位文件已找到: {AppSettings.UiYamlPath}");
                // 这里可以添加对YAML文件的基本验证
            }
            else
            {
                TestContext.WriteLine($"警告: 元素定位文件未找到: {AppSettings.UiYamlPath}");
            }
        }
        
        [OneTimeTearDown]
        public void CleanupSession()
        {
            if (Driver != null)
            {
                Driver.Quit();
                Driver = null;
            }
            
            TestContext.WriteLine($"========== {TestType} 自动化测试结束 ==========");
            
            CollectTestResults();
        }
        
        private void CollectTestResults()
        {
            // 收集测试结果
            int totalTests = 0; 
            int passedTests = 0;
            int failedTests = 0;
            int skippedTests = 0;
            int errorTests = 0;
            
            // 获取测试结果（NUnit需要使用其他方式收集全局测试结果）
            // 实际项目中，可能需要从存储的测试结果文件中读取
            
            // 计算成功率
            var successRate = totalTests > 0 ? (passedTests / (double)totalTests * 100).ToString("F2") + "%" : "0%";
            
            // 计算总耗时
            var duration = "0秒"; // 需要自行计算
            
            TestContext.WriteLine($"总用例数: {totalTests} | 通过: {passedTests}| 失败: {failedTests} | 跳过: {skippedTests} | 错误: {errorTests} | 成功率: {successRate} | 总耗时: {duration}");
            
            // 发送邮件通知
            bool emailOnOff = AppSettings.Configuration["common:email_on_off"] == "True";
            if (emailOnOff)
            {
                new EmailTool().SendEmail(
                    title: TestType,
                    environment: Environment,
                    tester: Tester,
                    total: totalTests,
                    passed: passedTests,
                    failed: failedTests,
                    skipped: skippedTests,
                    error: errorTests,
                    successRate: successRate,
                    duration: duration,
                    reportUrl: AppSettings.Configuration["common:report_url"],
                    jenkinsUrl: AppSettings.Configuration["common:jenkins_url"]
                );
            }
            
            // 发送钉钉消息
            bool dingdingOnOff = AppSettings.Configuration["common:dingding_on_off"] == "True";
            if (dingdingOnOff)
            {
                new DingTalkTool().SendDingding(
                    title: TestType,
                    environment: Environment,
                    tester: Tester,
                    total: totalTests,
                    passed: passedTests,
                    failed: failedTests,
                    skipped: skippedTests,
                    error: errorTests,
                    successRate: successRate,
                    duration: duration,
                    reportUrl: AppSettings.Configuration["common:report_url"],
                    jenkinsUrl: AppSettings.Configuration["common:jenkins_url"]
                );
            }
        }
    }
    
    // 截图处理特性
    [AttributeUsage(AttributeTargets.Method)]
    public class ScreenshotOnFailAttribute : Attribute, ITestAction
    {
        public void BeforeTest(ITest test) { }
        
        public void AfterTest(ITest test)
        {
            if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
            {
                try
                {
                    // 失败时截图
                    var driver = UiTestSetup.Driver;
                    if (driver != null)
                    {
                        // 添加截图
                        var screenshot = ((ITakesScreenshot)driver).GetScreenshot();
                        string fileName = $"fail_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.png";
                        string filePath = Path.Combine(AppSettings.UiImgPath, fileName);
                        
                        // 确保目录存在
                        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                        
                        screenshot.SaveAsFile(filePath);
                        TestContext.AddTestAttachment(filePath, "测试失败截图");
                    }
                }
                catch (Exception ex)
                {
                    TestContext.WriteLine($"截图失败: {ex.Message}");
                }
            }
        }
        
        public ActionTargets Targets => ActionTargets.Test;
    }
} 