using EnterpriseAutomationFramework.Services.Reporting;
using Xunit;

namespace EnterpriseAutomationFramework.Tests.Services;

/// <summary>
/// HTML模板提供器测试
/// </summary>
public class HtmlTemplateProviderTests
{
    private readonly HtmlTemplateProvider _templateProvider;

    public HtmlTemplateProviderTests()
    {
        _templateProvider = new HtmlTemplateProvider();
    }

    [Fact]
    public async Task GetMainTemplateAsync_ShouldReturnValidHtmlTemplate()
    {
        // Act
        var template = await _templateProvider.GetMainTemplateAsync();

        // Assert
        Assert.NotNull(template);
        Assert.NotEmpty(template);
        Assert.Contains("<!DOCTYPE html>", template);
        Assert.Contains("<html", template);
        Assert.Contains("</html>", template);
        Assert.Contains("<head>", template);
        Assert.Contains("</head>", template);
        Assert.Contains("<body>", template);
        Assert.Contains("</body>", template);
    }

    [Fact]
    public async Task GetMainTemplateAsync_ShouldContainRequiredPlaceholders()
    {
        // Act
        var template = await _templateProvider.GetMainTemplateAsync();

        // Assert
        Assert.Contains("{{REPORT_TITLE}}", template);
        Assert.Contains("{{REPORT_GENERATED_AT}}", template);
        Assert.Contains("{{ENVIRONMENT}}", template);
        Assert.Contains("{{TEST_START_TIME}}", template);
        Assert.Contains("{{TEST_END_TIME}}", template);
        Assert.Contains("{{TOTAL_DURATION}}", template);
        Assert.Contains("{{SUMMARY_SECTION}}", template);
        Assert.Contains("{{CHARTS_SECTION}}", template);
        Assert.Contains("{{RESULTS_SECTION}}", template);
        Assert.Contains("{{FAILED_TESTS_SECTION}}", template);
        Assert.Contains("{{SCREENSHOTS_SECTION}}", template);
        Assert.Contains("{{SYSTEM_INFO_SECTION}}", template);
        Assert.Contains("{{CONFIGURATION_SECTION}}", template);
        Assert.Contains("{{METADATA_SECTION}}", template);
    }

    [Fact]
    public async Task GetMainTemplateAsync_ShouldContainNavigationElements()
    {
        // Act
        var template = await _templateProvider.GetMainTemplateAsync();

        // Assert
        Assert.Contains("report-nav", template);
        Assert.Contains("nav-link", template);
        Assert.Contains("#summary", template);
        Assert.Contains("#charts", template);
        Assert.Contains("#results", template);
        Assert.Contains("#failures", template);
        Assert.Contains("#screenshots", template);
        Assert.Contains("#system", template);
    }

    [Fact]
    public async Task GetMainTemplateAsync_ShouldContainModalElements()
    {
        // Act
        var template = await _templateProvider.GetMainTemplateAsync();

        // Assert
        Assert.Contains("screenshotModal", template);
        Assert.Contains("testDetailsModal", template);
        Assert.Contains("modal-content", template);
        Assert.Contains("close", template);
    }

    [Fact]
    public async Task GetMainTemplateAsync_ShouldIncludeChartJsReference()
    {
        // Act
        var template = await _templateProvider.GetMainTemplateAsync();

        // Assert
        Assert.Contains("chart.js", template);
        Assert.Contains("cdn.jsdelivr.net", template);
    }

    [Fact]
    public async Task GetStylesheetAsync_ShouldReturnValidCss()
    {
        // Act
        var css = await _templateProvider.GetStylesheetAsync();

        // Assert
        Assert.NotNull(css);
        Assert.NotEmpty(css);
        Assert.Contains("body {", css);
        Assert.Contains(".container", css);
        Assert.Contains(".report-header", css);
        Assert.Contains(".summary-cards", css);
        Assert.Contains(".results-table", css);
    }

    [Fact]
    public async Task GetStylesheetAsync_ShouldContainResponsiveDesign()
    {
        // Act
        var css = await _templateProvider.GetStylesheetAsync();

        // Assert
        Assert.Contains("@media", css);
        Assert.Contains("max-width: 768px", css);
        Assert.Contains("grid-template-columns", css);
        Assert.Contains("responsive", css);
    }

    [Fact]
    public async Task GetStylesheetAsync_ShouldContainPrintStyles()
    {
        // Act
        var css = await _templateProvider.GetStylesheetAsync();

        // Assert
        Assert.Contains("@media print", css);
        Assert.Contains("display: none", css);
        Assert.Contains("page-break-inside: avoid", css);
    }

    [Fact]
    public async Task GetStylesheetAsync_ShouldContainStatusColors()
    {
        // Act
        var css = await _templateProvider.GetStylesheetAsync();

        // Assert
        Assert.Contains(".passed", css);
        Assert.Contains(".failed", css);
        Assert.Contains(".skipped", css);
        Assert.Contains("#28a745", css); // 绿色 - 通过
        Assert.Contains("#dc3545", css); // 红色 - 失败
        Assert.Contains("#ffc107", css); // 黄色 - 跳过
    }

    [Fact]
    public async Task GetJavaScriptAsync_ShouldReturnValidJavaScript()
    {
        // Act
        var js = await _templateProvider.GetJavaScriptAsync();

        // Assert
        Assert.NotNull(js);
        Assert.NotEmpty(js);
        Assert.Contains("document.addEventListener", js);
        Assert.Contains("DOMContentLoaded", js);
        Assert.Contains("function", js);
    }

    [Fact]
    public async Task GetJavaScriptAsync_ShouldContainRequiredFunctions()
    {
        // Act
        var js = await _templateProvider.GetJavaScriptAsync();

        // Assert
        Assert.Contains("initializeNavigation", js);
        Assert.Contains("initializeFilters", js);
        Assert.Contains("initializeModals", js);
        Assert.Contains("initializeScrollSpy", js);
        Assert.Contains("openScreenshot", js);
        Assert.Contains("showTestDetails", js);
    }

    [Fact]
    public async Task GetJavaScriptAsync_ShouldContainEventHandlers()
    {
        // Act
        var js = await _templateProvider.GetJavaScriptAsync();

        // Assert
        Assert.Contains("addEventListener", js);
        Assert.Contains("click", js);
        Assert.Contains("scroll", js);
        Assert.Contains("scrollIntoView", js);
    }

    [Fact]
    public async Task GetJavaScriptAsync_ShouldContainFilterLogic()
    {
        // Act
        var js = await _templateProvider.GetJavaScriptAsync();

        // Assert
        Assert.Contains("filter-btn", js);
        Assert.Contains("result-row", js);
        Assert.Contains("data-filter", js);
        Assert.Contains("data-status", js);
        Assert.Contains("hidden", js);
    }

    [Fact]
    public async Task GetJavaScriptAsync_ShouldContainModalLogic()
    {
        // Act
        var js = await _templateProvider.GetJavaScriptAsync();

        // Assert
        Assert.Contains("modal", js);
        Assert.Contains("display", js);
        Assert.Contains("block", js);
        Assert.Contains("none", js);
    }

    [Fact]
    public async Task GetJavaScriptAsync_ShouldContainUtilityFunctions()
    {
        // Act
        var js = await _templateProvider.GetJavaScriptAsync();

        // Assert
        Assert.Contains("formatJson", js);
        Assert.Contains("escapeHtml", js);
    }

    [Theory]
    [InlineData("GetMainTemplateAsync")]
    [InlineData("GetStylesheetAsync")]
    [InlineData("GetJavaScriptAsync")]
    public async Task TemplateProviderMethods_ShouldReturnConsistentResults(string methodName)
    {
        // Arrange
        var method = typeof(HtmlTemplateProvider).GetMethod(methodName);
        Assert.NotNull(method);

        // Act
        var result1 = await (Task<string>)method.Invoke(_templateProvider, null)!;
        var result2 = await (Task<string>)method.Invoke(_templateProvider, null)!;

        // Assert
        Assert.Equal(result1, result2);
        Assert.NotNull(result1);
        Assert.NotEmpty(result1);
    }

    [Fact]
    public async Task GetMainTemplateAsync_ShouldContainMetaViewport()
    {
        // Act
        var template = await _templateProvider.GetMainTemplateAsync();

        // Assert
        Assert.Contains("viewport", template);
        Assert.Contains("width=device-width", template);
        Assert.Contains("initial-scale=1.0", template);
    }

    [Fact]
    public async Task GetMainTemplateAsync_ShouldContainCharsetUtf8()
    {
        // Act
        var template = await _templateProvider.GetMainTemplateAsync();

        // Assert
        Assert.Contains("charset='UTF-8'", template);
    }

    [Fact]
    public async Task GetMainTemplateAsync_ShouldContainLanguageAttribute()
    {
        // Act
        var template = await _templateProvider.GetMainTemplateAsync();

        // Assert
        Assert.Contains("lang='zh-CN'", template);
    }

    [Fact]
    public async Task GetStylesheetAsync_ShouldContainFlexboxStyles()
    {
        // Act
        var css = await _templateProvider.GetStylesheetAsync();

        // Assert
        Assert.Contains("display: flex", css);
        Assert.Contains("flex-wrap", css);
        Assert.Contains("justify-content", css);
        Assert.Contains("align-items", css);
    }

    [Fact]
    public async Task GetStylesheetAsync_ShouldContainGridStyles()
    {
        // Act
        var css = await _templateProvider.GetStylesheetAsync();

        // Assert
        Assert.Contains("display: grid", css);
        Assert.Contains("grid-template-columns", css);
        Assert.Contains("gap:", css);
    }

    [Fact]
    public async Task GetStylesheetAsync_ShouldContainTransitionEffects()
    {
        // Act
        var css = await _templateProvider.GetStylesheetAsync();

        // Assert
        Assert.Contains("transition:", css);
        Assert.Contains("transform:", css);
        Assert.Contains("hover", css);
    }

    [Fact]
    public async Task GetJavaScriptAsync_ShouldContainChartConfiguration()
    {
        // Act
        var js = await _templateProvider.GetJavaScriptAsync();

        // Assert
        Assert.Contains("Chart", js);
        Assert.Contains("canvas", js);
        Assert.Contains("getContext", js);
        Assert.Contains("2d", js);
    }
}