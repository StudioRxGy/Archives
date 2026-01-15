using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using CsPlaywrightXun.src.playwright.Core.Exceptions;
using CsPlaywrightXun.src.playwright.Core.Configuration;

namespace CsPlaywrightXun.src.playwright.Services.Data;

/// <summary>
/// CSV 数据读取器
/// </summary>
public class CsvDataReader
{
    private readonly ILogger<CsvDataReader> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public CsvDataReader(ILogger<CsvDataReader>? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<CsvDataReader>.Instance;
    }

    /// <summary>
    /// 读取强类型数据
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="fileName">文件名（不含路径）</param>
    /// <param name="subDirectory">子目录（可选）</param>
    /// <returns>数据集合</returns>
    /// <exception cref="ArgumentException">文件名为空或无效时抛出</exception>
    /// <exception cref="FileNotFoundException">文件不存在时抛出</exception>
    /// <exception cref="CsvDataException">CSV数据格式错误时抛出</exception>
    public IEnumerable<T> ReadData<T>(string fileName, string? subDirectory = null) where T : class, new()
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("文件名不能为空", nameof(fileName));
        }

        var filePath = PathConfiguration.GetTestDataPath(fileName, subDirectory);
        return ReadDataFromPath<T>(filePath);
    }

    /// <summary>
    /// 从完整路径读取强类型数据
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="filePath">文件完整路径</param>
    /// <returns>数据集合</returns>
    /// <exception cref="ArgumentException">文件路径为空或无效时抛出</exception>
    /// <exception cref="FileNotFoundException">文件不存在时抛出</exception>
    /// <exception cref="CsvDataException">CSV数据格式错误时抛出</exception>
    public IEnumerable<T> ReadDataFromPath<T>(string filePath) where T : class, new()
    {
        ValidateFilePath(filePath);

        try
        {
            _logger.LogInformation("开始读取CSV文件: {FilePath}", filePath);

            using var reader = new StringReader(File.ReadAllText(filePath));
            using var csv = new CsvReader(reader, GetCsvConfiguration());

            var records = csv.GetRecords<T>().ToList();
            
            _logger.LogInformation("成功读取 {Count} 条记录", records.Count);
            
            return records;
        }
        catch (Exception ex) when (!(ex is ArgumentException || ex is FileNotFoundException))
        {
            _logger.LogError(ex, "读取CSV文件失败: {FilePath}", filePath);
            throw new CsvDataException($"读取CSV文件失败: {filePath}", ex);
        }
    }

    /// <summary>
    /// 读取动态数据
    /// </summary>
    /// <param name="fileName">文件名（不含路径）</param>
    /// <param name="subDirectory">子目录（可选）</param>
    /// <returns>动态数据集合</returns>
    /// <exception cref="ArgumentException">文件名为空或无效时抛出</exception>
    /// <exception cref="FileNotFoundException">文件不存在时抛出</exception>
    /// <exception cref="CsvDataException">CSV数据格式错误时抛出</exception>
    public IEnumerable<Dictionary<string, object>> ReadDynamicData(string fileName, string? subDirectory = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("文件名不能为空", nameof(fileName));
        }

        var filePath = PathConfiguration.GetTestDataPath(fileName, subDirectory);
        return ReadDynamicDataFromPath(filePath);
    }

    /// <summary>
    /// 从完整路径读取动态数据
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>动态数据集合</returns>
    /// <exception cref="ArgumentException">文件路径为空或无效时抛出</exception>
    /// <exception cref="FileNotFoundException">文件不存在时抛出</exception>
    /// <exception cref="CsvDataException">CSV数据格式错误时抛出</exception>
    public IEnumerable<Dictionary<string, object>> ReadDynamicDataFromPath(string filePath)
    {
        ValidateFilePath(filePath);

        try
        {
            _logger.LogInformation("开始读取CSV文件(动态): {FilePath}", filePath);

            var fileContent = File.ReadAllText(filePath);
            
            // 检查文件是否为空
            if (string.IsNullOrWhiteSpace(fileContent))
            {
                throw new CsvDataException("CSV文件缺少标题行");
            }

            using var reader = new StringReader(fileContent);
            using var csv = new CsvReader(reader, GetCsvConfiguration());

            var records = new List<Dictionary<string, object>>();
            
            // 读取标题行
            if (!csv.Read())
            {
                throw new CsvDataException("CSV文件缺少标题行");
            }
            
            csv.ReadHeader();
            var headers = csv.HeaderRecord;

            if (headers == null || headers.Length == 0)
            {
                throw new CsvDataException("CSV文件缺少标题行");
            }

            // 读取数据行
            while (csv.Read())
            {
                var record = new Dictionary<string, object>();
                
                for (int i = 0; i < headers.Length; i++)
                {
                    var value = csv.GetField(i);
                    record[headers[i]] = ConvertValue(value);
                }
                
                records.Add(record);
            }

            _logger.LogInformation("成功读取 {Count} 条动态记录", records.Count);
            
            return records;
        }
        catch (Exception ex) when (!(ex is ArgumentException || ex is FileNotFoundException || ex is CsvDataException))
        {
            _logger.LogError(ex, "读取CSV文件失败(动态): {FilePath}", filePath);
            throw new CsvDataException($"读取CSV文件失败: {filePath}", ex);
        }
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
            throw new FileNotFoundException($"CSV文件不存在: {filePath}");
        }
    }

    /// <summary>
    /// 获取CSV配置
    /// </summary>
    /// <returns>CSV配置</returns>
    private static CsvConfiguration GetCsvConfiguration()
    {
        return new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            BadDataFound = null, // 忽略坏数据
            MissingFieldFound = null, // 忽略缺失字段
            HeaderValidated = null, // 忽略标题验证
            PrepareHeaderForMatch = args => args.Header.ToLower()
        };
    }

    /// <summary>
    /// 转换值类型
    /// </summary>
    /// <param name="value">原始值</param>
    /// <returns>转换后的值</returns>
    private static object ConvertValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        // 尝试转换为数字
        if (int.TryParse(value, out var intValue))
        {
            return intValue;
        }

        if (double.TryParse(value, out var doubleValue))
        {
            return doubleValue;
        }

        // 尝试转换为布尔值
        if (bool.TryParse(value, out var boolValue))
        {
            return boolValue;
        }

        // 尝试转换为日期时间
        if (DateTime.TryParse(value, out var dateValue))
        {
            return dateValue;
        }

        // 默认返回字符串
        return value;
    }
}