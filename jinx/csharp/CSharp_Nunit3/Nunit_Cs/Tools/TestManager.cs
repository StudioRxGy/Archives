using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Nunit_Cs.Common;
using NUnit.Framework;

namespace Nunit_Cs.Tools
{
    /// <summary>
    /// 测试用例管理工具
    /// </summary>
    public class TestManager
    {
        private readonly RequestTool _requestTool;
        private readonly ResponseTool _responseTool;

        public TestManager()
        {
            _requestTool = new RequestTool();
            _responseTool = new ResponseTool();
        }

        /// <summary>
        /// 创建测试结果对象
        /// </summary>
        /// <param name="name">测试用例名称</param>
        /// <param name="status">测试状态</param>
        /// <param name="response">测试响应</param>
        /// <returns>测试结果对象</returns>
        public Dictionary<string, object> CreateResult(
            string name,
            string status = Constants.TestStatus.SKIP,
            object response = null)
        {
            return new Dictionary<string, object>
            {
                ["name"] = name,
                ["result"] = status,
                ["response"] = response
            };
        }

        /// <summary>
        /// 执行API测试用例
        /// </summary>
        /// <param name="testCase">测试用例</param>
        /// <returns>测试结果</returns>
        public Dictionary<string, object> ExecuteTestCase(Dictionary<string, object> testCase)
        {
            try
            {
                // 获取请求数据
                var name = testCase["name"].ToString();
                var request = testCase["request"] as Dictionary<string, object>;
                var expected = testCase["expected"] as Dictionary<string, object>;
                var dataType = testCase.ContainsKey("type") ? testCase["type"].ToString() : Constants.DataTypes.JSON;

                // 构建请求数据
                var url = request["url"].ToString();
                var method = request["method"].ToString();
                var data = request["data"];
                var headers = request["headers"] as Dictionary<string, string>;

                // 发送请求并获取响应
                var httpResponse = _requestTool.SendRequest(url, method, data, headers, dataType);
                var response = _responseTool.ProcessResponse(httpResponse);

                // 验证结果
                bool statusCodeMatch = int.Parse(expected["status_code"].ToString()) == response.code;
                bool msgMatch = expected["msg"].ToString() == response.body.msg.ToString();
                bool dataMatch = JsonConvert.SerializeObject(response.body).Contains(expected["data"].ToString());

                var status = (statusCodeMatch && msgMatch && dataMatch) ? Constants.TestStatus.PASS : Constants.TestStatus.FAIL;
                var result = CreateResult(name, status, response);

                TestContext.WriteLine($"测试用例执行完成: {name}, 结果: {status}");
                return result;
            }
            catch (Exception ex)
            {
                var errorMsg = $"执行测试用例异常: {ex.Message}";
                TestContext.WriteLine(errorMsg);
                return CreateResult(testCase["name"].ToString(), Constants.TestStatus.FAIL, errorMsg);
            }
        }
    }
} 