namespace BlogApi.Application.DTOs.Common;

/// <summary>
/// 验证结果
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// 是否验证成功
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 验证错误列表
    /// </summary>
    public List<ValidationError> Errors { get; set; } = new();

    /// <summary>
    /// 创建成功的验证结果
    /// </summary>
    /// <returns>成功的验证结果</returns>
    public static ValidationResult Success()
    {
        return new ValidationResult { IsValid = true };
    }

    /// <summary>
    /// 创建失败的验证结果
    /// </summary>
    /// <param name="errors">验证错误列表</param>
    /// <returns>失败的验证结果</returns>
    public static ValidationResult Failure(List<ValidationError> errors)
    {
        return new ValidationResult
        {
            IsValid = false,
            Errors = errors
        };
    }

    /// <summary>
    /// 创建失败的验证结果
    /// </summary>
    /// <param name="field">字段名</param>
    /// <param name="message">错误消息</param>
    /// <returns>失败的验证结果</returns>
    public static ValidationResult Failure(string field, string message)
    {
        return new ValidationResult
        {
            IsValid = false,
            Errors = new List<ValidationError>
            {
                new ValidationError(field, message)
            }
        };
    }
}

/// <summary>
/// 验证错误
/// </summary>
public class ValidationError
{
    /// <summary>
    /// 字段名
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// 错误消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    public ValidationError() { }

    public ValidationError(string field, string message)
    {
        Field = field;
        Message = message;
    }
}