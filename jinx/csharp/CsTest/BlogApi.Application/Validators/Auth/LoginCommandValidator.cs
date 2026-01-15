using FluentValidation;
using BlogApi.Application.Commands.Auth;

namespace BlogApi.Application.Validators.Auth;

/// <summary>
/// 用户登录命令验证器
/// </summary>
public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.EmailOrUsername)
            .NotEmpty().WithMessage("邮箱或用户名不能为空")
            .MaximumLength(100).WithMessage("邮箱或用户名长度不能超过100个字符")
            .Must(BeValidEmailOrUsername).WithMessage("邮箱或用户名格式不正确");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("密码不能为空")
            .MaximumLength(100).WithMessage("密码长度不能超过100个字符");
    }

    private bool BeValidEmailOrUsername(string emailOrUsername)
    {
        if (string.IsNullOrEmpty(emailOrUsername))
            return false;

        // 如果包含@符号，验证为邮箱格式
        if (emailOrUsername.Contains('@'))
        {
            return IsValidEmail(emailOrUsername);
        }

        // 否则验证为用户名格式
        return IsValidUsername(emailOrUsername);
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private bool IsValidUsername(string username)
    {
        // 用户名只能包含字母、数字、下划线和中文字符，长度3-50
        if (username.Length < 3 || username.Length > 50)
            return false;

        return System.Text.RegularExpressions.Regex.IsMatch(username, @"^[a-zA-Z0-9_\u4e00-\u9fa5]+$");
    }
}