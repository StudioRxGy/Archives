using Microsoft.Extensions.Logging;
using CsPlaywrightXun.src.playwright.Core.Fixtures;
using CsPlaywrightXun.src.playwright.Core.Interfaces;
using CsPlaywrightXun.src.playwright.Core.Configuration;
using CsPlaywrightXun.src.playwright.Tests.API.baidu;

namespace CsPlaywrightXun.src.playwright.Tests.Integration.Scenarios;

/// <summary>
/// 场景基类
/// 提供多流程编排的基础功能，不包含断言逻辑
/// 需求: 1.3, 5.4
/// </summary>
public abstract class BaseScenario : IFlow
{
    protected readonly BrowserFixture _browserFixture;
    protected readonly ApiTestFixture _apiFixture;
    protected readonly ILogger _logger;
    private readonly List<string> _executedSteps;
    private DateTime _scenarioStartTime;
    private ScenarioExecutionResult? _executionResult;

    protected BaseScenario(BrowserFixture browserFixture, ApiTestFixture apiFixture, ILogger logger)
    {
        _browserFixture = browserFixture ?? throw new ArgumentNullException(nameof(browserFixture));
        _apiFixture = apiFixture ?? throw new ArgumentNullException(nameof(apiFixture));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _executedSteps = new List<string>();
    }

    /// <summary>
    /// 场景名称
    /// </summary>
    protected virtual string ScenarioName => GetType().Name;

    /// <summary>
    /// 已执行的步骤列表
    /// </summary>
    protected IReadOnlyList<string> ExecutedSteps => _executedSteps.AsReadOnly();

    /// <summary>
    /// 执行场景
    /// </summary>
    public abstract Task ExecuteAsync(Dictionary<string, object>? parameters = null);

    /// <summary>
    /// 获取场景执行结果
    /// </summary>
    public ScenarioExecutionResult GetExecutionResult()
    {
        return _executionResult ?? new ScenarioExecutionResult
        {
            ScenarioName = ScenarioName,
            IsSuccess = false,
            ErrorMessage = "场景尚未执行或执行结果未设置"
        };
    }

    /// <summary>
    /// 执行场景步骤
    /// </summary>
    protected async Task ExecuteScenarioStepAsync(string stepName, Func<Task> stepAction)
    {
        if (string.IsNullOrWhiteSpace(stepName))
        {
            throw new ArgumentException("步骤名称不能为空", nameof(stepName));
        }

        if (stepAction == null)
        {
            throw new ArgumentNullException(nameof(stepAction));
        }

        var stepStartTime = DateTime.UtcNow;
        _logger.LogInformation($"[{ScenarioName}] 开始执行场景步骤: {stepName}");
        
        try
        {
            await stepAction();
            var stepDuration = DateTime.UtcNow - stepStartTime;
            _executedSteps.Add(stepName);
            _logger.LogInformation($"[{ScenarioName}] 场景步骤执行成功: {stepName} (耗时: {stepDuration.TotalMilliseconds:F2}ms)");
        }
        catch (Exception ex)
        {
            var stepDuration = DateTime.UtcNow - stepStartTime;
            _logger.LogError(ex, $"[{ScenarioName}] 场景步骤执行失败: {stepName} (耗时: {stepDuration.TotalMilliseconds:F2}ms)");
            throw;
        }
    }

    /// <summary>
    /// 执行带返回值的场景步骤
    /// </summary>
    protected async Task<T> ExecuteScenarioStepAsync<T>(string stepName, Func<Task<T>> stepAction)
    {
        if (string.IsNullOrWhiteSpace(stepName))
        {
            throw new ArgumentException("步骤名称不能为空", nameof(stepName));
        }

        if (stepAction == null)
        {
            throw new ArgumentNullException(nameof(stepAction));
        }

        var stepStartTime = DateTime.UtcNow;
        _logger.LogInformation($"[{ScenarioName}] 开始执行场景步骤: {stepName}");
        
        try
        {
            var result = await stepAction();
            var stepDuration = DateTime.UtcNow - stepStartTime;
            _executedSteps.Add(stepName);
            _logger.LogInformation($"[{ScenarioName}] 场景步骤执行成功: {stepName} (耗时: {stepDuration.TotalMilliseconds:F2}ms)");
            return result;
        }
        catch (Exception ex)
        {
            var stepDuration = DateTime.UtcNow - stepStartTime;
            _logger.LogError(ex, $"[{ScenarioName}] 场景步骤执行失败: {stepName} (耗时: {stepDuration.TotalMilliseconds:F2}ms)");
            throw;
        }
    }

    /// <summary>
    /// 开始场景执行跟踪
    /// </summary>
    protected void StartScenarioExecution()
    {
        _scenarioStartTime = DateTime.UtcNow;
        _executedSteps.Clear();
        _executionResult = new ScenarioExecutionResult
        {
            ScenarioName = ScenarioName,
            StartTime = _scenarioStartTime
        };
        _logger.LogInformation($"[{ScenarioName}] 开始执行场景");
    }

    /// <summary>
    /// 结束场景执行跟踪
    /// </summary>
    protected void EndScenarioExecution(bool isSuccess = true, string? errorMessage = null)
    {
        var endTime = DateTime.UtcNow;
        var totalDuration = endTime - _scenarioStartTime;
        
        if (_executionResult != null)
        {
            _executionResult.EndTime = endTime;
            _executionResult.TotalDuration = totalDuration;
            _executionResult.IsSuccess = isSuccess;
            _executionResult.ErrorMessage = errorMessage;
            _executionResult.ExecutedSteps = new List<string>(_executedSteps);
        }
        
        if (isSuccess)
        {
            _logger.LogInformation($"[{ScenarioName}] 场景执行完成 (总耗时: {totalDuration.TotalMilliseconds:F2}ms, 执行步骤: {_executedSteps.Count})");
        }
        else
        {
            _logger.LogError($"[{ScenarioName}] 场景执行失败 (耗时: {totalDuration.TotalMilliseconds:F2}ms, 已执行步骤: {_executedSteps.Count}): {errorMessage}");
        }
    }

    /// <summary>
    /// 记录场景执行失败
    /// </summary>
    protected void LogScenarioExecutionFailure(Exception ex)
    {
        var totalDuration = DateTime.UtcNow - _scenarioStartTime;
        _logger.LogError(ex, $"[{ScenarioName}] 场景执行失败 (耗时: {totalDuration.TotalMilliseconds:F2}ms, 已执行步骤: {_executedSteps.Count})");
        _logger.LogInformation($"[{ScenarioName}] 已执行的步骤: {string.Join(" -> ", _executedSteps)}");
        
        EndScenarioExecution(false, ex.Message);
    }

    /// <summary>
    /// 验证必需参数
    /// </summary>
    protected void ValidateRequiredParameter(Dictionary<string, object>? parameters, string parameterName)
    {
        if (parameters == null)
        {
            throw new ArgumentNullException(nameof(parameters), $"场景 {ScenarioName} 需要参数");
        }

        if (!parameters.ContainsKey(parameterName))
        {
            throw new ArgumentException($"场景 {ScenarioName} 需要 '{parameterName}' 参数", nameof(parameters));
        }

        if (parameters[parameterName] == null)
        {
            throw new ArgumentException($"参数 '{parameterName}' 不能为 null", nameof(parameters));
        }
    }

    /// <summary>
    /// 验证字符串参数不为空
    /// </summary>
    protected void ValidateStringParameter(Dictionary<string, object> parameters, string parameterName)
    {
        ValidateRequiredParameter(parameters, parameterName);
        
        if (string.IsNullOrWhiteSpace(parameters[parameterName]?.ToString()))
        {
            throw new ArgumentException($"参数 '{parameterName}' 不能为空字符串", nameof(parameters));
        }
    }
}

/// <summary>
/// 场景执行结果
/// </summary>
public class ScenarioExecutionResult
{
    /// <summary>
    /// 场景名称
    /// </summary>
    public string ScenarioName { get; set; } = string.Empty;
    
    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime EndTime { get; set; }
    
    /// <summary>
    /// 总耗时
    /// </summary>
    public TimeSpan TotalDuration { get; set; }
    
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }
    
    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// 已执行的步骤
    /// </summary>
    public List<string> ExecutedSteps { get; set; } = new();
    
    /// <summary>
    /// UI验证是否通过
    /// </summary>
    public bool UIValidationPassed { get; set; }
    
    /// <summary>
    /// API验证是否通过
    /// </summary>
    public bool APIValidationPassed { get; set; }
    
    /// <summary>
    /// 性能检查是否通过
    /// </summary>
    public bool PerformanceCheckPassed { get; set; }
    
    /// <summary>
    /// 完成的搜索次数
    /// </summary>
    public int CompletedSearches { get; set; }
    
    /// <summary>
    /// 平均响应时间（毫秒）
    /// </summary>
    public double AverageResponseTime { get; set; }
    
    /// <summary>
    /// 成功率（百分比）
    /// </summary>
    public double SuccessRate { get; set; }
    
    /// <summary>
    /// 扩展属性
    /// </summary>
    public Dictionary<string, object> ExtendedProperties { get; set; } = new();
}