using Microsoft.Extensions.Logging;
using CsPlaywrightXun.src.playwright.Core.Base;
using CsPlaywrightXun.src.docs.examples;

namespace CsPlaywrightXun.src.docs.examples
{
    /// <summary>
    /// 搜索业务流程示例，展示 Flow 模式的最佳实践
    /// </summary>
    public class SearchFlow : BaseFlow
    {
        private readonly ExamplePage _examplePage;
        
        public SearchFlow(ExamplePage examplePage, ILogger logger) : base(logger)
        {
            _examplePage = examplePage ?? throw new ArgumentNullException(nameof(examplePage));
        }
        
        /// <summary>
        /// 执行基本搜索流程
        /// </summary>
        /// <param name="parameters">流程参数</param>
        public override async Task ExecuteAsync(Dictionary<string, object> parameters = null)
        {
            // 验证必需参数
            ValidateParameters(parameters, "searchTerm");
            
            var searchTerm = GetParameter<string>(parameters, "searchTerm");
            var expectedMinResults = GetParameter<int>(parameters, "expectedMinResults", 0);
            var applyFilter = GetParameter<string>(parameters, "filter", null);
            
            Logger.LogInformation($"开始执行搜索流程，关键词：{searchTerm}");
            
            try
            {
                // 步骤1：执行搜索
                await _examplePage.SearchAsync(searchTerm);
                
                // 步骤2：等待结果加载
                await _examplePage.WaitForSearchResultsAsync();
                
                // 步骤3：验证结果数量（如果指定了期望值）
                if (expectedMinResults > 0)
                {
                    var resultCount = await _examplePage.GetSearchResultCountAsync();
                    if (resultCount < expectedMinResults)
                    {
                        Logger.LogWarning($"搜索结果不足，期望至少 {expectedMinResults} 个，实际 {resultCount} 个");
                    }
                    else
                    {
                        Logger.LogInformation($"搜索结果符合期望，找到 {resultCount} 个结果");
                    }
                }
                
                // 步骤4：应用过滤器（如果指定）
                if (!string.IsNullOrEmpty(applyFilter))
                {
                    await _examplePage.ApplyFilterAsync(applyFilter);
                    Logger.LogInformation($"已应用过滤器：{applyFilter}");
                }
                
                Logger.LogInformation("搜索流程执行完成");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"搜索流程执行失败：{searchTerm}");
                throw;
            }
        }
        
        /// <summary>
        /// 执行高级搜索流程（使用强类型参数）
        /// </summary>
        /// <param name="searchParameters">搜索参数</param>
        public async Task ExecuteAdvancedSearchAsync(AdvancedSearchParameters searchParameters)
        {
            if (searchParameters == null)
                throw new ArgumentNullException(nameof(searchParameters));
            
            Logger.LogInformation($"开始执行高级搜索流程：{searchParameters.SearchTerm}");
            
            try
            {
                // 步骤1：基本搜索
                await _examplePage.SearchAsync(searchParameters.SearchTerm);
                await _examplePage.WaitForSearchResultsAsync();
                
                // 步骤2：应用多个过滤器
                if (searchParameters.Filters?.Any() == true)
                {
                    foreach (var filter in searchParameters.Filters)
                    {
                        await _examplePage.ApplyFilterAsync(filter);
                        await _examplePage.WaitForSearchResultsAsync();
                        Logger.LogInformation($"已应用过滤器：{filter}");
                    }
                }
                
                // 步骤3：验证结果质量
                if (searchParameters.ValidateResults)
                {
                    await ValidateSearchResultsAsync(searchParameters);
                }
                
                // 步骤4：收集结果统计
                if (searchParameters.CollectStatistics)
                {
                    await CollectSearchStatisticsAsync(searchParameters);
                }
                
                Logger.LogInformation("高级搜索流程执行完成");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"高级搜索流程执行失败：{searchParameters.SearchTerm}");
                throw;
            }
        }
        
        /// <summary>
        /// 执行搜索结果验证流程
        /// </summary>
        /// <param name="searchParameters">搜索参数</param>
        private async Task ValidateSearchResultsAsync(AdvancedSearchParameters searchParameters)
        {
            Logger.LogInformation("开始验证搜索结果质量");
            
            var results = await _examplePage.GetSearchResultsAsync();
            var validationErrors = new List<string>();
            
            // 验证结果数量
            if (searchParameters.MinExpectedResults > 0 && results.Count < searchParameters.MinExpectedResults)
            {
                validationErrors.Add($"结果数量不足：期望至少 {searchParameters.MinExpectedResults}，实际 {results.Count}");
            }
            
            if (searchParameters.MaxExpectedResults > 0 && results.Count > searchParameters.MaxExpectedResults)
            {
                validationErrors.Add($"结果数量过多：期望最多 {searchParameters.MaxExpectedResults}，实际 {results.Count}");
            }
            
            // 验证结果内容
            foreach (var result in results)
            {
                if (string.IsNullOrWhiteSpace(result.Title))
                {
                    validationErrors.Add("发现空标题的搜索结果");
                }
                
                if (string.IsNullOrWhiteSpace(result.Description))
                {
                    validationErrors.Add($"搜索结果缺少描述：{result.Title}");
                }
                
                // 验证关键词相关性
                if (searchParameters.ValidateRelevance)
                {
                    var isRelevant = result.Title.Contains(searchParameters.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                                   result.Description.Contains(searchParameters.SearchTerm, StringComparison.OrdinalIgnoreCase);
                    
                    if (!isRelevant)
                    {
                        validationErrors.Add($"搜索结果与关键词不相关：{result.Title}");
                    }
                }
            }
            
            // 记录验证结果
            if (validationErrors.Any())
            {
                Logger.LogWarning($"搜索结果验证发现 {validationErrors.Count} 个问题：");
                foreach (var error in validationErrors)
                {
                    Logger.LogWarning($"- {error}");
                }
                
                if (searchParameters.FailOnValidationErrors)
                {
                    throw new InvalidOperationException($"搜索结果验证失败：{string.Join("; ", validationErrors)}");
                }
            }
            else
            {
                Logger.LogInformation("搜索结果验证通过");
            }
        }
        
        /// <summary>
        /// 收集搜索统计信息
        /// </summary>
        /// <param name="searchParameters">搜索参数</param>
        private async Task CollectSearchStatisticsAsync(AdvancedSearchParameters searchParameters)
        {
            Logger.LogInformation("开始收集搜索统计信息");
            
            try
            {
                var resultCount = await _examplePage.GetSearchResultCountAsync();
                var hasResults = await _examplePage.HasSearchResultsAsync();
                var hasErrors = await _examplePage.HasErrorMessageAsync();
                
                var statistics = new SearchStatistics
                {
                    SearchTerm = searchParameters.SearchTerm,
                    ResultCount = resultCount,
                    HasResults = hasResults,
                    HasErrors = hasErrors,
                    FiltersApplied = searchParameters.Filters?.ToList() ?? new List<string>(),
                    SearchTimestamp = DateTime.Now
                };
                
                // 存储统计信息（这里可以扩展为保存到数据库或文件）
                searchParameters.Statistics = statistics;
                
                Logger.LogInformation($"搜索统计信息：结果数量={resultCount}, 有结果={hasResults}, 有错误={hasErrors}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "收集搜索统计信息失败");
            }
        }
    }
    
    /// <summary>
    /// 用户注册业务流程示例
    /// </summary>
    public class UserRegistrationFlow : BaseFlow
    {
        private readonly RegistrationPage _registrationPage;
        private readonly EmailVerificationPage _emailVerificationPage;
        private readonly WelcomePage _welcomePage;
        
        public UserRegistrationFlow(
            RegistrationPage registrationPage,
            EmailVerificationPage emailVerificationPage,
            WelcomePage welcomePage,
            ILogger logger) : base(logger)
        {
            _registrationPage = registrationPage ?? throw new ArgumentNullException(nameof(registrationPage));
            _emailVerificationPage = emailVerificationPage ?? throw new ArgumentNullException(nameof(emailVerificationPage));
            _welcomePage = welcomePage ?? throw new ArgumentNullException(nameof(welcomePage));
        }
        
        public override async Task ExecuteAsync(Dictionary<string, object> parameters = null)
        {
            // 验证必需参数
            ValidateParameters(parameters, "email", "password", "firstName", "lastName");
            
            var registrationData = new UserRegistrationData
            {
                Email = GetParameter<string>(parameters, "email"),
                Password = GetParameter<string>(parameters, "password"),
                FirstName = GetParameter<string>(parameters, "firstName"),
                LastName = GetParameter<string>(parameters, "lastName"),
                PhoneNumber = GetParameter<string>(parameters, "phoneNumber", null),
                AcceptTerms = GetParameter<bool>(parameters, "acceptTerms", true),
                SubscribeNewsletter = GetParameter<bool>(parameters, "subscribeNewsletter", false)
            };
            
            Logger.LogInformation($"开始用户注册流程：{registrationData.Email}");
            
            try
            {
                // 步骤1：填写注册表单
                await _registrationPage.FillRegistrationFormAsync(registrationData);
                await _registrationPage.SubmitRegistrationAsync();
                
                // 步骤2：处理邮箱验证（如果需要）
                var requiresEmailVerification = GetParameter<bool>(parameters, "requiresEmailVerification", false);
                if (requiresEmailVerification)
                {
                    await _emailVerificationPage.WaitForLoadAsync();
                    
                    var verificationCode = GetParameter<string>(parameters, "verificationCode", null);
                    if (!string.IsNullOrEmpty(verificationCode))
                    {
                        await _emailVerificationPage.EnterVerificationCodeAsync(verificationCode);
                        await _emailVerificationPage.VerifyEmailAsync();
                    }
                }
                
                // 步骤3：确认注册成功
                await _welcomePage.WaitForLoadAsync();
                var welcomeMessage = await _welcomePage.GetWelcomeMessageAsync();
                
                Logger.LogInformation($"用户注册流程完成：{registrationData.Email}，欢迎消息：{welcomeMessage}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"用户注册流程失败：{registrationData.Email}");
                throw;
            }
        }
    }
    
    /// <summary>
    /// 复合业务流程示例 - 完整的用户入职流程
    /// </summary>
    public class CompleteUserOnboardingFlow : BaseFlow
    {
        private readonly UserRegistrationFlow _registrationFlow;
        private readonly ProfileSetupFlow _profileSetupFlow;
        private readonly TutorialFlow _tutorialFlow;
        
        public CompleteUserOnboardingFlow(
            UserRegistrationFlow registrationFlow,
            ProfileSetupFlow profileSetupFlow,
            TutorialFlow tutorialFlow,
            ILogger logger) : base(logger)
        {
            _registrationFlow = registrationFlow ?? throw new ArgumentNullException(nameof(registrationFlow));
            _profileSetupFlow = profileSetupFlow ?? throw new ArgumentNullException(nameof(profileSetupFlow));
            _tutorialFlow = tutorialFlow ?? throw new ArgumentNullException(nameof(tutorialFlow));
        }
        
        public override async Task ExecuteAsync(Dictionary<string, object> parameters = null)
        {
            Logger.LogInformation("开始完整的用户入职流程");
            
            var startTime = DateTime.Now;
            var completedSteps = new List<string>();
            
            try
            {
                // 步骤1：用户注册
                Logger.LogInformation("执行用户注册步骤");
                await _registrationFlow.ExecuteAsync(parameters);
                completedSteps.Add("用户注册");
                
                // 步骤2：个人资料设置
                var skipProfileSetup = GetParameter<bool>(parameters, "skipProfileSetup", false);
                if (!skipProfileSetup)
                {
                    Logger.LogInformation("执行个人资料设置步骤");
                    await _profileSetupFlow.ExecuteAsync(parameters);
                    completedSteps.Add("个人资料设置");
                }
                
                // 步骤3：新手教程
                var skipTutorial = GetParameter<bool>(parameters, "skipTutorial", false);
                if (!skipTutorial)
                {
                    Logger.LogInformation("执行新手教程步骤");
                    await _tutorialFlow.ExecuteAsync(parameters);
                    completedSteps.Add("新手教程");
                }
                
                var duration = DateTime.Now - startTime;
                Logger.LogInformation($"用户入职流程完成，耗时：{duration.TotalSeconds:F2}秒，完成步骤：{string.Join(", ", completedSteps)}");
            }
            catch (Exception ex)
            {
                var duration = DateTime.Now - startTime;
                Logger.LogError(ex, $"用户入职流程失败，耗时：{duration.TotalSeconds:F2}秒，已完成步骤：{string.Join(", ", completedSteps)}");
                throw;
            }
        }
    }
    
    #region 数据模型
    
    /// <summary>
    /// 高级搜索参数
    /// </summary>
    public class AdvancedSearchParameters
    {
        public string SearchTerm { get; set; } = string.Empty;
        public IEnumerable<string> Filters { get; set; } = new List<string>();
        public int MinExpectedResults { get; set; } = 0;
        public int MaxExpectedResults { get; set; } = 0;
        public bool ValidateResults { get; set; } = false;
        public bool ValidateRelevance { get; set; } = false;
        public bool FailOnValidationErrors { get; set; } = false;
        public bool CollectStatistics { get; set; } = false;
        public SearchStatistics Statistics { get; set; }
    }
    
    /// <summary>
    /// 搜索统计信息
    /// </summary>
    public class SearchStatistics
    {
        public string SearchTerm { get; set; } = string.Empty;
        public int ResultCount { get; set; }
        public bool HasResults { get; set; }
        public bool HasErrors { get; set; }
        public List<string> FiltersApplied { get; set; } = new();
        public DateTime SearchTimestamp { get; set; }
        public TimeSpan SearchDuration { get; set; }
    }
    
    /// <summary>
    /// 用户注册数据
    /// </summary>
    public class UserRegistrationData
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool AcceptTerms { get; set; } = true;
        public bool SubscribeNewsletter { get; set; } = false;
        public DateTime DateOfBirth { get; set; }
        public string Country { get; set; } = string.Empty;
        public string PreferredLanguage { get; set; } = "zh-CN";
    }
    
    #endregion
    
    #region 占位符页面类（实际项目中需要实现）
    
    /// <summary>
    /// 注册页面占位符
    /// </summary>
    public class RegistrationPage
    {
        public async Task FillRegistrationFormAsync(UserRegistrationData data) { /* 实现注册表单填写 */ }
        public async Task SubmitRegistrationAsync() { /* 实现注册提交 */ }
    }
    
    /// <summary>
    /// 邮箱验证页面占位符
    /// </summary>
    public class EmailVerificationPage
    {
        public async Task WaitForLoadAsync() { /* 实现页面加载等待 */ }
        public async Task EnterVerificationCodeAsync(string code) { /* 实现验证码输入 */ }
        public async Task VerifyEmailAsync() { /* 实现邮箱验证 */ }
    }
    
    /// <summary>
    /// 欢迎页面占位符
    /// </summary>
    public class WelcomePage
    {
        public async Task WaitForLoadAsync() { /* 实现页面加载等待 */ }
        public async Task<string> GetWelcomeMessageAsync() { return "欢迎！"; }
    }
    
    /// <summary>
    /// 个人资料设置流程占位符
    /// </summary>
    public class ProfileSetupFlow : BaseFlow
    {
        public ProfileSetupFlow(ILogger logger) : base(logger) { }
        public override async Task ExecuteAsync(Dictionary<string, object> parameters = null) { /* 实现个人资料设置流程 */ }
    }
    
    /// <summary>
    /// 新手教程流程占位符
    /// </summary>
    public class TutorialFlow : BaseFlow
    {
        public TutorialFlow(ILogger logger) : base(logger) { }
        public override async Task ExecuteAsync(Dictionary<string, object> parameters = null) { /* 实现新手教程流程 */ }
    }
    
    #endregion
}