using System.Text.Json;

namespace CsPlaywrightApi.src.playwright.Core.Config
{
    /// <summary>
    /// 运行环境枚举
    /// </summary>
    public enum Environment
    {
        Development,    // 开发环境
        Test,          // 测试环境
        Staging,       // 预发布环境
        Production     // 生产环境
    }

    /// <summary>
    /// 应用程序配置管理类
    /// </summary>
    public class AppSettings
    {
        private static AppSettings? _instance;
        private static readonly object _lock = new();

        /// <summary>
        /// 获取配置单例
        /// </summary>
        public static AppSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new AppSettings();
                    }
                }
                return _instance;
            }
        }

        private AppSettings()
        {
            // 从环境变量读取当前环境，默认为开发环境
            var envString = System.Environment.GetEnvironmentVariable("ENV") ?? "Development";
            CurrentEnvironment = Enum.TryParse<Environment>(envString, true, out var env) 
                ? env 
                : Environment.Development;

            // 初始化路径
            InitializePaths();
            
            // 加载环境配置
            LoadEnvironmentConfig();
        }

        #region 环境配置

        /// <summary>
        /// 当前运行环境
        /// </summary>
        public Environment CurrentEnvironment { get; }

        /// <summary>
        /// 环境配置
        /// </summary>
        public EnvironmentConfig Config { get; private set; } = new();

        #endregion

        #region 路径配置

        /// <summary>
        /// 项目根目录
        /// </summary>
        public string BaseDirectory { get; private set; } = string.Empty;

        /// <summary>
        /// 源代码目录
        /// </summary>
        public string SrcDirectory { get; private set; } = string.Empty;

        /// <summary>
        /// Playwright 目录
        /// </summary>
        public string PlaywrightDirectory { get; private set; } = string.Empty;

        /// <summary>
        /// 输出目录
        /// </summary>
        public string OutputDirectory { get; private set; } = string.Empty;

        /// <summary>
        /// 日志目录
        /// </summary>
        public string LogDirectory { get; private set; } = string.Empty;

        /// <summary>
        /// 截图目录
        /// </summary>
        public string ScreenshotDirectory { get; private set; } = string.Empty;

        /// <summary>
        /// 配置文件目录
        /// </summary>
        public string ConfigDirectory { get; private set; } = string.Empty;

        /// <summary>
        /// 测试用例目录
        /// </summary>
        public string CaseDirectory { get; private set; } = string.Empty;

        #endregion

        #region Playwright 配置

        /// <summary>
        /// 浏览器类型
        /// </summary>
        public string BrowserType { get; set; } = "chromium";

        /// <summary>
        /// 视口宽度
        /// </summary>
        public int ViewportWidth { get; set; } = 1920;

        /// <summary>
        /// 视口高度
        /// </summary>
        public int ViewportHeight { get; set; } = 1080;

        /// <summary>
        /// 浏览器启动参数
        /// </summary>
        public string[] BrowserArgs { get; set; } = 
        [
            "--disable-blink-features=AutomationControlled",
            "--disable-dev-shm-usage",
            "--no-sandbox"
        ];

        #endregion

        #region 日志配置

        /// <summary>
        /// 是否启用控制台日志
        /// </summary>
        public bool EnableConsoleLog { get; set; } = true;

        /// <summary>
        /// 失败时是否截图
        /// </summary>
        public bool ScreenshotOnFailure { get; set; } = true;

        /// <summary>
        /// 是否全页面截图
        /// </summary>
        public bool FullPageScreenshot { get; set; } = true;

        #endregion

        #region 重试配置

        /// <summary>
        /// 最大重试次数
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// 重试延迟（秒）
        /// </summary>
        public int RetryDelay { get; set; } = 2;

        /// <summary>
        /// 需要重试的 HTTP 状态码
        /// </summary>
        public int[] RetryStatusCodes { get; set; } = [500, 502, 503, 504];

        #endregion

        /// <summary>
        /// 初始化路径配置
        /// </summary>
        private void InitializePaths()
        {
            // 获取项目根目录（向上查找到包含 .csproj 的目录）
            BaseDirectory = FindProjectRoot(AppContext.BaseDirectory);
            
            SrcDirectory = Path.Combine(BaseDirectory, "src");
            PlaywrightDirectory = Path.Combine(SrcDirectory, "playwright");
            OutputDirectory = Path.Combine(SrcDirectory, "output");
            LogDirectory = Path.Combine(OutputDirectory, "logs");
            ScreenshotDirectory = Path.Combine(OutputDirectory, "screenshots");
            ConfigDirectory = Path.Combine(BaseDirectory, "config");
            CaseDirectory = Path.Combine(BaseDirectory, "case");

            // 确保必要的目录存在
            EnsureDirectoryExists(OutputDirectory);
            EnsureDirectoryExists(LogDirectory);
            EnsureDirectoryExists(ScreenshotDirectory);
        }

        /// <summary>
        /// 查找项目根目录
        /// </summary>
        private static string FindProjectRoot(string startPath)
        {
            var directory = new DirectoryInfo(startPath);
            
            while (directory != null)
            {
                // 查找 .csproj 或 .sln 文件
                if (directory.GetFiles("*.csproj").Length > 0 || 
                    directory.GetFiles("*.sln*").Length > 0)
                {
                    return directory.FullName;
                }
                
                directory = directory.Parent;
            }
            
            // 如果找不到，返回当前目录
            return startPath;
        }

        /// <summary>
        /// 确保目录存在
        /// </summary>
        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        /// <summary>
        /// 加载环境配置
        /// </summary>
        private void LoadEnvironmentConfig()
        {
            Config = CurrentEnvironment switch
            {
                Environment.Development => new EnvironmentConfig
                {
                    BaseUrl = "https://www.ast1001.com",
                    Timeout = 30,
                    Headless = false,
                    SlowMo = 100,
                    LogLevel = "DEBUG"
                },
                Environment.Test => new EnvironmentConfig
                {
                    BaseUrl = "http://test.example.com",
                    Timeout = 30,
                    Headless = false,
                    SlowMo = 50,
                    LogLevel = "INFO"
                },
                Environment.Staging => new EnvironmentConfig
                {
                    BaseUrl = "http://staging.example.com",
                    Timeout = 20,
                    Headless = true,
                    SlowMo = 0,
                    LogLevel = "INFO"
                },
                Environment.Production => new EnvironmentConfig
                {
                    BaseUrl = "http://prod.example.com",
                    Timeout = 15,
                    Headless = true,
                    SlowMo = 0,
                    LogLevel = "WARNING"
                },
                _ => throw new ArgumentException($"未知的环境: {CurrentEnvironment}")
            };

            // 从环境变量读取浏览器类型
            BrowserType = System.Environment.GetEnvironmentVariable("BROWSER") ?? "chromium";
        }

        /// <summary>
        /// 打印配置信息
        /// </summary>
        public void PrintConfigInfo()
        {
            Console.WriteLine(@"
              ⠰⢷⢿⠄
              ⠀⠀⠀⠀⠀⣼⣷⣄
              ⠀⠀⣤⣿⣇⣿⣿⣧⣿⡄
              ⢴⠾⠋⠀⠀⠻⣿⣷⣿⣿⡀
              🏀⢀⣿⣿⡿⢿⠈⣿
              ⠀⠀⠀⢠⣿⡿⠁⠀⡊⠀⠙
              ⠀⠀⠀⢿⣿⠀⠀⠹⣿
              ⠀⠀⠀⠀⠹⣷⡀⠀⣿⡄
              ⠀⠀⠀⠀⣀⣼⣿⠀⢈⣧.
");
            Console.WriteLine(new string('=', 80));
            Console.WriteLine($"当前环境: {CurrentEnvironment}");
            Console.WriteLine($"项目根目录: {BaseDirectory}");
            Console.WriteLine($"日志目录: {LogDirectory}");
            Console.WriteLine($"输出目录: {OutputDirectory}");
            Console.WriteLine($"截图目录: {ScreenshotDirectory}");
            Console.WriteLine($"Base URL: {Config.BaseUrl}");
            Console.WriteLine($"浏览器类型: {BrowserType}");
            Console.WriteLine($"无头模式: {Config.Headless}");
            Console.WriteLine($"超时时间: {Config.Timeout}秒");
            Console.WriteLine($"日志级别: {Config.LogLevel}");
            Console.WriteLine(new string('=', 80));
        }

        /// <summary>
        /// 获取日志文件路径（按日期）
        /// </summary>
        public string GetLogFilePath(string? sessionTimestamp = null)
        {
            var timestamp = sessionTimestamp ?? DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var sessionDir = Path.Combine(LogDirectory, timestamp);
            EnsureDirectoryExists(sessionDir);
            return Path.Combine(sessionDir, "api_log.log");
        }

        /// <summary>
        /// 获取 HTML 报告路径
        /// </summary>
        public string GetHtmlReportPath(string? sessionTimestamp = null)
        {
            var timestamp = sessionTimestamp ?? DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var sessionDir = Path.Combine(LogDirectory, timestamp);
            EnsureDirectoryExists(sessionDir);
            return Path.Combine(sessionDir, "api_report.html");
        }

        /// <summary>
        /// 获取截图文件路径
        /// </summary>
        public string GetScreenshotPath(string testName)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"{testName}_{timestamp}.png";
            return Path.Combine(ScreenshotDirectory, fileName);
        }
    }

    /// <summary>
    /// 环境配置类
    /// </summary>
    public class EnvironmentConfig
    {
        /// <summary>
        /// 基础 URL
        /// </summary>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// 超时时间（秒）
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// 是否无头模式
        /// </summary>
        public bool Headless { get; set; }

        /// <summary>
        /// 慢动作延迟（毫秒）
        /// </summary>
        public int SlowMo { get; set; }

        /// <summary>
        /// 日志级别
        /// </summary>
        public string LogLevel { get; set; } = "INFO";
    }
}
