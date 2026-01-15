namespace CsPlaywrightXun.src.playwright.Core.Utilities;

/// <summary>
/// 错误恢复设置
/// </summary>
public class ErrorRecoverySettings
{
    /// <summary>
    /// 页面刷新超时时间（毫秒）
    /// </summary>
    public int PageRefreshTimeout { get; set; } = 30000;

    /// <summary>
    /// 页面加载超时时间（毫秒）
    /// </summary>
    public int PageLoadTimeout { get; set; } = 30000;

    /// <summary>
    /// 浏览器重启延迟时间（毫秒）
    /// </summary>
    public int BrowserRestartDelay { get; set; } = 2000;

    /// <summary>
    /// API重试延迟时间（毫秒）
    /// </summary>
    public int ApiRetryDelay { get; set; } = 1000;

    /// <summary>
    /// 浏览器重启延迟时间（TimeSpan）
    /// </summary>
    public TimeSpan BrowserRestartDelayTimeSpan => TimeSpan.FromMilliseconds(BrowserRestartDelay);

    /// <summary>
    /// API重试延迟时间（TimeSpan）
    /// </summary>
    public TimeSpan ApiRetryDelayTimeSpan => TimeSpan.FromMilliseconds(ApiRetryDelay);

    /// <summary>
    /// 最大重试延迟时间（TimeSpan）
    /// </summary>
    public TimeSpan MaxRetryDelayTimeSpan => TimeSpan.FromMilliseconds(MaxRetryDelay);

    /// <summary>
    /// API最大重试次数
    /// </summary>
    public int ApiMaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// 是否启用指数退避
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = true;

    /// <summary>
    /// 指数退避倍数
    /// </summary>
    public double ExponentialBackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// 最大重试延迟时间（毫秒）
    /// </summary>
    public int MaxRetryDelay { get; set; } = 30000;

    /// <summary>
    /// 是否启用页面刷新恢复
    /// </summary>
    public bool EnablePageRefreshRecovery { get; set; } = true;

    /// <summary>
    /// 是否启用浏览器重启恢复
    /// </summary>
    public bool EnableBrowserRestartRecovery { get; set; } = true;

    /// <summary>
    /// 是否启用API重试恢复
    /// </summary>
    public bool EnableApiRetryRecovery { get; set; } = true;

    /// <summary>
    /// 页面刷新最大尝试次数
    /// </summary>
    public int PageRefreshMaxAttempts { get; set; } = 2;

    /// <summary>
    /// 浏览器重启最大尝试次数
    /// </summary>
    public int BrowserRestartMaxAttempts { get; set; } = 1;

    /// <summary>
    /// 创建默认错误恢复设置
    /// </summary>
    /// <returns>默认错误恢复设置</returns>
    public static ErrorRecoverySettings CreateDefault()
    {
        return new ErrorRecoverySettings();
    }

    /// <summary>
    /// 创建API专用错误恢复设置
    /// </summary>
    /// <returns>API错误恢复设置</returns>
    public static ErrorRecoverySettings CreateForApi()
    {
        return new ErrorRecoverySettings
        {
            ApiRetryDelay = 1000,
            ApiMaxRetryAttempts = 5,
            UseExponentialBackoff = true,
            ExponentialBackoffMultiplier = 2.0,
            MaxRetryDelay = 60000,
            EnablePageRefreshRecovery = false,
            EnableBrowserRestartRecovery = false,
            EnableApiRetryRecovery = true
        };
    }

    /// <summary>
    /// 创建UI专用错误恢复设置
    /// </summary>
    /// <returns>UI错误恢复设置</returns>
    public static ErrorRecoverySettings CreateForUi()
    {
        return new ErrorRecoverySettings
        {
            PageRefreshTimeout = 30000,
            PageLoadTimeout = 30000,
            BrowserRestartDelay = 3000,
            PageRefreshMaxAttempts = 3,
            BrowserRestartMaxAttempts = 2,
            EnablePageRefreshRecovery = true,
            EnableBrowserRestartRecovery = true,
            EnableApiRetryRecovery = false
        };
    }

    /// <summary>
    /// 创建快速恢复设置（用于开发和调试）
    /// </summary>
    /// <returns>快速恢复设置</returns>
    public static ErrorRecoverySettings CreateFast()
    {
        return new ErrorRecoverySettings
        {
            PageRefreshTimeout = 10000,
            PageLoadTimeout = 10000,
            BrowserRestartDelay = 1000,
            ApiRetryDelay = 500,
            ApiMaxRetryAttempts = 2,
            UseExponentialBackoff = false,
            PageRefreshMaxAttempts = 1,
            BrowserRestartMaxAttempts = 1
        };
    }

    /// <summary>
    /// 创建保守恢复设置（用于生产环境）
    /// </summary>
    /// <returns>保守恢复设置</returns>
    public static ErrorRecoverySettings CreateConservative()
    {
        return new ErrorRecoverySettings
        {
            PageRefreshTimeout = 60000,
            PageLoadTimeout = 60000,
            BrowserRestartDelay = 5000,
            ApiRetryDelay = 2000,
            ApiMaxRetryAttempts = 5,
            UseExponentialBackoff = true,
            ExponentialBackoffMultiplier = 1.5,
            MaxRetryDelay = 120000,
            PageRefreshMaxAttempts = 3,
            BrowserRestartMaxAttempts = 2
        };
    }

    /// <summary>
    /// 验证设置是否有效
    /// </summary>
    /// <returns>是否有效</returns>
    public bool IsValid()
    {
        return PageRefreshTimeout > 0 &&
               PageLoadTimeout > 0 &&
               BrowserRestartDelay >= 0 &&
               ApiRetryDelay >= 0 &&
               ApiMaxRetryAttempts >= 0 &&
               ExponentialBackoffMultiplier > 1.0 &&
               MaxRetryDelay > 0 &&
               PageRefreshMaxAttempts >= 0 &&
               BrowserRestartMaxAttempts >= 0;
    }

    /// <summary>
    /// 获取验证错误列表
    /// </summary>
    /// <returns>验证错误列表</returns>
    public List<string> GetValidationErrors()
    {
        var errors = new List<string>();

        if (PageRefreshTimeout <= 0)
            errors.Add("页面刷新超时时间必须大于0");

        if (PageLoadTimeout <= 0)
            errors.Add("页面加载超时时间必须大于0");

        if (BrowserRestartDelay < 0)
            errors.Add("浏览器重启延迟时间不能小于0");

        if (ApiRetryDelay < 0)
            errors.Add("API重试延迟时间不能小于0");

        if (ApiMaxRetryAttempts < 0)
            errors.Add("API最大重试次数不能小于0");

        if (ExponentialBackoffMultiplier <= 1.0)
            errors.Add("指数退避倍数必须大于1.0");

        if (MaxRetryDelay <= 0)
            errors.Add("最大重试延迟时间必须大于0");

        if (PageRefreshMaxAttempts < 0)
            errors.Add("页面刷新最大尝试次数不能小于0");

        if (BrowserRestartMaxAttempts < 0)
            errors.Add("浏览器重启最大尝试次数不能小于0");

        return errors;
    }

    /// <summary>
    /// 克隆设置
    /// </summary>
    /// <returns>克隆的设置</returns>
    public ErrorRecoverySettings Clone()
    {
        return new ErrorRecoverySettings
        {
            PageRefreshTimeout = PageRefreshTimeout,
            PageLoadTimeout = PageLoadTimeout,
            BrowserRestartDelay = BrowserRestartDelay,
            ApiRetryDelay = ApiRetryDelay,
            ApiMaxRetryAttempts = ApiMaxRetryAttempts,
            UseExponentialBackoff = UseExponentialBackoff,
            ExponentialBackoffMultiplier = ExponentialBackoffMultiplier,
            MaxRetryDelay = MaxRetryDelay,
            EnablePageRefreshRecovery = EnablePageRefreshRecovery,
            EnableBrowserRestartRecovery = EnableBrowserRestartRecovery,
            EnableApiRetryRecovery = EnableApiRetryRecovery,
            PageRefreshMaxAttempts = PageRefreshMaxAttempts,
            BrowserRestartMaxAttempts = BrowserRestartMaxAttempts
        };
    }
}