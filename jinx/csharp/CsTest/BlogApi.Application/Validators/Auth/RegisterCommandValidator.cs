using FluentValidation;
using BlogApi.Application.Commands.Auth;
using System.Text.RegularExpressions;

namespace BlogApi.Application.Validators.Auth;

/// <summary>
/// 用户注册命令验证器
/// </summary>
public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("用户名不能为空")
            .Length(3, 50).WithMessage("用户名长度必须在3-50个字符之间")
            .Matches(@"^[a-zA-Z0-9_\u4e00-\u9fa5]+$").WithMessage("用户名只能包含字母、数字、下划线和中文字符")
            .Must(NotContainSpecialWords).WithMessage("用户名不能包含敏感词汇");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("邮箱不能为空")
            .EmailAddress().WithMessage("邮箱格式不正确")
            .MaximumLength(100).WithMessage("邮箱长度不能超过100个字符")
            .Must(BeValidEmailDomain).WithMessage("邮箱域名不在允许列表中");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("密码不能为空")
            .Length(8, 100).WithMessage("密码长度必须在8-100个字符之间")
            .Must(BeStrongPassword).WithMessage("密码必须包含至少一个大写字母、一个小写字母、一个数字和一个特殊字符");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("确认密码不能为空")
            .Equal(x => x.Password).WithMessage("密码和确认密码不匹配");
    }

    private bool NotContainSpecialWords(string username)
    {
        var forbiddenWords = new[] { "admin", "root", "system", "test", "guest", "null", "undefined" };
        return !forbiddenWords.Any(word => username.ToLowerInvariant().Contains(word));
    }

    private bool BeValidEmailDomain(string email)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains('@'))
            return false;

        var domain = email.Split('@')[1].ToLowerInvariant();
        
        // 允许常见的邮箱域名，可以根据需要扩展
        var allowedDomains = new[]
        {
            "gmail.com", "outlook.com", "hotmail.com", "yahoo.com", "qq.com", 
            "163.com", "126.com", "sina.com", "sohu.com", "foxmail.com"
        };

        // 如果是企业邮箱（包含多个点），也允许
        if (domain.Count(c => c == '.') > 1)
            return true;

        return allowedDomains.Contains(domain);
    }

    private bool BeStrongPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            return false;

        // 至少包含一个大写字母
        if (!Regex.IsMatch(password, @"[A-Z]"))
            return false;

        // 至少包含一个小写字母
        if (!Regex.IsMatch(password, @"[a-z]"))
            return false;

        // 至少包含一个数字
        if (!Regex.IsMatch(password, @"\d"))
            return false;

        // 至少包含一个特殊字符
        if (!Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]"))
            return false;

        // 不能包含常见的弱密码模式
        var weakPatterns = new[]
        {
            @"123456", @"password", @"qwerty", @"abc123", @"admin",
            @"(.)\1{2,}", // 连续相同字符
            @"012345|123456|234567|345678|456789|567890", // 连续数字
            @"abcdef|bcdefg|cdefgh|defghi|efghij|fghijk" // 连续字母
        };

        return !weakPatterns.Any(pattern => Regex.IsMatch(password.ToLowerInvariant(), pattern));
    }
}