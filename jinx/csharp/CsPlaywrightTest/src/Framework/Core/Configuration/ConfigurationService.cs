using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;

namespace EnterpriseAutomationFramework.Core.Configuration;

/// <summary>
/// 配置服务，支持多环境配置加载
/// </summary>
public class ConfigurationService
{
    private readonly string _basePath;
    private readonly Dictionary<string, TestConfiguration> _configurationCache;
    private readonly object _lock = new();

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="basePath">配置文件基础路径</param>
    public ConfigurationService(string? basePath = null)
    {
        _basePath = basePath ?? Directory.GetCurrentDirectory();
        _configurationCache = new Dictionary<string, TestConfiguration>();
    }

    /// <summary>
    /// 加载指定环境的配置
    /// </summary>
    /// <param name="environment">环境名称（Development, Test, Staging, Production）</param>
    /// <returns>测试配置</returns>
    public TestConfiguration LoadConfiguration(string environment)
    {
        if (string.IsNullOrWhiteSpace(environment))
        {
            throw new ArgumentException("环境名称不能为空", nameof(environment));
        }

        // 验证环境名称是否有效
        var validEnvironments = new[] { "Development", "Test", "Staging", "Production" };
        if (!validEnvironments.Contains(environment, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"无效的环境名称: {environment}。有效值: {string.Join(", ", validEnvironments)}", nameof(environment));
        }

        // 使用缓存避免重复加载
        var cacheKey = environment.ToLowerInvariant();
        if (_configurationCache.TryGetValue(cacheKey, out var cachedConfig))
        {
            return cachedConfig;
        }

        lock (_lock)
        {
            // 双重检查锁定
            if (_configurationCache.TryGetValue(cacheKey, out cachedConfig))
            {
                return cachedConfig;
            }

            var configuration = LoadConfigurationInternal(environment);
            _configurationCache[cacheKey] = configuration;
            return configuration;
        }
    }

    /// <summary>
    /// 从命令行参数加载配置
    /// </summary>
    /// <param name="args">命令行参数</param>
    /// <returns>测试配置</returns>
    public TestConfiguration LoadConfigurationFromCommandLine(string[] args)
    {
        var environment = GetEnvironmentFromCommandLine(args);
        return LoadConfiguration(environment);
    }

    /// <summary>
    /// 从环境变量或命令行参数获取环境名称
    /// </summary>
    /// <param name="args">命令行参数</param>
    /// <returns>环境名称</returns>
    public string GetEnvironmentFromCommandLine(string[] args)
    {
        // 首先检查命令行参数
        var environment = ParseEnvironmentFromArgs(args);
        
        // 如果命令行没有指定，检查环境变量
        if (string.IsNullOrEmpty(environment))
        {
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") 
                         ?? Environment.GetEnvironmentVariable("TEST_ENVIRONMENT");
        }

        // 默认使用 Development 环境
        return environment ?? "Development";
    }

    /// <summary>
    /// 验证配置文件是否存在
    /// </summary>
    /// <param name="environment">环境名称</param>
    /// <returns>配置文件是否存在</returns>
    public bool ConfigurationFileExists(string environment)
    {
        var configFileName = $"appsettings.{environment}.json";
        var configFilePath = Path.Combine(_basePath, configFileName);
        return File.Exists(configFilePath);
    }

    /// <summary>
    /// 获取所有可用的环境配置
    /// </summary>
    /// <returns>可用环境列表</returns>
    public List<string> GetAvailableEnvironments()
    {
        var environments = new List<string>();
        var validEnvironments = new[] { "Development", "Test", "Staging", "Production" };

        foreach (var env in validEnvironments)
        {
            if (ConfigurationFileExists(env))
            {
                environments.Add(env);
            }
        }

        return environments;
    }

    /// <summary>
    /// 清除配置缓存
    /// </summary>
    public void ClearCache()
    {
        lock (_lock)
        {
            _configurationCache.Clear();
        }
    }

    /// <summary>
    /// 验证配置是否有效
    /// </summary>
    /// <param name="configuration">测试配置</param>
    /// <returns>验证结果</returns>
    public ValidationResult ValidateConfiguration(TestConfiguration configuration)
    {
        if (configuration == null)
        {
            return new ValidationResult("配置不能为空");
        }

        var context = new ValidationContext(configuration);
        var results = new List<ValidationResult>();
        
        var isValid = Validator.TryValidateObject(configuration, context, results, true);
        
        if (isValid)
        {
            return ValidationResult.Success!;
        }

        var errorMessages = results.Select(r => r.ErrorMessage).Where(m => !string.IsNullOrEmpty(m));
        return new ValidationResult(string.Join("; ", errorMessages));
    }

    /// <summary>
    /// 内部配置加载方法
    /// </summary>
    /// <param name="environment">环境名称</param>
    /// <returns>测试配置</returns>
    private TestConfiguration LoadConfigurationInternal(string environment)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(_basePath)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);

        // 添加环境特定的配置文件
        var environmentConfigFile = $"appsettings.{environment}.json";
        builder.AddJsonFile(environmentConfigFile, optional: false, reloadOnChange: false);

        // 添加环境变量支持
        builder.AddEnvironmentVariables();

        var config = builder.Build();
        var testConfig = new TestConfiguration();

        try
        {
            // 绑定配置
            config.GetSection("TestConfiguration").Bind(testConfig);

            // 验证配置
            var validationResult = ValidateConfiguration(testConfig);
            if (validationResult != ValidationResult.Success)
            {
                throw new InvalidOperationException($"配置验证失败: {validationResult.ErrorMessage}");
            }

            return testConfig;
        }
        catch (Exception ex) when (!(ex is InvalidOperationException))
        {
            throw new InvalidOperationException($"加载环境 '{environment}' 的配置时发生错误: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 从命令行参数解析环境名称
    /// </summary>
    /// <param name="args">命令行参数</param>
    /// <returns>环境名称</returns>
    private static string? ParseEnvironmentFromArgs(string[] args)
    {
        if (args == null || args.Length == 0)
        {
            return null;
        }

        // 支持的命令行格式:
        // --environment Development
        // --env Development  
        // -e Development
        // --environment=Development
        // --env=Development
        // -e=Development

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            // 处理 --environment=value 格式
            if (arg.StartsWith("--environment=", StringComparison.OrdinalIgnoreCase))
            {
                return arg.Substring("--environment=".Length);
            }

            // 处理 --env=value 格式
            if (arg.StartsWith("--env=", StringComparison.OrdinalIgnoreCase))
            {
                return arg.Substring("--env=".Length);
            }

            // 处理 -e=value 格式
            if (arg.StartsWith("-e=", StringComparison.OrdinalIgnoreCase))
            {
                return arg.Substring("-e=".Length);
            }

            // 处理 --environment value 格式
            if ((arg.Equals("--environment", StringComparison.OrdinalIgnoreCase) ||
                 arg.Equals("--env", StringComparison.OrdinalIgnoreCase) ||
                 arg.Equals("-e", StringComparison.OrdinalIgnoreCase)) &&
                i + 1 < args.Length)
            {
                return args[i + 1];
            }
        }

        return null;
    }
}