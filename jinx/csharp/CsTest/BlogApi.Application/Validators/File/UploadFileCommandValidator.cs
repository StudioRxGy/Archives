using FluentValidation;
using BlogApi.Application.Commands.File;

namespace BlogApi.Application.Validators.File;

/// <summary>
/// 文件上传命令验证器
/// </summary>
public class UploadFileCommandValidator : AbstractValidator<UploadFileCommand>
{
    // 允许的文件类型
    private static readonly HashSet<string> AllowedContentTypes = new()
    {
        "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp", "image/svg+xml",
        "application/pdf", "text/plain", "text/markdown", "text/csv",
        "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-powerpoint", "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "application/zip", "application/x-zip-compressed", "application/x-rar-compressed",
        "video/mp4", "video/avi", "video/mov", "video/wmv",
        "audio/mp3", "audio/wav", "audio/ogg"
    };

    // 危险的文件扩展名
    private static readonly HashSet<string> DangerousExtensions = new()
    {
        ".exe", ".bat", ".cmd", ".com", ".pif", ".scr", ".vbs", ".js", ".jar", ".app", ".deb", ".pkg", ".dmg",
        ".asp", ".aspx", ".php", ".jsp", ".py", ".rb", ".pl", ".sh", ".ps1", ".psm1"
    };

    // 最大文件大小 (10MB)
    private const long MaxFileSize = 10 * 1024 * 1024;

    public UploadFileCommandValidator()
    {
        RuleFor(x => x.FileStream)
            .NotNull().WithMessage("文件流不能为空")
            .Must(BeReadableStream).WithMessage("文件流不可读");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("文件名不能为空")
            .Length(1, 255).WithMessage("文件名长度必须在1-255个字符之间")
            .Must(BeValidFileName).WithMessage("文件名格式不正确")
            .Must(NotHaveDangerousExtension).WithMessage("不允许上传此类型的文件")
            .Must(NotContainDangerousCharacters).WithMessage("文件名包含不安全的字符");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("内容类型不能为空")
            .MaximumLength(100).WithMessage("内容类型长度不能超过100个字符")
            .Must(BeAllowedContentType).WithMessage("不支持的文件类型");

        RuleFor(x => x.Size)
            .GreaterThan(0).WithMessage("文件大小必须大于0")
            .LessThanOrEqualTo(MaxFileSize).WithMessage($"文件大小不能超过 {MaxFileSize / (1024 * 1024)} MB");

        RuleFor(x => x.UploadedBy)
            .GreaterThan(0).WithMessage("上传者ID必须大于0");

        // 验证文件流大小与声明大小一致
        RuleFor(x => x)
            .Must(HaveConsistentSize).WithMessage("文件实际大小与声明大小不一致");
    }

    private bool BeReadableStream(Stream? stream)
    {
        return stream != null && stream.CanRead;
    }

    private bool BeValidFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return false;

        // 文件名不能只包含空白字符
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        // 文件名必须包含扩展名
        if (!fileName.Contains('.'))
            return false;

        // 文件名不能以点开头或结尾
        if (fileName.StartsWith('.') || fileName.EndsWith('.'))
            return false;

        return true;
    }

    private bool NotHaveDangerousExtension(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return false;

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return !DangerousExtensions.Contains(extension);
    }

    private bool NotContainDangerousCharacters(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return false;

        // 检查是否包含危险字符
        var dangerousChars = new[] { '<', '>', ':', '"', '|', '?', '*', '\0' };
        if (fileName.Any(c => dangerousChars.Contains(c)))
            return false;

        // 检查是否包含控制字符
        if (fileName.Any(c => char.IsControl(c)))
            return false;

        // 检查路径遍历攻击
        if (fileName.Contains("..") || fileName.Contains("./") || fileName.Contains(".\\"))
            return false;

        return true;
    }

    private bool BeAllowedContentType(string contentType)
    {
        if (string.IsNullOrEmpty(contentType))
            return false;

        return AllowedContentTypes.Contains(contentType.ToLowerInvariant());
    }

    private bool HaveConsistentSize(UploadFileCommand command)
    {
        if (command.FileStream == null)
            return false;

        try
        {
            // 如果流支持Length属性，验证大小一致性
            if (command.FileStream.CanSeek)
            {
                return command.FileStream.Length == command.Size;
            }

            // 如果流不支持Seek，跳过此验证
            return true;
        }
        catch
        {
            // 如果无法获取流长度，跳过此验证
            return true;
        }
    }
}