using System.Reflection;
using System.Text.Json;
using Xunit.Sdk;

namespace EnterpriseAutomationFramework.Services.Data;

/// <summary>
/// JSON 数据属性，用于 xUnit Theory 集成
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class JsonDataAttribute : DataAttribute
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="filePath">JSON文件路径</param>
    public JsonDataAttribute(string filePath)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
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
                throw new FileNotFoundException($"JSON测试数据文件不存在: {_filePath}");
            }

            var jsonContent = File.ReadAllText(_filePath);
            
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                throw new InvalidOperationException($"JSON文件为空: {_filePath}");
            }

            // 如果只有一个参数且是强类型，尝试强类型反序列化
            if (parameters.Length == 1 && parameters[0].ParameterType != typeof(Dictionary<string, object>))
            {
                var parameterType = parameters[0].ParameterType;
                
                // 尝试反序列化为数组
                var arrayType = parameterType.MakeArrayType();
                var result = JsonSerializer.Deserialize(jsonContent, arrayType, _jsonOptions);
                
                if (result is Array array)
                {
                    return array.Cast<object>().Select(item => new object[] { item });
                }
                
                // 如果不是数组，尝试反序列化为单个对象
                var singleResult = JsonSerializer.Deserialize(jsonContent, parameterType, _jsonOptions);
                if (singleResult != null)
                {
                    return new[] { new object[] { singleResult } };
                }
            }

            // 默认反序列化为字典数组
            using var document = JsonDocument.Parse(jsonContent);
            var root = document.RootElement;
            
            if (root.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException($"JSON数据必须是数组格式: {_filePath}");
            }

            var dictionaries = new List<Dictionary<string, object>>();
            
            foreach (var element in root.EnumerateArray())
            {
                var dict = ConvertJsonElementToDictionary(element);
                dictionaries.Add(dict);
            }

            return dictionaries.Select(dict => new object[] { dict });
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"JSON格式错误: {_filePath}", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"读取JSON测试数据失败: {_filePath}", ex);
        }
    }

    /// <summary>
    /// 将JsonElement转换为Dictionary
    /// </summary>
    /// <param name="element">JSON元素</param>
    /// <returns>字典对象</returns>
    private static Dictionary<string, object> ConvertJsonElementToDictionary(JsonElement element)
    {
        var dict = new Dictionary<string, object>();
        
        foreach (var property in element.EnumerateObject())
        {
            dict[property.Name] = ConvertJsonElementToObject(property.Value);
        }
        
        return dict;
    }

    /// <summary>
    /// 将JsonElement转换为对象
    /// </summary>
    /// <param name="element">JSON元素</param>
    /// <returns>对象</returns>
    private static object ConvertJsonElementToObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString()!,
            JsonValueKind.Number => element.TryGetInt32(out var intValue) ? intValue : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null!,
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElementToObject).ToArray(),
            JsonValueKind.Object => ConvertJsonElementToDictionary(element),
            _ => element.ToString()
        };
    }
}