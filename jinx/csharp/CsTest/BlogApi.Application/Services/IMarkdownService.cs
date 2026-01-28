namespace BlogApi.Application.Services;

/// <summary>
/// Markdown处理服务接口
/// </summary>
public interface IMarkdownService
{
    /// <summary>
    /// 将Markdown内容转换为HTML
    /// </summary>
    /// <param name="markdown">Markdown内容</param>
    /// <returns>HTML内容</returns>
    string ConvertToHtml(string markdown);

    /// <summary>
    /// 清理和安全化Markdown内容
    /// </summary>
    /// <param name="markdown">原始Markdown内容</param>
    /// <returns>清理后的安全Markdown内容</returns>
    string SanitizeMarkdown(string markdown);

    /// <summary>
    /// 验证Markdown内容是否安全
    /// </summary>
    /// <param name="markdown">Markdown内容</param>
    /// <returns>验证结果</returns>
    bool IsMarkdownSafe(string markdown);

    /// <summary>
    /// 从Markdown内容中提取纯文本（用于摘要生成）
    /// </summary>
    /// <param name="markdown">Markdown内容</param>
    /// <param name="maxLength">最大长度</param>
    /// <returns>纯文本内容</returns>
    string ExtractPlainText(string markdown, int maxLength = 200);

    /// <summary>
    /// 从Markdown内容中提取标题列表（用于目录生成）
    /// </summary>
    /// <param name="markdown">Markdown内容</param>
    /// <returns>标题列表</returns>
    List<MarkdownHeading> ExtractHeadings(string markdown);

    /// <summary>
    /// 验证Markdown语法是否正确
    /// </summary>
    /// <param name="markdown">Markdown内容</param>
    /// <returns>验证结果和错误信息</returns>
    MarkdownValidationResult ValidateMarkdown(string markdown);
}

/// <summary>
/// Markdown标题信息
/// </summary>
public class MarkdownHeading
{
    /// <summary>
    /// 标题级别（1-6）
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// 标题文本
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// 标题锚点ID
    /// </summary>
    public string AnchorId { get; set; } = string.Empty;
}

/// <summary>
/// Markdown验证结果
/// </summary>
public class MarkdownValidationResult
{
    /// <summary>
    /// 是否有效
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 错误信息列表
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// 警告信息列表
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}