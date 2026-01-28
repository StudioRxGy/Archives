using System.Collections;
using System.Reflection;
using System.Text.Json;
using Xunit.Sdk;

namespace CsPlaywrightXun.src.playwright.Services.Data;

/// <summary>
/// JSON 数据属性，用于从 JSON 文件加载测试数据
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class JsonDataAttribute : DataAttribute
{
    private readonly string _filePath;
    private readonly string? _propertyName;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="filePath">JSON 文件路径</param>
    /// <param name="propertyName">JSON 属性名称（可选，用于从复杂 JSON 中提取特定属性）</param>
    public JsonDataAttribute(string filePath, string? propertyName = null)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _propertyName = propertyName;
    }

    /// <summary>
    /// 获取测试数据
    /// </summary>
    /// <param name="testMethod">测试方法</param>
    /// <returns>测试数据集合</returns>
    public override IEnumerable<object[]> GetData(MethodInfo testMethod)
    {
        if (testMethod == null)
            throw new ArgumentNullException(nameof(testMethod));

        // 获取文件的完整路径
        var fullPath = GetFullPath(_filePath);
        
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"JSON 数据文件不存在: {fullPath}");

        try
        {
            // 读取 JSON 文件内容
            var jsonContent = File.ReadAllText(fullPath);
            
            if (string.IsNullOrWhiteSpace(jsonContent))
                throw new InvalidOperationException($"JSON 文件内容为空: {fullPath}");

            // 解析 JSON 内容
            using var document = JsonDocument.Parse(jsonContent);
            var rootElement = document.RootElement;

            // 如果指定了属性名称，则提取该属性
            if (!string.IsNullOrEmpty(_propertyName))
            {
                if (!rootElement.TryGetProperty(_propertyName, out var propertyElement))
                    throw new InvalidOperationException($"JSON 文件中不存在属性: {_propertyName}");
                rootElement = propertyElement;
            }

            // 根据 JSON 结构返回数据
            if (rootElement.ValueKind == JsonValueKind.Array)
            {
                return GetDataFromArray(rootElement, testMethod);
            }
            else if (rootElement.ValueKind == JsonValueKind.Object)
            {
                return GetDataFromObject(rootElement, testMethod);
            }
            else
            {
                throw new InvalidOperationException($"不支持的 JSON 根元素类型: {rootElement.ValueKind}");
            }
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"解析 JSON 文件失败: {fullPath}", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"读取 JSON 数据文件时发生错误: {fullPath}", ex);
        }
    }

    /// <summary>
    /// 从 JSON 数组中获取数据
    /// </summary>
    /// <param name="arrayElement">JSON 数组元素</param>
    /// <param name="testMethod">测试方法</param>
    /// <returns>测试数据集合</returns>
    private IEnumerable<object[]> GetDataFromArray(JsonElement arrayElement, MethodInfo testMethod)
    {
        var parameters = testMethod.GetParameters();
        
        foreach (var item in arrayElement.EnumerateArray())
        {
            if (parameters.Length == 1)
            {
                // 单参数：直接传递整个对象
                var data = ConvertJsonElementToObject(item, parameters[0].ParameterType);
                yield return new object[] { data };
            }
            else
            {
                // 多参数：从对象属性中提取值
                var values = new object[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    var paramName = parameters[i].Name;
                    if (item.TryGetProperty(paramName!, out var propertyElement))
                    {
                        values[i] = ConvertJsonElementToObject(propertyElement, parameters[i].ParameterType);
                    }
                    else
                    {
                        values[i] = GetDefaultValue(parameters[i].ParameterType);
                    }
                }
                yield return values;
            }
        }
    }

    /// <summary>
    /// 从 JSON 对象中获取数据
    /// </summary>
    /// <param name="objectElement">JSON 对象元素</param>
    /// <param name="testMethod">测试方法</param>
    /// <returns>测试数据集合</returns>
    private IEnumerable<object[]> GetDataFromObject(JsonElement objectElement, MethodInfo testMethod)
    {
        var parameters = testMethod.GetParameters();
        
        if (parameters.Length == 1)
        {
            // 单参数：直接传递整个对象
            var data = ConvertJsonElementToObject(objectElement, parameters[0].ParameterType);
            yield return new object[] { data };
        }
        else
        {
            // 多参数：从对象属性中提取值
            var values = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                var paramName = parameters[i].Name;
                if (objectElement.TryGetProperty(paramName!, out var propertyElement))
                {
                    values[i] = ConvertJsonElementToObject(propertyElement, parameters[i].ParameterType);
                }
                else
                {
                    values[i] = GetDefaultValue(parameters[i].ParameterType);
                }
            }
            yield return values;
        }
    }

    /// <summary>
    /// 将 JSON 元素转换为指定类型的对象
    /// </summary>
    /// <param name="element">JSON 元素</param>
    /// <param name="targetType">目标类型</param>
    /// <returns>转换后的对象</returns>
    private object ConvertJsonElementToObject(JsonElement element, Type targetType)
    {
        try
        {
            // 处理可空类型
            var actualType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            // 如果元素为 null 且类型可空，返回 null
            if (element.ValueKind == JsonValueKind.Null)
            {
                if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
                    throw new InvalidOperationException($"无法将 null 值转换为非可空类型: {targetType.Name}");
                return null!;
            }

            // 基本类型转换
            if (actualType == typeof(string))
                return element.GetString() ?? string.Empty;
            
            if (actualType == typeof(int))
                return element.GetInt32();
            
            if (actualType == typeof(long))
                return element.GetInt64();
            
            if (actualType == typeof(double))
                return element.GetDouble();
            
            if (actualType == typeof(float))
                return element.GetSingle();
            
            if (actualType == typeof(bool))
                return element.GetBoolean();
            
            if (actualType == typeof(DateTime))
                return element.GetDateTime();
            
            if (actualType == typeof(Guid))
                return element.GetGuid();

            // 字典类型
            if (actualType.IsGenericType && actualType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                var keyType = actualType.GetGenericArguments()[0];
                var valueType = actualType.GetGenericArguments()[1];
                
                if (keyType == typeof(string))
                {
                    var dictionary = (IDictionary)Activator.CreateInstance(actualType)!;
                    foreach (var property in element.EnumerateObject())
                    {
                        var value = ConvertJsonElementToObject(property.Value, valueType);
                        dictionary.Add(property.Name, value);
                    }
                    return dictionary;
                }
            }

            // 复杂对象类型：使用 JsonSerializer 反序列化
            var json = element.GetRawText();
            return JsonSerializer.Deserialize(json, targetType, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            })!;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"无法将 JSON 元素转换为类型 {targetType.Name}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 获取类型的默认值
    /// </summary>
    /// <param name="type">类型</param>
    /// <returns>默认值</returns>
    private static object GetDefaultValue(Type type)
    {
        if (type.IsValueType)
        {
            return Activator.CreateInstance(type)!;
        }
        return null!;
    }

    /// <summary>
    /// 获取文件的完整路径
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>完整路径</returns>
    private static string GetFullPath(string filePath)
    {
        // 如果是绝对路径，直接返回
        if (Path.IsPathRooted(filePath))
            return filePath;

        // 相对路径：相对于当前工作目录或项目根目录
        var currentDirectory = Directory.GetCurrentDirectory();
        var fullPath = Path.Combine(currentDirectory, filePath);
        
        if (File.Exists(fullPath))
            return fullPath;

        // 尝试相对于项目根目录（查找包含 .csproj 文件的目录）
        var projectRoot = FindProjectRoot(currentDirectory);
        if (projectRoot != null)
        {
            fullPath = Path.Combine(projectRoot, filePath);
            if (File.Exists(fullPath))
                return fullPath;
        }

        // 如果都找不到，返回原始的完整路径（让后续的文件存在检查处理）
        return Path.Combine(currentDirectory, filePath);
    }

    /// <summary>
    /// 查找项目根目录
    /// </summary>
    /// <param name="startDirectory">开始搜索的目录</param>
    /// <returns>项目根目录路径，如果未找到则返回 null</returns>
    private static string? FindProjectRoot(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);
        
        while (directory != null)
        {
            // 查找 .csproj 文件
            if (directory.GetFiles("*.csproj").Any())
                return directory.FullName;
            
            // 查找 .sln 文件
            if (directory.GetFiles("*.sln").Any())
                return directory.FullName;
            
            directory = directory.Parent;
        }
        
        return null;
    }
}