using BlogApi.Infrastructure.Services;
using Xunit;

namespace BlogApi.Infrastructure.Tests.Services;

public class MarkdownServiceTests
{
    private readonly MarkdownService _service;

    public MarkdownServiceTests()
    {
        _service = new MarkdownService();
    }

    [Fact]
    public void ConvertToHtml_ValidMarkdown_ReturnsHtml()
    {
        // Arrange
        var markdown = "# Hello World\n\nThis is **bold** text.";

        // Act
        var html = _service.ConvertToHtml(markdown);

        // Assert
        Assert.NotNull(html);
        Assert.Contains("<h1", html);
        Assert.Contains("Hello World", html);
        Assert.Contains("<strong>bold</strong>", html);
    }

    [Fact]
    public void ConvertToHtml_EmptyMarkdown_ReturnsEmptyString()
    {
        // Act
        var html = _service.ConvertToHtml(string.Empty);

        // Assert
        Assert.Equal(string.Empty, html);
    }

    [Fact]
    public void ConvertToHtml_NullMarkdown_ReturnsEmptyString()
    {
        // Act
        var html = _service.ConvertToHtml(null!);

        // Assert
        Assert.Equal(string.Empty, html);
    }

    [Fact]
    public void SanitizeMarkdown_DangerousScript_RemovesScript()
    {
        // Arrange
        var markdown = "# Title\n\n<script>alert('xss')</script>\n\nSafe content.";

        // Act
        var sanitized = _service.SanitizeMarkdown(markdown);

        // Assert
        Assert.NotNull(sanitized);
        Assert.DoesNotContain("<script>", sanitized);
        Assert.DoesNotContain("alert('xss')", sanitized);
        Assert.Contains("# Title", sanitized);
        Assert.Contains("Safe content.", sanitized);
    }

    [Fact]
    public void SanitizeMarkdown_JavaScriptUrl_RemovesJavaScript()
    {
        // Arrange
        var markdown = "[Click me](javascript:alert('xss'))";

        // Act
        var sanitized = _service.SanitizeMarkdown(markdown);

        // Assert
        Assert.NotNull(sanitized);
        Assert.DoesNotContain("javascript:", sanitized);
        Assert.Contains("[Click me]", sanitized);
    }

    [Fact]
    public void IsMarkdownSafe_SafeMarkdown_ReturnsTrue()
    {
        // Arrange
        var markdown = "# Title\n\nThis is **safe** markdown with [links](https://example.com).";

        // Act
        var result = _service.IsMarkdownSafe(markdown);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMarkdownSafe_DangerousScript_ReturnsFalse()
    {
        // Arrange
        var markdown = "# Title\n\n<script>alert('xss')</script>";

        // Act
        var result = _service.IsMarkdownSafe(markdown);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsMarkdownSafe_JavaScriptUrl_ReturnsFalse()
    {
        // Arrange
        var markdown = "[Click me](javascript:alert('xss'))";

        // Act
        var result = _service.IsMarkdownSafe(markdown);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ExtractPlainText_MarkdownWithFormatting_ReturnsPlainText()
    {
        // Arrange
        var markdown = "# Title\n\nThis is **bold** and *italic* text with [links](https://example.com).";

        // Act
        var plainText = _service.ExtractPlainText(markdown);

        // Assert
        Assert.NotNull(plainText);
        Assert.Contains("Title", plainText);
        Assert.Contains("bold", plainText);
        Assert.Contains("italic", plainText);
        Assert.DoesNotContain("**", plainText);
        Assert.DoesNotContain("*", plainText);
        Assert.DoesNotContain("#", plainText);
        Assert.DoesNotContain("[", plainText);
        Assert.DoesNotContain("](", plainText);
    }

    [Fact]
    public void ExtractPlainText_LongText_TruncatesCorrectly()
    {
        // Arrange
        var longText = string.Join(" ", Enumerable.Repeat("word", 100));
        var markdown = $"# Title\n\n{longText}";

        // Act
        var plainText = _service.ExtractPlainText(markdown, 50);

        // Assert
        Assert.NotNull(plainText);
        Assert.True(plainText.Length <= 53); // 50 + "..."
        Assert.EndsWith("...", plainText);
    }

    [Fact]
    public void ExtractHeadings_MarkdownWithHeadings_ReturnsHeadings()
    {
        // Arrange
        var markdown = @"# Main Title
## Subtitle
### Sub-subtitle
Some content here.
## Another Section";

        // Act
        var headings = _service.ExtractHeadings(markdown);

        // Assert
        Assert.NotNull(headings);
        Assert.Equal(4, headings.Count);
        
        Assert.Equal(1, headings[0].Level);
        Assert.Equal("Main Title", headings[0].Text);
        Assert.Equal("main-title", headings[0].AnchorId);
        
        Assert.Equal(2, headings[1].Level);
        Assert.Equal("Subtitle", headings[1].Text);
        
        Assert.Equal(3, headings[2].Level);
        Assert.Equal("Sub-subtitle", headings[2].Text);
        
        Assert.Equal(2, headings[3].Level);
        Assert.Equal("Another Section", headings[3].Text);
    }

    [Fact]
    public void ValidateMarkdown_ValidMarkdown_ReturnsValid()
    {
        // Arrange
        var markdown = "# Title\n\nThis is valid markdown.";

        // Act
        var result = _service.ValidateMarkdown(markdown);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateMarkdown_DangerousContent_ReturnsInvalid()
    {
        // Arrange
        var markdown = "# Title\n\n<script>alert('xss')</script>";

        // Act
        var result = _service.ValidateMarkdown(markdown);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.Contains("dangerous content", result.Errors[0]);
    }

    [Fact]
    public void ValidateMarkdown_UnmatchedBrackets_ReturnsWarnings()
    {
        // Arrange
        var markdown = "# Title\n\nThis has [unmatched brackets.";

        // Act
        var result = _service.ValidateMarkdown(markdown);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid); // Still valid, but has warnings
        Assert.NotEmpty(result.Warnings);
        Assert.Contains("Unmatched square brackets", result.Warnings[0]);
    }
}