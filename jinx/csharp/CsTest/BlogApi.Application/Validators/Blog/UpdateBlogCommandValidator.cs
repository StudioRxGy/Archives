using FluentValidation;
using BlogApi.Application.Commands.Blog;
using BlogApi.Application.Services;

namespace BlogApi.Application.Validators.Blog;

/// <summary>
/// 更新博客命令验证器
/// </summary>
public class UpdateBlogCommandValidator : AbstractValidator<UpdateBlogCommand>
{
    private readonly IMarkdownService _markdownService;

    public UpdateBlogCommandValidator(IMarkdownService markdownService)
    {
        _markdownService = markdownService;

        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("博客ID必须大于0");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("标题不能为空")
            .Length(1, 200).WithMessage("标题长度必须在1-200个字符之间")
            .Must(NotContainDangerousContent).WithMessage("标题包含不安全的内容");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("内容不能为空")
            .MaximumLength(100000).WithMessage("内容长度不能超过100000个字符")
            .Must(BeValidMarkdown).WithMessage("Markdown内容格式不正确或包含不安全的内容");

        RuleFor(x => x.Summary)
            .MaximumLength(500).WithMessage("摘要长度不能超过500个字符")
            .Must(NotContainDangerousContent).WithMessage("摘要包含不安全的内容");

        RuleFor(x => x.Tags)
            .Must(HaveValidTags).WithMessage("标签格式不正确")
            .Must(NotHaveTooManyTags).WithMessage("标签数量不能超过10个");

        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("用户ID必须大于0");
    }

    private bool NotContainDangerousContent(string? content)
    {
        if (string.IsNullOrEmpty(content))
            return true;

        // 检查是否包含危险的HTML标签或脚本
        var dangerousPatterns = new[]
        {
            @"<script[^>]*>.*?</script>",
            @"<iframe[^>]*>.*?</iframe>",
            @"javascript:",
            @"vbscript:",
            @"data:text/html",
            @"onload\s*=",
            @"onclick\s*=",
            @"onerror\s*="
        };

        return !dangerousPatterns.Any(pattern => 
            System.Text.RegularExpressions.Regex.IsMatch(content, pattern, 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }

    private bool BeValidMarkdown(string content)
    {
        if (string.IsNullOrEmpty(content))
            return false;

        try
        {
            // 验证Markdown语法
            var validationResult = _markdownService.ValidateMarkdown(content);
            if (!validationResult.IsValid)
                return false;

            // 验证Markdown安全性
            return _markdownService.IsMarkdownSafe(content);
        }
        catch
        {
            return false;
        }
    }

    private bool HaveValidTags(List<string> tags)
    {
        if (tags == null)
            return true;

        foreach (var tag in tags)
        {
            // 标签不能为空或只包含空白字符
            if (string.IsNullOrWhiteSpace(tag))
                return false;

            // 标签长度限制
            if (tag.Length > 50)
                return false;

            // 标签只能包含字母、数字、中文字符和连字符
            if (!System.Text.RegularExpressions.Regex.IsMatch(tag, @"^[a-zA-Z0-9\u4e00-\u9fa5\-]+$"))
                return false;

            // 标签不能包含危险内容
            if (!NotContainDangerousContent(tag))
                return false;
        }

        return true;
    }

    private bool NotHaveTooManyTags(List<string> tags)
    {
        return tags == null || tags.Count <= 10;
    }
}