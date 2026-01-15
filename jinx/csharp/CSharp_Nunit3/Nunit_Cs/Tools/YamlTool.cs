using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Nunit_Cs.Config;
using NUnit.Framework;

namespace Nunit_Cs.Tools
{
    /// <summary>
    /// YAML数据处理工具
    /// </summary>
    public class YamlTool
    {
        private readonly string _filePath;

        public YamlTool(string filePath = null)
        {
            _filePath = filePath ?? AppSettings.ApiYamlPath;
        }

        /// <summary>
        /// 从YAML文件加载测试用例
        /// </summary>
        /// <returns>测试用例列表</returns>
        public List<Dictionary<string, object>> LoadTestCases()
        {
            try
            {
                var testCases = new List<Dictionary<string, object>>();
                
                if (!File.Exists(_filePath))
                {
                    TestContext.WriteLine($"YAML文件不存在: {_filePath}");
                    return testCases;
                }

                // 读取YAML文件内容
                var yamlContent = File.ReadAllText(_filePath);
                
                // 设置反序列化器
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();
                
                // 反序列化为对象
                var yamlObject = deserializer.Deserialize<Dictionary<string, List<Dictionary<string, object>>>>(yamlContent);
                
                // 从YAML结构中提取测试用例
                if (yamlObject.ContainsKey("Case"))
                {
                    foreach (var testContainer in yamlObject["Case"])
                    {
                        if (testContainer.ContainsKey("Test") && testContainer["Test"] is Dictionary<string, object> testCase)
                        {
                            testCases.Add(testCase);
                        }
                    }
                }
                
                return testCases;
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"加载YAML测试用例异常: {ex.Message}");
                return new List<Dictionary<string, object>>();
            }
        }

        /// <summary>
        /// 加载YAML元素定位数据
        /// </summary>
        /// <returns>元素定位数据</returns>
        public Dictionary<string, string> LoadElementLocators()
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    TestContext.WriteLine($"YAML文件不存在: {_filePath}");
                    return new Dictionary<string, string>();
                }

                // 读取YAML文件内容
                var yamlContent = File.ReadAllText(_filePath);
                
                // 设置反序列化器
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();
                
                // 反序列化为元素定位字典
                return deserializer.Deserialize<Dictionary<string, string>>(yamlContent);
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"加载YAML元素定位数据异常: {ex.Message}");
                return new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// 保存测试用例到YAML文件
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

                // 构建YAML结构
                var yamlObject = new Dictionary<string, List<Dictionary<string, object>>>
                {
                    ["Case"] = new List<Dictionary<string, object>>()
                };
                
                foreach (var testCase in testCases)
                {
                    yamlObject["Case"].Add(new Dictionary<string, object> { ["Test"] = testCase });
                }
                
                // 设置序列化器
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();
                
                // 序列化为YAML
                var yamlContent = serializer.Serialize(yamlObject);
                
                // 写入文件
                File.WriteAllText(_filePath, yamlContent);
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"保存YAML测试用例异常: {ex.Message}");
            }
        }
    }
} 