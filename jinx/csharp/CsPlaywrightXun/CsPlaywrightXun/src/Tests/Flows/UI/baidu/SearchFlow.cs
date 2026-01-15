using Microsoft.Extensions.Logging;
using CsPlaywrightXun.src.playwright.Core.Base;
using CsPlaywrightXun.src.playwright.Core.Interfaces;
using CsPlaywrightXun.src.playwright.Pages.UI.baidu;

namespace CsPlaywrightXun.src.playwright.Flows.UI.baidu;

/// <summary>
/// 搜索业务流程
/// </summary>
public class SearchFlow : BaseFlow
{
    private readonly HomePage _homePage;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="testFixture">测试固件</param>
    /// <param name="logger">日志记录器</param>
    public SearchFlow(ITestFixture testFixture, ILogger logger) 
        : base(testFixture, logger)
    {
        _homePage = new HomePage(testFixture.Page, logger);
    }

    /// <summary>
    /// 执行搜索流程
    /// </summary>
    /// <param name="parameters">流程参数，应包含 "searchQuery" 键，可选包含 "validateResults"、"expectedMinResults"、"useYamlConfig" 键</param>
    public override async Task ExecuteAsync(Dictionary<string, object>? parameters = null)
    {
        StartFlowExecution();

        try
        {
            // 验证参数
            ValidateStringParameter(parameters!, "searchQuery");
            
            var searchQuery = parameters!["searchQuery"].ToString()!;
            var validateResults = parameters.ContainsKey("validateResults") && Convert.ToBoolean(parameters["validateResults"]);
            var expectedMinResults = parameters.ContainsKey("expectedMinResults") ? Convert.ToInt32(parameters["expectedMinResults"]) : 0;
            var useYamlConfig = parameters.ContainsKey("useYamlConfig") && Convert.ToBoolean(parameters["useYamlConfig"]);
            var yamlFilePath = parameters.ContainsKey("yamlFilePath") ? parameters["yamlFilePath"]?.ToString() : null;
            
            _logger.LogInformation($"[{FlowName}] 搜索关键词: {searchQuery}, 验证结果: {validateResults}, 最少结果数: {expectedMinResults}");

            // 步骤1: 导航到首页
            await ExecuteStepAsync("导航到首页", async () =>
            {
                await _homePage.NavigateAsync(_testFixture.Configuration.Environment.BaseUrl);
            });

            // 步骤2: 等待页面加载
            await ExecuteStepAsync("等待页面加载", async () =>
            {
                await _homePage.WaitForLoadAsync();
            });

            // 验证页面已加载
            ValidateStep("页面加载验证", await _homePage.IsLoadedAsync(), "首页未正确加载");

            // 步骤3: 验证搜索功能可用性
            await ExecuteStepAsync("验证搜索功能可用性", async () =>
            {
                var searchBoxAvailable = await _homePage.IsSearchBoxAvailableAsync();
                var searchButtonAvailable = await _homePage.IsSearchButtonAvailableAsync();
                
                ValidateStep("搜索框可用性验证", searchBoxAvailable, "搜索框不可用");
                ValidateStep("搜索按钮可用性验证", searchButtonAvailable, "搜索按钮不可用");
            });

            // 步骤4: 执行搜索
            await ExecuteStepAsync("执行搜索操作", async () =>
            {
                if (useYamlConfig && !string.IsNullOrWhiteSpace(yamlFilePath))
                {
                    await _homePage.SearchWithYamlAsync(searchQuery, yamlFilePath);
                }
                else
                {
                    await _homePage.SearchAsync(searchQuery);
                }
            });

            // 步骤5: 等待搜索结果（如果需要验证结果）
            if (validateResults)
            {
                await ExecuteStepAsync("等待搜索结果加载", async () =>
                {
                    await _homePage.WaitForSearchResultsAsync();
                });

                // 步骤6: 验证搜索结果
                await ExecuteStepAsync("验证搜索结果", async () =>
                {
                    var resultCount = await _homePage.GetSearchResultCountAsync();
                    _logger.LogInformation($"[{FlowName}] 搜索结果数量: {resultCount}");
                    
                    ValidateStep("搜索结果数量验证", resultCount >= expectedMinResults, 
                        $"搜索结果数量不足，期望至少 {expectedMinResults} 个，实际 {resultCount} 个");
                    
                    // 记录搜索结果到执行上下文
                    if (parameters.ContainsKey("captureResults") && Convert.ToBoolean(parameters["captureResults"]))
                    {
                        var results = await _homePage.GetSearchResultsAsync();
                        _logger.LogInformation($"[{FlowName}] 搜索结果标题: {string.Join(", ", results.Take(3))}...");
                    }
                });
            }

            EndFlowExecution();
        }
        catch (Exception ex)
        {
            LogFlowExecutionFailure(ex);
            throw;
        }
    }

    /// <summary>
    /// 执行简单搜索流程（仅搜索，不验证结果）
    /// </summary>
    /// <param name="searchQuery">搜索关键词</param>
    public async Task ExecuteSimpleSearchAsync(string searchQuery)
    {
        var parameters = new Dictionary<string, object>
        {
            ["searchQuery"] = searchQuery,
            ["validateResults"] = false
        };
        
        await ExecuteAsync(parameters);
    }

    /// <summary>
    /// 执行带结果验证的搜索流程
    /// </summary>
    /// <param name="searchQuery">搜索关键词</param>
    /// <param name="expectedMinResults">期望的最少结果数</param>
    public async Task ExecuteSearchWithValidationAsync(string searchQuery, int expectedMinResults = 1)
    {
        var parameters = new Dictionary<string, object>
        {
            ["searchQuery"] = searchQuery,
            ["validateResults"] = true,
            ["expectedMinResults"] = expectedMinResults,
            ["captureResults"] = true
        };
        
        await ExecuteAsync(parameters);
    }

    /// <summary>
    /// 使用YAML配置执行搜索流程
    /// </summary>
    /// <param name="searchQuery">搜索关键词</param>
    /// <param name="yamlFilePath">YAML配置文件路径</param>
    /// <param name="validateResults">是否验证搜索结果</param>
    public async Task ExecuteYamlSearchAsync(string searchQuery, string yamlFilePath, bool validateResults = false)
    {
        var parameters = new Dictionary<string, object>
        {
            ["searchQuery"] = searchQuery,
            ["useYamlConfig"] = true,
            ["yamlFilePath"] = yamlFilePath,
            ["validateResults"] = validateResults
        };
        
        await ExecuteAsync(parameters);
    }
}