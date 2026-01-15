using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenQA.Selenium;
using Nunit_Cs.BasePage;
using Nunit_Cs.BasePage.Pages;
using Nunit_Cs.Config;
using Nunit_Cs.Tools;
using static NUnit.Framework.Assert;

namespace Nunit_Cs.TestCase.UI
{
    [TestFixture]
    [TestLogListener]
    public class UiTests
    {
        private IWebDriver _driver;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestContext.WriteLine("开始执行UI测试...");
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            TestContext.WriteLine("UI测试完成!");
        }

        [SetUp]
        public void SetUp()
        {
            // 使用UiTestSetup中的共享Driver实例
            if (UiTestSetup.Driver != null)
            {
                _driver = UiTestSetup.Driver;
                return;
            }
            
            // 如果没有共享实例，则创建新的
            if (_driver == null)
            {
                _driver = Browser.SelectBrowser();
                // 存储到UiTestSetup中以便共享
                UiTestSetup.Driver = _driver;
            }
        }

        [TearDown]
        public void TearDown()
        {
            // 在这里不关闭浏览器，由OneTimeTearDown在所有测试结束后统一关闭
        }

        /// <summary>
        /// 加载CSV测试数据的通用方法
        /// </summary>
        /// <param name="csvPath">CSV文件路径</param>
        /// <returns>测试数据集合</returns>
        private static IEnumerable<Dictionary<string, string>> LoadCsvData(string csvPath)
        {
            var testCases = new List<Dictionary<string, string>>();

            try
            {
                // 调试输出实际路径
                TestContext.WriteLine($"[DEBUG] 当前加载的CSV路径: {csvPath}");

                // 验证路径是否存在
                if (!File.Exists(csvPath))
                {
                    string message = $"CSV文件不存在于: {csvPath}\n" +
                                   $"请检查配置设置";
                    TestContext.WriteLine(message);
                    throw new FileNotFoundException(message);
                }

                // 读取CSV文件
                var lines = File.ReadAllLines(csvPath);
                if (lines.Length <= 1)
                {
                    TestContext.WriteLine("CSV文件为空或只有标题行");
                    return testCases;
                }

                // 获取标题行
                var headers = lines[0].Split(',');

                // 读取数据行
                for (int i = 1; i < lines.Length; i++)
                {
                    var data = lines[i].Split(',');
                    if (data.Length != headers.Length)
                    {
                        TestContext.WriteLine($"CSV第{i}行格式不正确");
                        continue;
                    }

                    var testCase = new Dictionary<string, string>();
                    for (int j = 0; j < headers.Length; j++)
                    {
                        testCase[headers[j]] = data[j];
                    }

                    testCases.Add(testCase);
                }

                return testCases;
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"[ERROR] 加载CSV失败: {ex.Message}");
                throw;
            }
        }

        [Test]
        [TestCaseSource(nameof(GetLoginTestCases))]
        [ScreenshotOnFailAttribute]
        public void TestLogin(Dictionary<string, string> data)
        {
            // 提取测试数据
            string username = data["username"];
            string password = data["password"];
            string expectedResult = data["descr"] == "登录成功" ? "pass" : "fail";

            // 执行登录
            var astralPage = new AstralPage(_driver);
            string result = astralPage.Login(username, password);

            // 测试断言
            Assert.That(result, Is.EqualTo(expectedResult), $"登录验证失败: 用例 {data["test_case"]}");
        }

        [Test]
        [TestCaseSource(nameof(GetRegisterTestCases))]
        [ScreenshotOnFailAttribute] 
        [Ignore("暂未实现注册功能测试")]
        public void TestRegister(Dictionary<string, string> data)
        {
            // 提取测试数据
            string username = data["username"];
            string email = data["email"];
            string password = data["password"];
            string expectedResult = data["descr"] == "注册成功" ? "pass" : "fail";

            // 执行注册
            var astralPage = new AstralPage(_driver);
            string result = astralPage.Register(username, email, password);

            // 测试断言
            Assert.That(result, Is.EqualTo(expectedResult), $"注册验证失败: 用例 {data["test_case"]}");
        }

        /// <summary>
        /// 从CSV中加载登录测试数据
        /// </summary>
        private static IEnumerable<Dictionary<string, string>> GetLoginTestCases()
        {
            return LoadCsvData(AppSettings.UiLoginCsvFile);
        }

        /// <summary>
        /// 从CSV中加载注册测试数据
        /// </summary>
        private static IEnumerable<Dictionary<string, string>> GetRegisterTestCases()
        {
            return LoadCsvData(AppSettings.UiRegisterCsvFile);
        }
        
        /// <summary>
        /// 手动运行UI测试的方法，用于直接从Main方法或其他非NUnit环境调用
        /// </summary>
        /// <param name="testNames">要运行的测试名称数组，为null或空则运行所有测试</param>
        /// <returns>测试结果集合，键为测试名称，值为测试结果信息</returns>
        public Dictionary<string, Program.TestResultInfo> RunTests(params string[] testNames)
        {
            // 测试结果集合
            var testResults = new Dictionary<string, Program.TestResultInfo>();
            
            try
            {
                // 记录UI测试会话开始
                Tools.LogTool.LogUiTestSessionStart();
                
                // 初始化测试环境
                OneTimeSetUp();
                
                // 确保只创建一个浏览器实例
                SetUp();
                
                // 如果没有指定具体测试，则执行所有测试
                if (testNames == null || testNames.Length == 0)
                {
                    // 执行登录测试
                    Tools.LogTool.LogUiDebug("开始执行登录测试", "RunTests");
                    foreach (var testCase in GetLoginTestCases())
                    {
                        string testName = $"登录测试_{testCase["test_case"]}";
                        var resultInfo = new Program.TestResultInfo
                        {
                            Description = $"登录测试 - {testCase["descr"]}"
                        };
                        
                        var startTime = DateTime.Now;
                        try
                        {
                            TestLogin(testCase);
                            Tools.LogTool.LogUiAction($"登录测试通过: {testCase["test_case"]}", true, (DateTime.Now - startTime).TotalSeconds);
                            resultInfo.Status = "PASS";
                        }
                        catch (Exception ex)
                        {
                            var screenshotPath = Browser.TakeScreenshot(_driver, $"fail_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.png");
                            Tools.LogTool.LogUiAction($"登录测试: {testCase["test_case"]}", false, 
                                (DateTime.Now - startTime).TotalSeconds, ex.Message, screenshotPath);
                            resultInfo.Status = "FAIL";
                            resultInfo.ErrorMessage = ex.Message;
                        }
                        finally
                        {
                            resultInfo.Duration = (long)(DateTime.Now - startTime).TotalMilliseconds;
                            testResults[testName] = resultInfo;
                        }
                    }
                    
                    // 注册测试已被忽略
                    Tools.LogTool.LogUiDebug("注册测试已被设置为忽略", "RunTests");
                    testResults["注册测试"] = new Program.TestResultInfo
                    {
                        Status = "SKIP",
                        Description = "注册功能测试（已忽略）",
                        ErrorMessage = "暂未实现注册功能测试"
                    };
                }
                else
                {
                    // 根据指定的测试名称执行特定测试
                    foreach (var testName in testNames)
                    {
                        if (testName.Equals("TestLogin", StringComparison.OrdinalIgnoreCase))
                        {
                            Tools.LogTool.LogUiDebug("开始执行指定的登录测试", "RunTests");
                            foreach (var testCase in GetLoginTestCases())
                            {
                                string fullTestName = $"登录测试_{testCase["test_case"]}";
                                var resultInfo = new Program.TestResultInfo
                                {
                                    Description = $"登录测试 - {testCase["descr"]}"
                                };
                                
                                var startTime = DateTime.Now;
                                try
                                {
                                    TestLogin(testCase);
                                    Tools.LogTool.LogUiAction($"登录测试通过: {testCase["test_case"]}", true, 
                                        (DateTime.Now - startTime).TotalSeconds);
                                    resultInfo.Status = "PASS";
                                }
                                catch (Exception ex)
                                {
                                    var screenshotPath = Browser.TakeScreenshot(_driver, $"fail_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.png");
                                    Tools.LogTool.LogUiAction($"登录测试: {testCase["test_case"]}", false, 
                                        (DateTime.Now - startTime).TotalSeconds, ex.Message, screenshotPath);
                                    resultInfo.Status = "FAIL";
                                    resultInfo.ErrorMessage = ex.Message;
                                }
                                finally
                                {
                                    resultInfo.Duration = (long)(DateTime.Now - startTime).TotalMilliseconds;
                                    testResults[fullTestName] = resultInfo;
                                }
                            }
                        }
                    }
                }
                
                // 记录测试完成
                Tools.LogTool.LogUiAction("UI测试执行完成", true, 0);
                return testResults;
            }
            catch (Exception ex)
            {
                Tools.LogTool.LogException(ex, "UI测试执行");
                throw;
            }
        }
    }
} 