namespace BlogApi.Application.DTOs.Common;

/// <summary>
/// 操作结果
/// </summary>
/// <typeparam name="T">结果数据类型</typeparam>
public class OperationResult<T>
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 结果数据
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// 错误代码
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// 验证错误列表
    /// </summary>
    public List<ValidationError> ValidationErrors { get; set; } = new();

    /// <summary>
    /// 创建成功结果
    /// </summary>
    /// <param name="data">结果数据</param>
    /// <returns>成功结果</returns>
    public static OperationResult<T> CreateSuccess(T data)
    {
        return new OperationResult<T>
        {
            Success = true,
            Data = data
        };
    }

    /// <summary>
    /// 创建失败结果
    /// </summary>
    /// <param name="errorMessage">错误消息</param>
    /// <param name="errorCode">错误代码</param>
    /// <returns>失败结果</returns>
    public static OperationResult<T> CreateFailure(string errorMessage, string errorCode = "")
    {
        return new OperationResult<T>
        {
            Success = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
    }

    /// <summary>
    /// 创建验证失败结果
    /// </summary>
    /// <param name="validationErrors">验证错误列表</param>
    /// <returns>验证失败结果</returns>
    public static OperationResult<T> CreateValidationFailure(List<ValidationError> validationErrors)
    {
        return new OperationResult<T>
        {
            Success = false,
            ErrorMessage = "验证失败",
            ErrorCode = "VALIDATION_FAILED",
            ValidationErrors = validationErrors
        };
    }
}

/// <summary>
/// 无数据的操作结果
/// </summary>
public class OperationResult : OperationResult<object>
{
    /// <summary>
    /// 创建成功结果
    /// </summary>
    /// <returns>成功结果</returns>
    public static OperationResult CreateSuccess()
    {
        return new OperationResult
        {
            Success = true
        };
    }

    /// <summary>
    /// 创建失败结果
    /// </summary>
    /// <param name="errorMessage">错误消息</param>
    /// <param name="errorCode">错误代码</param>
    /// <returns>失败结果</returns>
    public static new OperationResult CreateFailure(string errorMessage, string errorCode = "")
    {
        return new OperationResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
    }
}