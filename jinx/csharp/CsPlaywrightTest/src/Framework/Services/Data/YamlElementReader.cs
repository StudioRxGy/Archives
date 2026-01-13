using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using EnterpriseAutomationFramework.Core.Exceptions;

namespace EnterpriseAutomationFramework.Services.Data;

/// <summary>
/// YAML 元素读取器
/// </summary>
public class YamlElementReader
{
    private readonly ILogger<YamlElementReader> _logger;
    private readonly IDeserializer _deserializer;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public YamlElementReader(ILogger<YamlElementReader>? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<YamlElementReader>.Instance;
        
        // 配置YAML反序列化器
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <summary>
    /// 加载页面元素集合
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>页面元素集合</returns>
    /// <exception cref="ArgumentException">文件路径为空或无效时抛出</exception>
    /// <exception cref="FileNotFoundException">文件不存在时抛出</exception>
    /// <exception cref="YamlDataException">YAML数据格式错误时抛出</exception>
    public PageElementCollection LoadElements(string filePath)
    {
        ValidateFilePath(filePath);

        try
        {
            _logger.LogInformation("开始加载YAML元素文件: {FilePath}", filePath);

            var yamlContent = File.ReadAllText(filePath);
            
            if (string.IsNullOrWhiteSpace(yamlContent))
            {
                throw new YamlDataException(filePath, "YAML文件内容为空");
            }

            // 反序列化YAML内容为字典结构
            var yamlData = _deserializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(yamlContent);
            
            if (yamlData == null)
            {
                throw new YamlDataException(filePath, "YAML文件格式无效");
            }

            var elementCollection = new PageElementCollection();
            
            // 遍历每个页面
            foreach (var page in yamlData)
            {
                var pageName = page.Key;
                var pageElements = page.Value;
                
                _logger.LogDebug("处理页面: {PageName}", pageName);
                
                // 遍历页面中的每个元素
                foreach (var elementEntry in pageElements)
                {
                    var elementName = elementEntry.Key;
                    var elementData = elementEntry.Value;
                    
                    try
                    {
                        var element = ParseElement(elementName, elementData);
                        elementCollection.AddElement(pageName, elementName, element);
                        
                        _logger.LogDebug("成功解析元素: {PageName}.{ElementName}", pageName, elementName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "解析元素失败: {PageName}.{ElementName}", pageName, elementName);
                        throw new YamlDataException(filePath, $"解析元素 '{pageName}.{elementName}' 失败: {ex.Message}", ex);
                    }
                }
            }

            _logger.LogInformation("成功加载 {PageCount} 个页面，共 {ElementCount} 个元素", 
                elementCollection.GetPageNames().Count(), elementCollection.Count);
            
            return elementCollection;
        }
        catch (Exception ex) when (!(ex is ArgumentException || ex is FileNotFoundException || ex is YamlDataException))
        {
            _logger.LogError(ex, "加载YAML元素文件失败: {FilePath}", filePath);
            throw new YamlDataException(filePath, $"加载YAML元素文件失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 获取指定页面的元素
    /// </summary>
    /// <param name="pageName">页面名称</param>
    /// <param name="elementName">元素名称</param>
    /// <returns>页面元素</returns>
    /// <exception cref="ArgumentException">页面名称或元素名称为空时抛出</exception>
    /// <exception cref="KeyNotFoundException">页面或元素不存在时抛出</exception>
    public PageElement GetElement(string pageName, string elementName)
    {
        if (string.IsNullOrWhiteSpace(pageName))
            throw new ArgumentException("页面名称不能为空", nameof(pageName));
        
        if (string.IsNullOrWhiteSpace(elementName))
            throw new ArgumentException("元素名称不能为空", nameof(elementName));

        // 这个方法需要先加载元素集合，但为了保持接口一致性，我们抛出异常提示用户先调用LoadElements
        throw new InvalidOperationException("请先调用 LoadElements 方法加载元素集合，然后使用返回的 PageElementCollection 获取元素");
    }

    /// <summary>
    /// 从单个YAML文件加载并获取指定元素
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="pageName">页面名称</param>
    /// <param name="elementName">元素名称</param>
    /// <returns>页面元素</returns>
    /// <exception cref="ArgumentException">参数为空或无效时抛出</exception>
    /// <exception cref="FileNotFoundException">文件不存在时抛出</exception>
    /// <exception cref="YamlDataException">YAML数据格式错误时抛出</exception>
    /// <exception cref="KeyNotFoundException">页面或元素不存在时抛出</exception>
    public PageElement GetElementFromFile(string filePath, string pageName, string elementName)
    {
        var elementCollection = LoadElements(filePath);
        return elementCollection.GetElement(pageName, elementName);
    }

    /// <summary>
    /// 验证文件路径
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <exception cref="ArgumentException">文件路径为空或无效时抛出</exception>
    /// <exception cref="FileNotFoundException">文件不存在时抛出</exception>
    private static void ValidateFilePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("文件路径不能为空", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"YAML文件不存在: {filePath}");
        }
    }

    /// <summary>
    /// 解析单个元素
    /// </summary>
    /// <param name="elementName">元素名称</param>
    /// <param name="elementData">元素数据</param>
    /// <returns>页面元素</returns>
    /// <exception cref="YamlDataException">元素数据格式错误时抛出</exception>
    private PageElement ParseElement(string elementName, object elementData)
    {
        if (elementData == null)
        {
            throw new YamlDataException($"元素 '{elementName}' 的数据为空");
        }

        // 将元素数据转换为字典
        Dictionary<string, object> elementDict;
        
        if (elementData is Dictionary<object, object> objDict)
        {
            // 转换键为字符串
            elementDict = objDict.ToDictionary(
                kvp => kvp.Key?.ToString() ?? string.Empty,
                kvp => kvp.Value ?? string.Empty
            );
        }
        else if (elementData is Dictionary<string, object> strDict)
        {
            elementDict = strDict;
        }
        else
        {
            throw new YamlDataException($"元素 '{elementName}' 的数据格式无效，期望字典类型");
        }

        var element = new PageElement
        {
            Name = elementName,
            Type = ElementType.Text // 设置默认类型
        };

        // 解析选择器（必需）
        if (!elementDict.TryGetValue("selector", out var selectorObj) || selectorObj == null)
        {
            throw new YamlDataException($"元素 '{elementName}' 缺少必需的 'selector' 属性");
        }
        element.Selector = selectorObj.ToString() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(element.Selector))
        {
            throw new YamlDataException($"元素 '{elementName}' 的 'selector' 属性不能为空");
        }

        // 解析元素类型（可选，默认为Text）
        if (elementDict.TryGetValue("type", out var typeObj) && typeObj != null)
        {
            var typeString = typeObj.ToString();
            if (!string.IsNullOrWhiteSpace(typeString))
            {
                if (Enum.TryParse<ElementType>(typeString, true, out var elementType))
                {
                    element.Type = elementType;
                }
                else
                {
                    _logger.LogWarning("元素 '{ElementName}' 的类型 '{Type}' 无效，使用默认类型 Text", elementName, typeString);
                    element.Type = ElementType.Text;
                }
            }
        }

        // 解析超时时间（可选，默认为30000毫秒）
        if (elementDict.TryGetValue("timeout", out var timeoutObj) && timeoutObj != null)
        {
            if (int.TryParse(timeoutObj.ToString(), out var timeout) && timeout > 0)
            {
                element.TimeoutMs = timeout;
            }
            else
            {
                _logger.LogWarning("元素 '{ElementName}' 的超时时间 '{Timeout}' 无效，使用默认值 30000", elementName, timeoutObj);
            }
        }

        // 解析属性（可选）
        if (elementDict.TryGetValue("attributes", out var attributesObj) && attributesObj != null)
        {
            if (attributesObj is Dictionary<object, object> attrObjDict)
            {
                element.Attributes = attrObjDict.ToDictionary(
                    kvp => kvp.Key?.ToString() ?? string.Empty,
                    kvp => kvp.Value?.ToString() ?? string.Empty
                );
            }
            else if (attributesObj is Dictionary<string, object> attrStrDict)
            {
                element.Attributes = attrStrDict.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.ToString() ?? string.Empty
                );
            }
            else
            {
                _logger.LogWarning("元素 '{ElementName}' 的属性格式无效，忽略属性设置", elementName);
            }
        }

        return element;
    }
}