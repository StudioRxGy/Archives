using System.ComponentModel.DataAnnotations;

namespace EnterpriseAutomationFramework.Core.Configuration;

/// <summary>
/// 测试配置类
/// </summary>
public class TestConfiguration : IValidatableObject
{
    /// <summary>
    /// 环境设置
    /// </summary>
    [Required(ErrorMessage = "环境设置不能为空")]
    public EnvironmentSettings Environment { get; set; } = new();
    
    /// <summary>
    /// 浏览器设置
    /// </summary>
    [Required(ErrorMessage = "浏览器设置不能为空")]
    public BrowserSettings Browser { get; set; } = new();
    
    /// <summary>
    /// API 设置
    /// </summary>
    [Required(ErrorMessage = "API设置不能为空")]
    public ApiSettings Api { get; set; } = new();
    
    /// <summary>
    /// 报告设置
    /// </summary>
    [Required(ErrorMessage = "报告设置不能为空")]
    public ReportingSettings Reporting { get; set; } = new();
    
    /// <summary>
    /// 日志设置
    /// </summary>
    [Required(ErrorMessage = "日志设置不能为空")]
    public LoggingSettings Logging { get; set; } = new();

    /// <summary>
    /// 验证配置
    /// </summary>
    /// <param name="validationContext">验证上下文</param>
    /// <returns>验证结果</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        // 验证环境设置
        var environmentResults = new List<ValidationResult>();
        var environmentContext = new ValidationContext(Environment);
        if (!Validator.TryValidateObject(Environment, environmentContext, environmentResults, true))
        {
            results.AddRange(environmentResults);
        }

        // 验证浏览器设置
        var browserResults = new List<ValidationResult>();
        var browserContext = new ValidationContext(Browser);
        if (!Validator.TryValidateObject(Browser, browserContext, browserResults, true))
        {
            results.AddRange(browserResults);
        }

        // 验证API设置
        var apiResults = new List<ValidationResult>();
        var apiContext = new ValidationContext(Api);
        if (!Validator.TryValidateObject(Api, apiContext, apiResults, true))
        {
            results.AddRange(apiResults);
        }

        // 验证报告设置
        var reportingResults = new List<ValidationResult>();
        var reportingContext = new ValidationContext(Reporting);
        if (!Validator.TryValidateObject(Reporting, reportingContext, reportingResults, true))
        {
            results.AddRange(reportingResults);
        }

        // 验证日志设置
        var loggingResults = new List<ValidationResult>();
        var loggingContext = new ValidationContext(Logging);
        if (!Validator.TryValidateObject(Logging, loggingContext, loggingResults, true))
        {
            results.AddRange(loggingResults);
        }

        return results;
    }

    /// <summary>
    /// 验证配置是否有效
    /// </summary>
    /// <returns>验证是否通过</returns>
    public bool IsValid()
    {
        var context = new ValidationContext(this);
        var results = new List<ValidationResult>();
        return Validator.TryValidateObject(this, context, results, true);
    }

    /// <summary>
    /// 获取验证错误信息
    /// </summary>
    /// <returns>验证错误列表</returns>
    public List<string> GetValidationErrors()
    {
        var context = new ValidationContext(this);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(this, context, results, true);
        return results.Select(r => r.ErrorMessage ?? "未知验证错误").ToList();
    }
}

/// <summary>
/// 环境设置
/// </summary>
public class EnvironmentSettings : IValidatableObject
{
    /// <summary>
    /// 环境名称
    /// </summary>
    [Required(ErrorMessage = "环境名称不能为空")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "环境名称长度必须在1-50个字符之间")]
    public string Name { get; set; } = "Development";
    
    /// <summary>
    /// 基础URL
    /// </summary>
    [Required(ErrorMessage = "基础URL不能为空")]
    [Url(ErrorMessage = "基础URL格式不正确")]
    public string BaseUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// API基础URL
    /// </summary>
    [Required(ErrorMessage = "API基础URL不能为空")]
    [Url(ErrorMessage = "API基础URL格式不正确")]
    public string ApiBaseUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// 环境变量
    /// </summary>
    public Dictionary<string, string> Variables { get; set; } = new();

    /// <summary>
    /// 验证环境设置
    /// </summary>
    /// <param name="validationContext">验证上下文</param>
    /// <returns>验证结果</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        // 验证环境名称是否为有效的环境类型
        var validEnvironments = new[] { "Development", "Test", "Staging", "Production" };
        if (!validEnvironments.Contains(Name, StringComparer.OrdinalIgnoreCase))
        {
            results.Add(new ValidationResult(
                $"环境名称必须是以下值之一: {string.Join(", ", validEnvironments)}",
                new[] { nameof(Name) }));
        }

        return results;
    }
}

/// <summary>
/// 浏览器设置
/// </summary>
public class BrowserSettings : IValidatableObject
{
    /// <summary>
    /// 浏览器类型
    /// </summary>
    [Required(ErrorMessage = "浏览器类型不能为空")]
    public string Type { get; set; } = "Chromium";
    
    /// <summary>
    /// 是否无头模式
    /// </summary>
    public bool Headless { get; set; } = false;
    
    /// <summary>
    /// 视口宽度
    /// </summary>
    [Range(100, 4000, ErrorMessage = "视口宽度必须在100-4000像素之间")]
    public int ViewportWidth { get; set; } = 1920;
    
    /// <summary>
    /// 视口高度
    /// </summary>
    [Range(100, 3000, ErrorMessage = "视口高度必须在100-3000像素之间")]
    public int ViewportHeight { get; set; } = 1080;
    
    /// <summary>
    /// 超时时间（毫秒）
    /// </summary>
    [Range(1000, 300000, ErrorMessage = "超时时间必须在1000-300000毫秒之间")]
    public int Timeout { get; set; } = 30000;

    /// <summary>
    /// 验证浏览器设置
    /// </summary>
    /// <param name="validationContext">验证上下文</param>
    /// <returns>验证结果</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        // 验证浏览器类型是否为支持的类型
        var validBrowserTypes = new[] { "Chromium", "Firefox", "Webkit" };
        if (!validBrowserTypes.Contains(Type, StringComparer.OrdinalIgnoreCase))
        {
            results.Add(new ValidationResult(
                $"浏览器类型必须是以下值之一: {string.Join(", ", validBrowserTypes)}",
                new[] { nameof(Type) }));
        }

        return results;
    }
}

/// <summary>
/// API 设置
/// </summary>
public class ApiSettings : IValidatableObject
{
    /// <summary>
    /// 超时时间（毫秒）
    /// </summary>
    [Range(1000, 300000, ErrorMessage = "API超时时间必须在1000-300000毫秒之间")]
    public int Timeout { get; set; } = 30000;
    
    /// <summary>
    /// 重试次数
    /// </summary>
    [Range(0, 10, ErrorMessage = "重试次数必须在0-10次之间")]
    public int RetryCount { get; set; } = 3;
    
    /// <summary>
    /// 重试延迟（毫秒）
    /// </summary>
    [Range(100, 60000, ErrorMessage = "重试延迟必须在100-60000毫秒之间")]
    public int RetryDelay { get; set; } = 1000;

    /// <summary>
    /// 验证API设置
    /// </summary>
    /// <param name="validationContext">验证上下文</param>
    /// <returns>验证结果</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        // 如果启用重试，重试延迟不能为0
        if (RetryCount > 0 && RetryDelay <= 0)
        {
            results.Add(new ValidationResult(
                "当重试次数大于0时，重试延迟必须大于0",
                new[] { nameof(RetryDelay) }));
        }

        return results;
    }
}

/// <summary>
/// 报告设置
/// </summary>
public class ReportingSettings
{
    /// <summary>
    /// 输出路径
    /// </summary>
    public string OutputPath { get; set; } = "Reports";
    
    /// <summary>
    /// 报告格式
    /// </summary>
    public string Format { get; set; } = "Html";
    
    /// <summary>
    /// 是否包含截图
    /// </summary>
    public bool IncludeScreenshots { get; set; } = true;
}

/// <summary>
/// 日志设置
/// </summary>
public class LoggingSettings : IValidatableObject
{
    /// <summary>
    /// 日志级别
    /// </summary>
    [Required(ErrorMessage = "日志级别不能为空")]
    public string Level { get; set; } = "Information";
    
    /// <summary>
    /// 日志文件路径
    /// </summary>
    [Required(ErrorMessage = "日志文件路径不能为空")]
    public string FilePath { get; set; } = "Logs/test-{Date}.log";
    
    /// <summary>
    /// 是否启用控制台输出
    /// </summary>
    public bool EnableConsole { get; set; } = true;
    
    /// <summary>
    /// 是否启用文件输出
    /// </summary>
    public bool EnableFile { get; set; } = true;
    
    /// <summary>
    /// 是否启用结构化日志
    /// </summary>
    public bool EnableStructuredLogging { get; set; } = true;
    
    /// <summary>
    /// 文件大小限制（MB）
    /// </summary>
    [Range(1, 1000, ErrorMessage = "文件大小限制必须在1-1000MB之间")]
    public int FileSizeLimitMB { get; set; } = 100;
    
    /// <summary>
    /// 保留文件数量
    /// </summary>
    [Range(1, 365, ErrorMessage = "保留文件数量必须在1-365之间")]
    public int RetainedFileCount { get; set; } = 30;
    
    /// <summary>
    /// 是否启用测试上下文关联
    /// </summary>
    public bool EnableTestContext { get; set; } = true;

    /// <summary>
    /// 验证日志设置
    /// </summary>
    /// <param name="validationContext">验证上下文</param>
    /// <returns>验证结果</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        // 验证日志级别是否为有效值
        var validLevels = new[] { "Verbose", "Debug", "Information", "Warning", "Error", "Fatal" };
        if (!validLevels.Contains(Level, StringComparer.OrdinalIgnoreCase))
        {
            results.Add(new ValidationResult(
                $"日志级别必须是以下值之一: {string.Join(", ", validLevels)}",
                new[] { nameof(Level) }));
        }

        // 验证至少启用一种输出方式
        if (!EnableConsole && !EnableFile)
        {
            results.Add(new ValidationResult(
                "必须至少启用控制台输出或文件输出中的一种",
                new[] { nameof(EnableConsole), nameof(EnableFile) }));
        }

        // 验证文件路径格式
        if (EnableFile && string.IsNullOrWhiteSpace(FilePath))
        {
            results.Add(new ValidationResult(
                "启用文件输出时，文件路径不能为空",
                new[] { nameof(FilePath) }));
        }

        return results;
    }
}