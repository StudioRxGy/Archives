namespace EnterpriseAutomationFramework.Services.Data;

/// <summary>
/// 页面元素集合
/// </summary>
public class PageElementCollection
{
    /// <summary>
    /// 页面元素字典，键为页面名称，值为该页面的元素字典
    /// </summary>
    private readonly Dictionary<string, Dictionary<string, PageElement>> _elements = new();

    /// <summary>
    /// 添加页面元素
    /// </summary>
    /// <param name="pageName">页面名称</param>
    /// <param name="elementName">元素名称</param>
    /// <param name="element">页面元素</param>
    public void AddElement(string pageName, string elementName, PageElement element)
    {
        if (string.IsNullOrWhiteSpace(pageName))
            throw new ArgumentException("页面名称不能为空", nameof(pageName));
        
        if (string.IsNullOrWhiteSpace(elementName))
            throw new ArgumentException("元素名称不能为空", nameof(elementName));
        
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        if (!_elements.ContainsKey(pageName))
        {
            _elements[pageName] = new Dictionary<string, PageElement>();
        }

        element.Name = elementName;
        _elements[pageName][elementName] = element;
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

        if (!_elements.ContainsKey(pageName))
            throw new KeyNotFoundException($"页面 '{pageName}' 不存在");

        if (!_elements[pageName].ContainsKey(elementName))
            throw new KeyNotFoundException($"页面 '{pageName}' 中的元素 '{elementName}' 不存在");

        return _elements[pageName][elementName];
    }

    /// <summary>
    /// 获取指定页面的所有元素
    /// </summary>
    /// <param name="pageName">页面名称</param>
    /// <returns>页面元素字典</returns>
    /// <exception cref="ArgumentException">页面名称为空时抛出</exception>
    /// <exception cref="KeyNotFoundException">页面不存在时抛出</exception>
    public Dictionary<string, PageElement> GetPageElements(string pageName)
    {
        if (string.IsNullOrWhiteSpace(pageName))
            throw new ArgumentException("页面名称不能为空", nameof(pageName));

        if (!_elements.ContainsKey(pageName))
            throw new KeyNotFoundException($"页面 '{pageName}' 不存在");

        return new Dictionary<string, PageElement>(_elements[pageName]);
    }

    /// <summary>
    /// 检查页面是否存在
    /// </summary>
    /// <param name="pageName">页面名称</param>
    /// <returns>页面是否存在</returns>
    public bool ContainsPage(string pageName)
    {
        return !string.IsNullOrWhiteSpace(pageName) && _elements.ContainsKey(pageName);
    }

    /// <summary>
    /// 检查元素是否存在
    /// </summary>
    /// <param name="pageName">页面名称</param>
    /// <param name="elementName">元素名称</param>
    /// <returns>元素是否存在</returns>
    public bool ContainsElement(string pageName, string elementName)
    {
        return ContainsPage(pageName) && 
               !string.IsNullOrWhiteSpace(elementName) && 
               _elements[pageName].ContainsKey(elementName);
    }

    /// <summary>
    /// 获取所有页面名称
    /// </summary>
    /// <returns>页面名称集合</returns>
    public IEnumerable<string> GetPageNames()
    {
        return _elements.Keys.ToList();
    }

    /// <summary>
    /// 获取指定页面的所有元素名称
    /// </summary>
    /// <param name="pageName">页面名称</param>
    /// <returns>元素名称集合</returns>
    /// <exception cref="ArgumentException">页面名称为空时抛出</exception>
    /// <exception cref="KeyNotFoundException">页面不存在时抛出</exception>
    public IEnumerable<string> GetElementNames(string pageName)
    {
        if (string.IsNullOrWhiteSpace(pageName))
            throw new ArgumentException("页面名称不能为空", nameof(pageName));

        if (!_elements.ContainsKey(pageName))
            throw new KeyNotFoundException($"页面 '{pageName}' 不存在");

        return _elements[pageName].Keys.ToList();
    }

    /// <summary>
    /// 清空所有元素
    /// </summary>
    public void Clear()
    {
        _elements.Clear();
    }

    /// <summary>
    /// 获取元素总数
    /// </summary>
    /// <returns>元素总数</returns>
    public int Count => _elements.Values.Sum(page => page.Count);
}