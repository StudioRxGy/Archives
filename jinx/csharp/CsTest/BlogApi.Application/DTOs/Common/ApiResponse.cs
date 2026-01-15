namespace BlogApi.Application.DTOs.Common;

/// <summary>
/// 统一API响应格式
/// </summary>
/// <typeparam name="T">响应数据类型</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 响应消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 响应数据
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// 错误列表
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 创建成功响应
    /// </summary>
    /// <param name="data">响应数据</param>
    /// <param name="message">成功消息</param>
    /// <returns>成功响应</returns>
    public static ApiResponse<T> CreateSuccess(T data, string message = "操作成功")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    /// <summary>
    /// 创建失败响应
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="errors">错误列表</param>
    /// <returns>失败响应</returns>
    public static ApiResponse<T> CreateFailure(string message, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors ?? new List<string>()
        };
    }

    /// <summary>
    /// 创建验证失败响应
    /// </summary>
    /// <param name="errors">验证错误列表</param>
    /// <returns>验证失败响应</returns>
    public static ApiResponse<T> CreateValidationFailure(List<string> errors)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = "输入验证失败",
            Errors = errors
        };
    }
}

/// <summary>
/// 无数据的API响应
/// </summary>
public class ApiResponse : ApiResponse<object>
{
    /// <summary>
    /// 创建成功响应
    /// </summary>
    /// <param name="message">成功消息</param>
    /// <returns>成功响应</returns>
    public static ApiResponse CreateSuccess(string message = "操作成功")
    {
        return new ApiResponse
        {
            Success = true,
            Message = message
        };
    }

    /// <summary>
    /// 创建失败响应
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="errors">错误列表</param>
    /// <returns>失败响应</returns>
    public static new ApiResponse CreateFailure(string message, List<string>? errors = null)
    {
        return new ApiResponse
        {
            Success = false,
            Message = message,
            Errors = errors ?? new List<string>()
        };
    }
}