using FluentValidation;
using BlogApi.Application.Commands.Auth;

namespace BlogApi.Application.Validators.Auth;

/// <summary>
/// 刷新令牌命令验证器
/// </summary>
public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.AccessToken)
            .NotEmpty().WithMessage("访问令牌不能为空")
            .Must(BeValidJwtFormat).WithMessage("访问令牌格式不正确");

        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("刷新令牌不能为空")
            .Length(32, 256).WithMessage("刷新令牌长度必须在32-256个字符之间");
    }

    private bool BeValidJwtFormat(string token)
    {
        if (string.IsNullOrEmpty(token))
            return false;

        // JWT token should have 3 parts separated by dots
        var parts = token.Split('.');
        if (parts.Length != 3)
            return false;

        // Each part should be base64 encoded (basic check)
        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part))
                return false;

            // Check if it contains only valid base64 characters
            if (!System.Text.RegularExpressions.Regex.IsMatch(part, @"^[A-Za-z0-9_-]+$"))
                return false;
        }

        return true;
    }
}