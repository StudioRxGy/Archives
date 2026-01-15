using System.Reflection;
using Xunit.Sdk;
using Microsoft.Extensions.Logging;

namespace EnterpriseAutomationFramework.Services.Data;

/// <summary>
/// CSV 数据属性，用于 xUnit Theory 集成
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class CsvDataAttribute : DataAttribute
{
    private readonly string _filePath;
    private readonly bool _hasHeaders;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="filePath">CSV文件路径</param>
    /// <param name="hasHeaders">是否包含标题行</param>
    public CsvDataAttribute(string filePath, bool hasHeaders = true)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _hasHeaders = hasHeaders;
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

        // 获取方法参数类型
        var parameters = testMethod.GetParameters();
        
        if (parameters.Length == 0)
        {
            throw new InvalidOperationException("测试方法必须至少有一个参数");
        }

        var csvReader = new CsvDataReader();
        
        try
        {
            // 如果只有一个参数且是强类型，尝试强类型读取
            if (parameters.Length == 1 && parameters[0].ParameterType != typeof(Dictionary<string, object>))
            {
                var parameterType = parameters[0].ParameterType;
                
                // 使用反射调用泛型方法
                var method = typeof(CsvDataReader).GetMethod(nameof(CsvDataReader.ReadData));
                var genericMethod = method?.MakeGenericMethod(parameterType);
                
                if (genericMethod != null)
                {
                    var result = genericMethod.Invoke(csvReader, new object[] { _filePath });
                    
                    if (result is IEnumerable<object> enumerable)
                    {
                        return enumerable.Select(item => new object[] { item });
                    }
                }
            }

            // 默认使用动态数据读取
            return csvReader.ReadDynamicData(_filePath)
                .Select(row => new object[] { row });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"读取CSV测试数据失败: {_filePath}", ex);
        }
    }
}