using Microsoft.Extensions.Logging;
using EnterpriseAutomationFramework.Core.Interfaces;
using EnterpriseAutomationFramework.Core.Exceptions;

namespace EnterpriseAutomationFramework.Core.Base;

/// <summary>
/// 业务流程基类
/// </summary>
public abstract class BaseFlow : IFlow
{
    protected readonly ILogger _logger;
    protected readonly ITestFixture _testFixture;
    private readonly List<string> _executedSteps;
    private DateTime _flowStartTime;

    protected BaseFlow(ITestFixture testFixture, ILogger logger)
    {
        _testFixture = testFixture ?? throw new ArgumentNullException(nameof(testFixture));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _executedSteps = new List<string>();
    }

    /// <summary>
    /// 流程名称
    /// </summary>
    protected virtual string FlowName => GetType().Name;

    /// <summary>
    /// 已执行的步骤列表
    /// </summary>
    protected IReadOnlyList<string> ExecutedSteps => _executedSteps.AsReadOnly();

    /// <summary>
    /// 执行业务流程
    /// </summary>
    public abstract Task ExecuteAsync(Dictionary<string, object>? parameters = null);

    /// <summary>
    /// 执行流程步骤
    /// </summary>
    protected async Task ExecuteStepAsync(string stepName, Func<Task> stepAction)
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
        _logger.LogInformation($"[{FlowName}] 开始执行步骤: {stepName}");
        
        try
        {
            await stepAction();
            var stepDuration = DateTime.UtcNow - stepStartTime;
            _executedSteps.Add(stepName);
            _logger.LogInformation($"[{FlowName}] 步骤执行成功: {stepName} (耗时: {stepDuration.TotalMilliseconds:F2}ms)");
        }
        catch (Exception ex)
        {
            var stepDuration = DateTime.UtcNow - stepStartTime;
            _logger.LogError(ex, $"[{FlowName}] 步骤执行失败: {stepName} (耗时: {stepDuration.TotalMilliseconds:F2}ms)");
            
            // 包装异常以提供更多上下文信息
            throw new TestFrameworkException(
                FlowName, 
                "Flow", 
                $"步骤 '{stepName}' 执行失败: {ex.Message}", 
                ex);
        }
    }

    /// <summary>
    /// 验证步骤结果
    /// </summary>
    protected void ValidateStep(string stepName, bool condition, string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(stepName))
        {
            throw new ArgumentException("步骤名称不能为空", nameof(stepName));
        }

        if (!condition)
        {
            _logger.LogError($"[{FlowName}] 步骤验证失败: {stepName} - {errorMessage}");
            throw new TestFrameworkException(
                FlowName, 
                "Flow", 
                $"步骤验证失败 '{stepName}': {errorMessage}");
        }
        
        _logger.LogInformation($"[{FlowName}] 步骤验证成功: {stepName}");
    }

    /// <summary>
    /// 验证必需参数
    /// </summary>
    protected void ValidateRequiredParameter(Dictionary<string, object>? parameters, string parameterName)
    {
        if (parameters == null)
        {
            throw new ArgumentNullException(nameof(parameters), $"流程 {FlowName} 需要参数");
        }

        if (!parameters.ContainsKey(parameterName))
        {
            throw new ArgumentException($"流程 {FlowName} 需要 '{parameterName}' 参数", nameof(parameters));
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

    /// <summary>
    /// 开始流程执行跟踪
    /// </summary>
    protected void StartFlowExecution()
    {
        _flowStartTime = DateTime.UtcNow;
        _executedSteps.Clear();
        _logger.LogInformation($"[{FlowName}] 开始执行业务流程");
    }

    /// <summary>
    /// 结束流程执行跟踪
    /// </summary>
    protected void EndFlowExecution()
    {
        var totalDuration = DateTime.UtcNow - _flowStartTime;
        _logger.LogInformation($"[{FlowName}] 业务流程执行完成 (总耗时: {totalDuration.TotalMilliseconds:F2}ms, 执行步骤: {_executedSteps.Count})");
    }

    /// <summary>
    /// 记录流程执行失败
    /// </summary>
    protected void LogFlowExecutionFailure(Exception ex)
    {
        var totalDuration = DateTime.UtcNow - _flowStartTime;
        _logger.LogError(ex, $"[{FlowName}] 业务流程执行失败 (耗时: {totalDuration.TotalMilliseconds:F2}ms, 已执行步骤: {_executedSteps.Count})");
        _logger.LogInformation($"[{FlowName}] 已执行的步骤: {string.Join(" -> ", _executedSteps)}");
    }
}