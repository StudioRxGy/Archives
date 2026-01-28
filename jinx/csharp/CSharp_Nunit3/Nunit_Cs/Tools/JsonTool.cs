using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Nunit_Cs.Config;
using NUnit.Framework;

namespace Nunit_Cs.Tools
{
    /// <summary>
    /// JSON数据处理工具
    /// </summary>
    public class JsonTool
    {
        private readonly string _filePath;

        public JsonTool(string filePath = null)
        {
            _filePath = filePath ?? AppSettings.ApiJsonPath;
        }

        /// <summary>
        /// 从JSON文件加载测试用例
        /// </summary>
        /// <returns>测试用例列表</returns>
        public List<Dictionary<string, object>> LoadTestCases()
        {
            try
            {
                var testCases = new List<Dictionary<string, object>>();
                
                if (!File.Exists(_filePath))
                {
                    TestContext.WriteLine($"JSON文件不存在: {_filePath}");
                    return testCases;
                }

                // 读取JSON文件内容
                var jsonContent = File.ReadAllText(_filePath);
                
                // 反序列化为对象
                var cases = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(jsonContent);
                if (cases != null)
                {
                    testCases.AddRange(cases);
                }

                return testCases;
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"加载JSON测试用例异常: {ex.Message}");
                return new List<Dictionary<string, object>>();
            }
        }

        /// <summary>
        /// 保存测试用例到JSON文件
        /// </summary>
        /// <param name="testCases">测试用例列表</param>
        public void SaveTestCases(List<Dictionary<string, object>> testCases)
        {
            try
            {
                // 确保目录存在
                var directory = Path.GetDirectoryName(_filePath);
                if (!Directory.Exists(directory) && directory != null)
                {
                    Directory.CreateDirectory(directory);
                }

                // 序列化为JSON
                var jsonContent = JsonConvert.SerializeObject(testCases, Formatting.Indented);
                
                // 写入文件
                File.WriteAllText(_filePath, jsonContent);
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"保存JSON测试用例异常: {ex.Message}");
            }
        }
    }
} 