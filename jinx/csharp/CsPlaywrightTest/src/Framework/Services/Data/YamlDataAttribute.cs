using System.Reflection;
using Xunit.Sdk;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace EnterpriseAutomationFramework.Services.Data;

/// <summary>
/// YAML 数据属性，用于 xUnit Theory 集成
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class YamlDataAttribute : DataAttribute
{
    private readonly string _filePath;
    private readonly IDeserializer _deserializer;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="filePath">YAML文件路径</param>
    public YamlDataAttribute(string filePath)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <summary>
    /// 获取测试数据
    /// </summary>
    /// <param name="testMethod">测试方法</param>
    /// <returns>测试数据集合</returns>
    public override IEnumerable<object[]> GetData(MethodInfo testMethod)
    {
        if (testMethod == null)
        {
            throw new ArgumentNullException(nameof(testMethod));
        }

        var parameters = testMethod.GetParameters();
        
        if (parameters.Length == 0)
        {
            throw new InvalidOperationException("测试方法必须至少有一个参数");
        }

        try
        {
            if (!File.Exists(_filePath))
            {
                throw new FileNotFoundException($"YAML测试数据文件不存在: {_filePath}");
            }

            var yamlContent = File.ReadAllText(_filePath);
            
            if (string.IsNullOrWhiteSpace(yamlContent))
            {
                throw new InvalidOperationException($"YAML文件为空: {_filePath}");
            }

            // 如果只有一个参数且是强类型，尝试强类型反序列化
            if (parameters.Length == 1 && parameters[0].ParameterType != typeof(Dictionary<string, object>))
            {
                var parameterType = parameters[0].ParameterType;
                
                // 尝试反序列化为数组
                var arrayType = parameterType.MakeArrayType();
                var result = _deserializer.Deserialize(yamlContent, arrayType);
                
                if (result is Array array)
                {
                    return array.Cast<object>().Select(item => new object[] { item });
                }
                
                // 如果不是数组，尝试反序列化为单个对象
                var singleResult = _deserializer.Deserialize(yamlContent, parameterType);
                if (singleResult != null)
                {
                    return new[] { new object[] { singleResult } };
                }
            }

            // 默认反序列化为字典数组
            var dictionaries = _deserializer.Deserialize<Dictionary<string, object>[]>(yamlContent);
            
            if (dictionaries == null)
            {
                throw new InvalidOperationException($"无法解析YAML数据: {_filePath}");
            }

            return dictionaries.Select(dict => new object[] { dict });
        }
        catch (FileNotFoundException ex)
        {
            throw new InvalidOperationException($"读取YAML测试数据失败: {_filePath}", ex);
        }
        catch (Exception ex) when (!(ex is ArgumentNullException || ex is InvalidOperationException))
        {
            throw new InvalidOperationException($"读取YAML测试数据失败: {_filePath}", ex);
        }
    }
}