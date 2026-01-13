using System.Text.RegularExpressions;
using BlogApi.Application.Services;
using Markdig;
using Markdig.Extensions.AutoIdentifiers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace BlogApi.Infrastructure.Services;

/// <summary>
/// Markdig implementation of markdown processing service
/// </summary>
public class MarkdownService : IMarkdownService
{
    private readonly MarkdownPipeline _pipeline;
    private readonly MarkdownPipeline _sanitizePipeline;

    // Dangerous HTML tags and attributes that should be removed
    private static readonly string[] DangerousTags = 
    {
        "script", "iframe", "object", "embed", "form", "input", "button", "textarea", "select", "option",
        "link", "meta", "style", "base", "applet", "frame", "frameset", "noframes", "svg", "math",
        "details", "summary", "dialog", "template", "slot"
    };

    private static readonly string[] DangerousAttributes = 
    {
        "onload", "onclick", "onmouseover", "onmouseout", "onfocus", "onblur", "onchange", "onsubmit",
        "onreset", "onselect", "onkeydown", "onkeypress", "onkeyup", "onerror", "onabort", "oncanplay",
        "oncanplaythrough", "ondurationchange", "onemptied", "onended", "onloadeddata", "onloadedmetadata",
        "onloadstart", "onpause", "onplay", "onplaying", "onprogress", "onratechange", "onseeked",
        "onseeking", "onstalled", "onsuspend", "ontimeupdate", "onvolumechange", "onwaiting",
        "javascript:", "vbscript:", "data:", "livescript:", "mocha:"
    };

    public MarkdownService()
    {
        // Configure pipeline with common extensions
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseAutoIdentifiers(AutoIdentifierOptions.GitHub)
            .UseSoftlineBreakAsHardlineBreak()
            .Build();

        // Configure sanitize pipeline with limited extensions
        _sanitizePipeline = new MarkdownPipelineBuilder()
            .UseAutoLinks()
            .UseAutoIdentifiers(AutoIdentifierOptions.GitHub)
            .Build();
    }

    /// <summary>
    /// Converts Markdown content to HTML
    /// </summary>
    /// <param name="markdown">Markdown content</param>
    /// <returns>HTML content</returns>
    public string ConvertToHtml(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        try
        {
            return Markdown.ToHtml(markdown, _pipeline);
        }
        catch (Exception)
        {
            // If conversion fails, return empty string
            return string.Empty;
        }
    }

    /// <summary>
    /// Sanitizes and cleans Markdown content for security
    /// </summary>
    /// <param name="markdown">Raw Markdown content</param>
    /// <returns>Sanitized Markdown content</returns>
    public string SanitizeMarkdown(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        // Remove dangerous HTML tags
        var sanitized = markdown;
        foreach (var tag in DangerousTags)
        {
            var pattern = $@"<\s*{tag}[^>]*>.*?<\s*/\s*{tag}\s*>";
            sanitized = Regex.Replace(sanitized, pattern, string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            
            // Also remove self-closing tags
            var selfClosingPattern = $@"<\s*{tag}[^>]*/?>";
            sanitized = Regex.Replace(sanitized, selfClosingPattern, string.Empty, RegexOptions.IgnoreCase);
        }

        // Remove dangerous attributes
        foreach (var attr in DangerousAttributes)
        {
            var pattern = $@"{attr}\s*=\s*[""'][^""']*[""']";
            sanitized = Regex.Replace(sanitized, pattern, string.Empty, RegexOptions.IgnoreCase);
        }

        // Remove javascript: and data: URLs in markdown links
        sanitized = Regex.Replace(sanitized, @"\[([^\]]*)\]\s*\(\s*(javascript|data|vbscript):[^)]*\)", 
            "[$1]()", RegexOptions.IgnoreCase);
        
        // Remove javascript: and data: URLs in HTML attributes
        sanitized = Regex.Replace(sanitized, @"(href|src)\s*=\s*[""']?\s*(javascript|data|vbscript):[^""'\s>]*[""']?", 
            string.Empty, RegexOptions.IgnoreCase);

        return sanitized.Trim();
    }

    /// <summary>
    /// Validates if Markdown content is safe
    /// </summary>
    /// <param name="markdown">Markdown content</param>
    /// <returns>True if safe, false otherwise</returns>
    public bool IsMarkdownSafe(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return true;
        }

        // Check for dangerous tags
        foreach (var tag in DangerousTags)
        {
            var pattern = $@"<\s*{tag}[^>]*>";
            if (Regex.IsMatch(markdown, pattern, RegexOptions.IgnoreCase))
            {
                return false;
            }
        }

        // Check for dangerous attributes
        foreach (var attr in DangerousAttributes)
        {
            if (markdown.Contains(attr, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // Check for dangerous URLs
        if (Regex.IsMatch(markdown, @"(href|src)\s*=\s*[""']?\s*(javascript|data|vbscript):", RegexOptions.IgnoreCase))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Extracts plain text from Markdown content for summary generation
    /// </summary>
    /// <param name="markdown">Markdown content</param>
    /// <param name="maxLength">Maximum length of extracted text</param>
    /// <returns>Plain text content</returns>
    public string ExtractPlainText(string markdown, int maxLength = 200)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        try
        {
            // Parse markdown document
            var document = Markdown.Parse(markdown, _sanitizePipeline);
            var plainText = ExtractTextFromDocument(document);

            // Clean up whitespace
            plainText = Regex.Replace(plainText, @"\s+", " ").Trim();

            // Truncate if necessary
            if (plainText.Length > maxLength)
            {
                var truncated = plainText.Substring(0, maxLength);
                var lastSpace = truncated.LastIndexOf(' ');
                if (lastSpace > maxLength * 0.8) // Only truncate at word boundary if it's not too far back
                {
                    truncated = truncated.Substring(0, lastSpace);
                }
                return truncated + "...";
            }

            return plainText;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Extracts headings from Markdown content for table of contents generation
    /// </summary>
    /// <param name="markdown">Markdown content</param>
    /// <returns>List of headings</returns>
    public List<MarkdownHeading> ExtractHeadings(string markdown)
    {
        var headings = new List<MarkdownHeading>();

        if (string.IsNullOrWhiteSpace(markdown))
        {
            return headings;
        }

        try
        {
            var document = Markdown.Parse(markdown, _pipeline);
            
            foreach (var block in document)
            {
                if (block is HeadingBlock heading)
                {
                    var text = ExtractTextFromInlines(heading.Inline);
                    var anchorId = GenerateAnchorId(text);

                    headings.Add(new MarkdownHeading
                    {
                        Level = heading.Level,
                        Text = text,
                        AnchorId = anchorId
                    });
                }
            }
        }
        catch (Exception)
        {
            // Return empty list if parsing fails
        }

        return headings;
    }

    /// <summary>
    /// Validates Markdown syntax and returns validation result
    /// </summary>
    /// <param name="markdown">Markdown content</param>
    /// <returns>Validation result with errors and warnings</returns>
    public MarkdownValidationResult ValidateMarkdown(string markdown)
    {
        var result = new MarkdownValidationResult { IsValid = true };

        if (string.IsNullOrWhiteSpace(markdown))
        {
            return result;
        }

        try
        {
            // Try to parse the markdown
            var document = Markdown.Parse(markdown, _pipeline);

            // Check for security issues
            if (!IsMarkdownSafe(markdown))
            {
                result.IsValid = false;
                result.Errors.Add("Markdown contains potentially dangerous content (scripts, unsafe HTML, etc.)");
            }

            // Check for common issues
            CheckForCommonIssues(markdown, result);

            // If we got here without exceptions, the syntax is valid
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Errors.Add($"Markdown syntax error: {ex.Message}");
        }

        return result;
    }

    private string ExtractTextFromDocument(MarkdownDocument document)
    {
        var text = new List<string>();

        foreach (var block in document)
        {
            switch (block)
            {
                case ParagraphBlock paragraph:
                    text.Add(ExtractTextFromInlines(paragraph.Inline));
                    break;
                case HeadingBlock heading:
                    text.Add(ExtractTextFromInlines(heading.Inline));
                    break;
                case QuoteBlock quote:
                    text.Add(ExtractTextFromBlocks(quote));
                    break;
                case ListBlock list:
                    foreach (var item in list)
                    {
                        if (item is ListItemBlock listItem)
                        {
                            text.Add(ExtractTextFromBlocks(listItem));
                        }
                    }
                    break;
            }
        }

        return string.Join(" ", text);
    }

    private string ExtractTextFromBlocks(ContainerBlock container)
    {
        var text = new List<string>();

        foreach (var block in container)
        {
            switch (block)
            {
                case ParagraphBlock paragraph:
                    text.Add(ExtractTextFromInlines(paragraph.Inline));
                    break;
                case HeadingBlock heading:
                    text.Add(ExtractTextFromInlines(heading.Inline));
                    break;
                case QuoteBlock quote:
                    text.Add(ExtractTextFromBlocks(quote));
                    break;
                case ListBlock list:
                    foreach (var item in list)
                    {
                        if (item is ListItemBlock listItem)
                        {
                            text.Add(ExtractTextFromBlocks(listItem));
                        }
                    }
                    break;
            }
        }

        return string.Join(" ", text);
    }

    private string ExtractTextFromInlines(ContainerInline? inlines)
    {
        if (inlines == null)
        {
            return string.Empty;
        }

        var text = new List<string>();

        foreach (var inline in inlines)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    text.Add(literal.Content.ToString());
                    break;
                case EmphasisInline emphasis:
                    text.Add(ExtractTextFromInlines(emphasis));
                    break;
                case LinkInline link:
                    text.Add(ExtractTextFromInlines(link));
                    break;
                case CodeInline code:
                    text.Add(code.Content);
                    break;
            }
        }

        return string.Join("", text);
    }

    private string GenerateAnchorId(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        // Convert to lowercase and replace spaces with hyphens
        var anchorId = text.ToLowerInvariant();
        anchorId = Regex.Replace(anchorId, @"[^\w\s-]", ""); // Remove special characters
        anchorId = Regex.Replace(anchorId, @"\s+", "-"); // Replace spaces with hyphens
        anchorId = anchorId.Trim('-'); // Remove leading/trailing hyphens

        return anchorId;
    }

    private void CheckForCommonIssues(string markdown, MarkdownValidationResult result)
    {
        // Check for unmatched brackets
        var openBrackets = markdown.Count(c => c == '[');
        var closeBrackets = markdown.Count(c => c == ']');
        if (openBrackets != closeBrackets)
        {
            result.Warnings.Add("Unmatched square brackets detected");
        }

        // Check for unmatched parentheses in links
        var openParens = markdown.Count(c => c == '(');
        var closeParens = markdown.Count(c => c == ')');
        if (openParens != closeParens)
        {
            result.Warnings.Add("Unmatched parentheses detected (may affect links)");
        }

        // Check for very long lines (readability warning)
        var lines = markdown.Split('\n');
        var longLines = lines.Where(line => line.Length > 120).Count();
        if (longLines > 0)
        {
            result.Warnings.Add($"{longLines} lines exceed 120 characters (readability concern)");
        }
    }
}