using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace CsPlaywrightXun.src.playwright.Core.Configuration;

/// <summary>
/// 配置管理器
/// </summary>
public static class ConfigurationManager
{
    private static TestConfiguration? _configuration;
    private static readonly object _lock = new();

    /// <summary>
    /// 获取测试配置
    /// </summary>
    public static TestConfiguration GetConfiguration()
    {
        if (_configuration == null)
        {
            lock (_lock)
            {
                _configuration ??= LoadConfiguration();
            }
        }
        
        return _configuration;
    }

    /// <summary>
    /// 加载配置
    /// </summary>
    private static TestConfiguration LoadConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true)
            .AddEnvironmentVariables();

        var config = builder.Build();
        var testConfig = new TestConfiguration();
        
        // 绑定配置
        config.GetSection("TestConfiguration").Bind(testConfig);
        
        return testConfig;
    }

    /// <summary>
    /// 从文件加载配置
    /// </summary>
    public static TestConfiguration LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"配置文件不存在: {filePath}");
        }

        var json = File.ReadAllText(filePath);
        return JsonConvert.DeserializeObject<TestConfiguration>(json) ?? new TestConfiguration();
    }

    /// <summary>
    /// 保存配置到文件
    /// </summary>
    public static void SaveToFile(TestConfiguration configuration, string filePath)
    {
        var json = JsonConvert.SerializeObject(configuration, Formatting.Indented);
        File.WriteAllText(filePath, json);
    }
}