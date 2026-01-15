using System;
using System.Collections.Generic;
using NUnit.Framework;
using Nunit_Cs.Config;
using Nunit_Cs.Tools;
using Nunit_Cs.Common;
using static NUnit.Framework.Assert;

namespace Nunit_Cs.TestCase.API
{
    [TestFixture]
    [TestLogListener]
    public class ApiTests
    {
        private ExcelTool _excelTool;
        private YamlTool _yamlTool;
        private TestManager _testManager;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestContext.WriteLine("开始执行API测试...");
            _excelTool = new ExcelTool();
            _yamlTool = new YamlTool(AppSettings.ApiYamlPath);
            _testManager = new TestManager();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            TestContext.WriteLine("API测试完成!");
        }

        [Test, TestCaseSource(nameof(GetExcelTestCases))]
        public void TestExcelApi(Dictionary<string, object> testCase)
        {
            TestContext.WriteLine($"开始执行用例: {testCase["name"]}");

            // 检查是否需要执行
            if (testCase["run"]?.ToString().ToLower() != "yes")
            {
                TestContext.WriteLine($"跳过用例: {testCase["name"]}");
                _excelTool.WriteResultToExcel((int)testCase["row"], 
                    _testManager.CreateResult(testCase["name"].ToString(), Constants.TestStatus.SKIP));
                Assert.Ignore("跳过执行");
                return;
            }

            // 执行测试用例
            var result = _testManager.ExecuteTestCase(testCase);

            // 写入结果
            _excelTool.WriteResultToExcel((int)testCase["row"], result);

            // 断言结果
            Assert.That(result["result"], Is.EqualTo(Constants.TestStatus.PASS), 
                $"用例执行失败: {testCase["name"]}\n" +
                $"请求信息: {testCase["request"]}\n" +
                $"期望结果: {testCase["expected"]}\n" +
                $"实际结果: {result["response"]}");
        }

        [Test, TestCaseSource(nameof(GetYamlTestCases))]
        public void TestYamlApi(Dictionary<string, object> testCase)
        {
            TestContext.WriteLine($"开始执行用例: {testCase["name"]}");

            // 检查是否需要执行
            if (testCase["run"]?.ToString().ToLower() != "yes")
            {
                TestContext.WriteLine($"跳过用例: {testCase["name"]}");
                Assert.Ignore("跳过执行");
                return;
            }

            // 执行测试用例
            var result = _testManager.ExecuteTestCase(testCase);

            // 断言结果
            Assert.That(result["result"], Is.EqualTo(Constants.TestStatus.PASS), 
                $"用例执行失败: {testCase["name"]}\n" +
                $"请求信息: {testCase["request"]}\n" +
                $"期望结果: {testCase["expected"]}\n" +
                $"实际结果: {result["response"]}");
        }

        /// <summary>
        /// 获取Excel测试用例
        /// </summary>
        private static IEnumerable<Dictionary<string, object>> GetExcelTestCases()
        {
            var excelTool = new ExcelTool();
            return excelTool.LoadTestCases();
        }

        /// <summary>
        /// 获取YAML测试用例
        /// </summary>
        private static IEnumerable<Dictionary<string, object>> GetYamlTestCases()
        {
            var yamlTool = new YamlTool(AppSettings.ApiYamlPath);
            return yamlTool.LoadTestCases();
        }
        
        /// <summary>
        /// 手动运行API测试的方法，用于直接从Main方法或其他非NUnit环境调用
        /// </summary>
        /// <param name="testNames">要运行的测试名称数组，为null或空则运行所有测试</param>
        /// <returns>测试结果集合，键为测试名称，值为测试结果信息</returns>
        public Dictionary<string, Program.TestResultInfo> RunTests(params string[] testNames)
        {
            // 测试结果集合
            var testResults = new Dictionary<string, Program.TestResultInfo>();
            
            try
            {
                Console.WriteLine("=== 开始执行API测试 ===");
                LogTool.Log("开始手动执行API测试", Nunit_Cs.Tools.LogLevel.Info);
                
                // 初始化测试环境
                OneTimeSetUp();
                
                // 如果没有指定具体测试，则执行所有测试
                if (testNames == null || testNames.Length == 0)
                {
                    // 执行Excel API测试
                    Console.WriteLine("\n>> 执行Excel API测试");
                    foreach (var testCase in GetExcelTestCases())
                    {
                        string testName = $"Excel_{testCase["name"]}";
                        var resultInfo = new Program.TestResultInfo
                        {
                            Description = $"Excel API测试 - {testCase["name"]}"
                        };
                        
                        try
                        {
                            // 检查是否需要执行
                            if (testCase["run"]?.ToString().ToLower() != "yes")
                            {
                                Console.WriteLine($"[SKIP] 跳过用例: {testCase["name"]}");
                                resultInfo.Status = "SKIP";
                                resultInfo.ErrorMessage = "用例被配置为跳过";
                                testResults[testName] = resultInfo;
                                continue;
                            }
                            
                            var startTime = DateTime.Now;
                            TestExcelApi(testCase);
                            resultInfo.Duration = (long)(DateTime.Now - startTime).TotalMilliseconds;
                            
                            Console.WriteLine($"[PASS] Excel API测试通过: {testCase["name"]}");
                            resultInfo.Status = "PASS";
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[FAIL] Excel API测试失败: {testCase["name"]} - {ex.Message}");
                            resultInfo.Status = "FAIL";
                            resultInfo.ErrorMessage = ex.Message;
                        }
                        finally
                        {
                            testResults[testName] = resultInfo;
                        }
                    }
                    
                    // 执行YAML API测试
                    Console.WriteLine("\n>> 执行YAML API测试");
                    foreach (var testCase in GetYamlTestCases())
                    {
                        string testName = $"YAML_{testCase["name"]}";
                        var resultInfo = new Program.TestResultInfo
                        {
                            Description = $"YAML API测试 - {testCase["name"]}"
                        };
                        
                        try
                        {
                            // 检查是否需要执行
                            if (testCase["run"]?.ToString().ToLower() != "yes")
                            {
                                Console.WriteLine($"[SKIP] 跳过用例: {testCase["name"]}");
                                resultInfo.Status = "SKIP";
                                resultInfo.ErrorMessage = "用例被配置为跳过";
                                testResults[testName] = resultInfo;
                                continue;
                            }
                            
                            var startTime = DateTime.Now;
                            TestYamlApi(testCase);
                            resultInfo.Duration = (long)(DateTime.Now - startTime).TotalMilliseconds;
                            
                            Console.WriteLine($"[PASS] YAML API测试通过: {testCase["name"]}");
                            resultInfo.Status = "PASS";
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[FAIL] YAML API测试失败: {testCase["name"]} - {ex.Message}");
                            resultInfo.Status = "FAIL";
                            resultInfo.ErrorMessage = ex.Message;
                        }
                        finally
                        {
                            testResults[testName] = resultInfo;
                        }
                    }
                }
                else
                {
                    // 根据指定的测试名称执行特定测试
                    foreach (var testName in testNames)
                    {
                        if (testName.Equals("TestExcelApi", StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine("\n>> 执行Excel API测试");
                            foreach (var testCase in GetExcelTestCases())
                            {
                                string fullTestName = $"Excel_{testCase["name"]}";
                                var resultInfo = new Program.TestResultInfo
                                {
                                    Description = $"Excel API测试 - {testCase["name"]}"
                                };
                                
                                try
                                {
                                    // 检查是否需要执行
                                    if (testCase["run"]?.ToString().ToLower() != "yes")
                                    {
                                        Console.WriteLine($"[SKIP] 跳过用例: {testCase["name"]}");
                                        resultInfo.Status = "SKIP";
                                        resultInfo.ErrorMessage = "用例被配置为跳过";
                                        testResults[fullTestName] = resultInfo;
                                        continue;
                                    }
                                    
                                    var startTime = DateTime.Now;
                                    TestExcelApi(testCase);
                                    resultInfo.Duration = (long)(DateTime.Now - startTime).TotalMilliseconds;
                                    
                                    Console.WriteLine($"[PASS] Excel API测试通过: {testCase["name"]}");
                                    resultInfo.Status = "PASS";
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[FAIL] Excel API测试失败: {testCase["name"]} - {ex.Message}");
                                    resultInfo.Status = "FAIL";
                                    resultInfo.ErrorMessage = ex.Message;
                                }
                                finally
                                {
                                    testResults[fullTestName] = resultInfo;
                                }
                            }
                        }
                        else if (testName.Equals("TestYamlApi", StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine("\n>> 执行YAML API测试");
                            foreach (var testCase in GetYamlTestCases())
                            {
                                string fullTestName = $"YAML_{testCase["name"]}";
                                var resultInfo = new Program.TestResultInfo
                                {
                                    Description = $"YAML API测试 - {testCase["name"]}"
                                };
                                
                                try
                                {
                                    // 检查是否需要执行
                                    if (testCase["run"]?.ToString().ToLower() != "yes")
                                    {
                                        Console.WriteLine($"[SKIP] 跳过用例: {testCase["name"]}");
                                        resultInfo.Status = "SKIP";
                                        resultInfo.ErrorMessage = "用例被配置为跳过";
                                        testResults[fullTestName] = resultInfo;
                                        continue;
                                    }
                                    
                                    var startTime = DateTime.Now;
                                    TestYamlApi(testCase);
                                    resultInfo.Duration = (long)(DateTime.Now - startTime).TotalMilliseconds;
                                    
                                    Console.WriteLine($"[PASS] YAML API测试通过: {testCase["name"]}");
                                    resultInfo.Status = "PASS";
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[FAIL] YAML API测试失败: {testCase["name"]} - {ex.Message}");
                                    resultInfo.Status = "FAIL";
                                    resultInfo.ErrorMessage = ex.Message;
                                }
                                finally
                                {
                                    testResults[fullTestName] = resultInfo;
                                }
                            }
                        }
                    }
                }
                
                // 清理测试环境
                OneTimeTearDown();
                Console.WriteLine("=== API测试执行完成 ===");
                LogTool.Log("API测试执行完成", Nunit_Cs.Tools.LogLevel.Success);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] 执行API测试时发生异常: {ex.Message}");
                LogTool.LogException(ex, "执行API测试");
                
                // 添加全局失败记录
                testResults["API测试全局异常"] = new Program.TestResultInfo
                {
                    Status = "FAIL",
                    Description = "执行API测试时发生全局异常",
                    ErrorMessage = ex.Message
                };
            }
            
            return testResults;
        }
    }
} 