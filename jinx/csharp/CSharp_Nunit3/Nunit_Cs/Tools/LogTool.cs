using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Nunit_Cs.Config;

namespace Nunit_Cs.Tools
{
    /// <summary>
    /// 控制台日志模式枚举
    /// </summary>
    public enum ConsoleLogMode
    {
        /// <summary>
        /// 简单模式，只显示基本信息和消息
        /// </summary>
        Simple,
        
        /// <summary>
        /// 详细模式，显示完整日志信息
        /// </summary>
        Detailed,
        
        /// <summary>
        /// 彩色模式，使用不同颜色显示不同级别的日志
        /// </summary>
        Colored
    }

    /// <summary>
    /// 日志工具类，负责记录测试执行结果和详细信息
    /// </summary>
    public static class LogTool
    {
        private static readonly object _lockObj = new object();
        private static readonly string _logDirectory;
        private static readonly string _staticLogPath;
        private static bool _isInitialized = false;
        private static readonly LogFileManager _logFileManager;
        
        // 日志配置 - 类似pytest的配置
        public static LogConfig LogConfig { get; private set; } = new LogConfig();

        // 静态构造函数，确保日志目录一开始就被创建
        static LogTool()
        {
            try
            {
                // 基本路径 - 直接使用当前目录，而不是BaseDirectory
                string projectDirectory = GetProjectDirectory();
                
                // 创建日志目录 - 只使用项目根目录下的Logs
                _logDirectory = Path.Combine(projectDirectory, "Logs");
                CreateDirectoryIfNotExists(_logDirectory);
                
                // 从配置文件加载日志设置
                LoadLogConfig();
                
                // 初始化日志文件管理器
                _logFileManager = new LogFileManager(_logDirectory, LogConfig.MaxLogFiles, LogConfig.MaxLogSizeKB);
                
                // 清理旧日志文件
                _logFileManager.ManageLogFiles();
                
                // 静态日志文件路径 - 只使用日期，不使用时间戳
                _staticLogPath = GetLogFilePath();
                
                // 如果是覆盖模式，先清空文件
                if (LogConfig.OverwriteMode && File.Exists(_staticLogPath))
                {
                    File.WriteAllText(_staticLogPath, string.Empty);
                }
                
                // 写入初始化信息
                string initMessage = $"[{DateTime.Now.ToString(LogConfig.DateFormat)}] [Init] 日志系统初始化 - .NET {Environment.Version}, OS: {Environment.OSVersion}{Environment.NewLine}";
                WriteTextToFile(_staticLogPath, initMessage, true);
                
                // 输出日志路径到控制台
                Console.WriteLine($"日志文件路径: {_staticLogPath}");
                
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"初始化日志系统失败: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
        
        /// <summary>
        /// 加载日志配置
        /// </summary>
        private static void LoadLogConfig()
        {
            try
            {
                // 默认配置已在LogConfig构造函数中设置
                // 尝试从NUnit.runsettings读取配置
                
                // 尝试从环境变量读取配置
                string logLevel = Environment.GetEnvironmentVariable("LOG_LEVEL");
                if (!string.IsNullOrEmpty(logLevel))
                {
                    LogConfig.LogLevel = ParseLogLevel(logLevel);
                }
                
                string logFormat = Environment.GetEnvironmentVariable("LOG_FORMAT");
                if (!string.IsNullOrEmpty(logFormat))
                {
                    LogConfig.LogFormat = logFormat;
                }
                
                string dateFormat = Environment.GetEnvironmentVariable("LOG_DATE_FORMAT");
                if (!string.IsNullOrEmpty(dateFormat))
                {
                    LogConfig.DateFormat = dateFormat;
                }
                
                string enableConsole = Environment.GetEnvironmentVariable("LOG_CONSOLE");
                if (!string.IsNullOrEmpty(enableConsole))
                {
                    LogConfig.EnableConsoleOutput = enableConsole.Equals("true", StringComparison.OrdinalIgnoreCase);
                }
                
                // 尝试从JsonConfiguration读取配置（如果已初始化）
                try
                {
                    var logSection = AppSettings.GetSection("Logging");
                    if (logSection != null)
                    {
                        var level = logSection["LogLevel:Default"];
                        if (!string.IsNullOrEmpty(level))
                        {
                            LogConfig.LogLevel = ParseLogLevel(level);
                        }
                        
                        var format = logSection["LogFormat"];
                        if (!string.IsNullOrEmpty(format))
                        {
                            LogConfig.LogFormat = format;
                        }
                        
                        var dateFormatValue = logSection["DateFormat"];
                        if (!string.IsNullOrEmpty(dateFormatValue))
                        {
                            LogConfig.DateFormat = dateFormatValue;
                        }
                        
                        var console = logSection["Console:Enabled"];
                        if (!string.IsNullOrEmpty(console))
                        {
                            LogConfig.EnableConsoleOutput = bool.Parse(console);
                        }
                        
                        var overwriteMode = logSection["OverwriteMode"];
                        if (!string.IsNullOrEmpty(overwriteMode))
                        {
                            LogConfig.OverwriteMode = bool.Parse(overwriteMode);
                        }
                        
                        var timeStampInFileName = logSection["TimeStampInFileName"];
                        if (!string.IsNullOrEmpty(timeStampInFileName))
                        {
                            LogConfig.TimeStampInFileName = bool.Parse(timeStampInFileName);
                        }
                    }
                }
                catch
                {
                    // 配置可能尚未初始化，忽略错误
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载日志配置失败: {ex.Message}");
                // 使用默认配置继续
            }
        }
        
        /// <summary>
        /// 解析日志级别字符串
        /// </summary>
        private static LogLevel ParseLogLevel(string level)
        {
            if (string.IsNullOrEmpty(level)) return LogLevel.Info;
            
            return level.ToLower() switch
            {
                "debug" => LogLevel.Debug,
                "info" => LogLevel.Info,
                "success" => LogLevel.Success,
                "warning" => LogLevel.Warning,
                "error" => LogLevel.Error,
                "test" => LogLevel.Test,
                _ => LogLevel.Info
            };
        }
        
        /// <summary>
        /// 尝试获取项目目录，从各种可能的位置
        /// </summary>
        private static string GetProjectDirectory()
        {
            try
            {
                // 尝试从AppDomain.BaseDirectory向上查找，找到包含.csproj的目录
                string directory = AppDomain.CurrentDomain.BaseDirectory;
                
                // 向上最多查找5层
                for (int i = 0; i < 5; i++)
                {
                    if (Directory.GetFiles(directory, "*.csproj").Length > 0)
                    {
                        return directory;
                    }
                    
                    var parentDir = Directory.GetParent(directory);
                    if (parentDir == null)
                    {
                        break;
                    }
                    
                    directory = parentDir.FullName;
                }
                
                // 如果找不到项目目录，则使用当前目录
                return Directory.GetCurrentDirectory();
            }
            catch
            {
                // 任何异常都回退到当前目录
                return Directory.GetCurrentDirectory();
            }
        }
        
        /// <summary>
        /// 安全创建目录
        /// </summary>
        private static void CreateDirectoryIfNotExists(string directory)
        {
            try
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Console.WriteLine($"已创建日志目录: {directory}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"创建目录失败 {directory}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 获取日志文件路径
        /// </summary>
        private static string GetLogFilePath()
        {
            try
            {
                // 始终使用日期作为文件名
                string logFileName = $"{DateTime.Now:yyyy-MM-dd}.log";
                return Path.Combine(_logDirectory, logFileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取日志文件路径失败: {ex.Message}");
                // 回退到默认路径
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
                    "Logs", $"fallback_{DateTime.Now:yyyy-MM-dd}.log");
            }
        }
        
        /// <summary>
        /// 将文本写入日志文件
        /// </summary>
        private static void WriteTextToFile(string filePath, string content, bool isFirstWrite = false)
        {
            try
            {
                lock (_lockObj)
                {
                    // 确保目录存在
                    string directory = Path.GetDirectoryName(filePath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    
                    // 检查文件大小
                    if (File.Exists(filePath) && LogConfig.MaxLogSizeKB > 0)
                    {
                        var fileInfo = new FileInfo(filePath);
                        if (fileInfo.Length > LogConfig.MaxLogSizeKB * 1024)
                        {
                            // 如果超过大小限制，创建备份文件
                            string backupFile = Path.Combine(directory, 
                                $"{Path.GetFileNameWithoutExtension(filePath)}_{DateTime.Now:HHmmss}_backup.log");
                            File.Move(filePath, backupFile);
                        }
                    }
                    
                    // 写入内容
                    File.AppendAllText(filePath, content);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"写入日志文件失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 记录信息到日志文件
        /// </summary>
        /// <param name="message">日志信息</param>
        /// <param name="logLevel">日志级别</param>
        public static void Log(string message, LogLevel logLevel = LogLevel.Info)
        {
            try
            {
                if (!_isInitialized)
                {
                    Console.WriteLine($"[警告] 日志系统未初始化，无法记录: {message}");
                    return;
                }
                
                // 检查日志级别
                if ((int)logLevel < (int)LogConfig.LogLevel)
                {
                    return; // 级别不够，不记录
                }
                
                // 构建日志内容 - 使用配置的格式
                string timestamp = DateTime.Now.ToString(LogConfig.DateFormat);
                string logLevelStr = logLevel.ToString();
                
                // 获取调用者信息（如果需要）
                string fileName = "<unknown>";
                string methodName = "<unknown>";
                string lineNumber = "0";
                
                if (LogConfig.IncludeCallerInfo)
                {
                    try
                    {
                        System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace(true);
                        if (stackTrace.FrameCount > 1)
                        {
                            var frame = stackTrace.GetFrame(1); // 获取调用Log方法的帧
                            fileName = Path.GetFileName(frame.GetFileName() ?? "<unknown>");
                            methodName = frame.GetMethod()?.Name ?? "<unknown>";
                            lineNumber = frame.GetFileLineNumber().ToString();
                        }
                    }
                    catch
                    {
                        // 如果获取堆栈信息失败，使用默认值
                    }
                }
                
                // 使用简单的格式而不是复杂的替换
                string logContent;
                if (LogConfig.IncludeCallerInfo)
                {
                    logContent = $"{timestamp} | {logLevelStr} | {fileName} - {methodName}[line:{lineNumber}]: {message}{Environment.NewLine}";
                }
                else 
                {
                    logContent = $"{timestamp} | {logLevelStr} | {message}{Environment.NewLine}";
                }
                
                // 获取主日志文件路径
                string mainLogFile = AppSettings.LogPath;
                
                // 写入日志内容
                lock (_lockObj)
                {
                    // 确保目录存在
                    string directory = Path.GetDirectoryName(mainLogFile);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    
                    // 写入日志
                    File.AppendAllText(mainLogFile, logContent);
                    
                    // 输出到控制台（如果启用）
                    if (LogConfig.EnableConsoleOutput)
                    {
                        string consoleContent;
                        switch (LogConfig.ConsoleLogMode)
                        {
                            case ConsoleLogMode.Simple:
                                consoleContent = $"[{logLevelStr}] {message}";
                                break;
                            case ConsoleLogMode.Detailed:
                                consoleContent = logContent.TrimEnd();
                                break;
                            case ConsoleLogMode.Colored:
                                ConsoleColor originalColor = Console.ForegroundColor;
                                ConsoleColor logColor = logLevel switch
                                {
                                    LogLevel.Debug => ConsoleColor.Gray,
                                    LogLevel.Info => ConsoleColor.White,
                                    LogLevel.Success => ConsoleColor.Green,
                                    LogLevel.Warning => ConsoleColor.Yellow,
                                    LogLevel.Error => ConsoleColor.Red,
                                    LogLevel.Test => ConsoleColor.Cyan,
                                    _ => ConsoleColor.White
                                };
                                Console.ForegroundColor = logColor;
                                consoleContent = $"[{timestamp}] [{logLevelStr}] {message}";
                                Console.WriteLine(consoleContent);
                                Console.ForegroundColor = originalColor;
                                break;
                            default:
                                consoleContent = $"[{timestamp}] [{logLevelStr}] {message}";
                                break;
                        }
                        
                        if (LogConfig.ConsoleLogMode != ConsoleLogMode.Colored)
                        {
                            Console.WriteLine(consoleContent);
                        }
                    }
                    
                    // 输出到测试上下文（如果在测试环境中）
                    try
                    {
                        if (TestContext.CurrentContext != null)
                        {
                            TestContext.WriteLine(logContent.TrimEnd());
                        }
                    }
                    catch
                    {
                        // 如果TestContext不可用，忽略错误
                    }
                }
            }
            catch (Exception ex)
            {
                // 如果日志记录失败，至少输出到控制台
                Console.WriteLine($"日志记录失败: {ex.Message}");
                Console.WriteLine($"原始日志内容: [{DateTime.Now.ToString(LogConfig.DateFormat)}] [{logLevel}] {message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
        
        /// <summary>
        /// 记录测试开始信息
        /// </summary>
        /// <param name="testName">测试名称</param>
        public static void LogTestStart(string testName)
        {
            Log($"开始执行测试 >>> {testName}", LogLevel.Test);
        }
        
        /// <summary>
        /// 记录测试结束信息
        /// </summary>
        /// <param name="testName">测试名称</param>
        /// <param name="result">测试结果</param>
        /// <param name="duration">测试持续时间（毫秒）</param>
        /// <param name="message">附加信息</param>
        public static void LogTestEnd(string testName, TestStatus result, long duration, string message = null)
        {
            var resultText = result switch
            {
                TestStatus.Passed => "通过",
                TestStatus.Failed => "失败",
                TestStatus.Skipped => "跳过",
                TestStatus.Inconclusive => "未决",
                TestStatus.Warning => "警告",
                _ => "未知状态"
            };
            
            var durationText = $"{duration}ms";
            if (duration >= 1000)
            {
                durationText = $"{duration / 1000.0:F2}s";
            }
            
            var logMessage = $"测试 {testName} {resultText}，耗时 {durationText}";
            if (!string.IsNullOrEmpty(message))
            {
                logMessage += $"，详细信息: {message}";
            }
            
            var logLevel = result == TestStatus.Passed ? LogLevel.Success : LogLevel.Error;
            Log(logMessage, logLevel);
        }
        
        /// <summary>
        /// 记录异常信息
        /// </summary>
        /// <param name="ex">异常对象</param>
        /// <param name="context">上下文信息</param>
        public static void LogException(Exception ex, string context = null)
        {
            if (ex == null) return;
            
            var message = new StringBuilder();
            string contextInfo = string.IsNullOrEmpty(context) ? "" : $"[{context}] ";
            
            // 记录异常的详细信息
            message.AppendLine($"{contextInfo}异常详细信息:");
            message.AppendLine("----------------------------------------");
            message.AppendLine($"异常类型: {ex.GetType().FullName}");
            message.AppendLine($"异常消息: {ex.Message}");
            
            // 记录异常的源和目标站点（如果有）
            if (!string.IsNullOrEmpty(ex.Source))
            {
                message.AppendLine($"异常来源: {ex.Source}");
            }
            if (!string.IsNullOrEmpty(ex.TargetSite?.ToString()))
            {
                message.AppendLine($"目标方法: {ex.TargetSite}");
            }
            
            // 记录堆栈跟踪
            if (!string.IsNullOrEmpty(ex.StackTrace))
            {
                message.AppendLine("堆栈跟踪:");
                message.AppendLine(ex.StackTrace);
            }
            
            message.AppendLine("----------------------------------------");
            
            // 记录内部异常
            if (ex.InnerException != null)
            {
                message.AppendLine("内部异常信息:");
                message.AppendLine("----------------------------------------");
                message.AppendLine($"内部异常类型: {ex.InnerException.GetType().FullName}");
                message.AppendLine($"内部异常消息: {ex.InnerException.Message}");
                if (!string.IsNullOrEmpty(ex.InnerException.StackTrace))
                {
                    message.AppendLine("内部异常堆栈跟踪:");
                    message.AppendLine(ex.InnerException.StackTrace);
                }
                message.AppendLine("----------------------------------------");
            }
            
            Log(message.ToString(), LogLevel.Error);
        }

        /// <summary>
        /// 记录UI测试初始化信息
        /// </summary>
        public static void LogUiTestInit()
        {
            var asciiArt = @"
  ┌───┐   ┌───┬───┬───┬───┐ ┌───┬───┬───┬───┐ ┌───┬───┬───┬───┐ ┌───┬───┬───┐
  │Esc│   │ F1│ F2│ F3│ F4│ │ F5│ F6│ F7│ F8│ │ F9│F10│F11│F12│ │P/S│S L│P/B│  ┌┐    ┌┐    ┌┐
  └───┘   └───┴───┴───┴───┘ └───┴───┴───┴───┘ └───┴───┴───┴───┘ └───┴───┴───┘  └┘    └┘    └┘
  ┌───┬───┬───┬───┬───┬───┬───┬───┬───┬───┬───┬───┬───┬───────┐ ┌───┬───┬───┐ ┌───┬───┬───┬───┐
  │~ `│! 1│@ 2│# 3│$ 4│% 5│^ 6│& 7│* 8│( 9│) 0│_ -│+ =│ BacSp │ │Ins│Hom│PUp│ │N L│ / │ * │ - │
  ├───┴─┬─┴─┬─┴─┬─┴─┬─┴─┬─┴─┬─┴─┬─┴─┬─┴─┬─┴─┬─┴─┬─┴─┬─┴─┬─────┤ ├───┼───┼───┤ ├───┼───┼───┼───┤
  │ Tab │ Q │ W │ E │ R │ T │ Y │ U │ I │ O │ P │{ [│} ]│ | \  │ │Del│End│PDn│ │ 7 │ 8 │ 9 │   │
  ├─────┴┬──┴┬──┴┬──┴┬──┴┬──┴┬──┴┬──┴┬──┴┬──┴┬──┴┬──┴┬──┴─────┤ └───┴───┴───┘ ├───┼───┼───┤ + │
  │ Caps │ A │ S │ D │ F │ G │ H │ J │ K │ L │: ;│"" '│ Enter  │               │ 4 │ 5 │ 6 │   │
  ├──────┴─┬─┴─┬─┴─┬─┴─┬─┴─┬─┴─┬─┴─┬─┴─┬─┴─┬─┴─┬─┴─┬─┴────────┤     ┌───┐     ├───┼───┼───┼───┤
  │ Shift  │ Z │ X │ C │ V │ B │ N │ M │< ,│> .│? /│  Shift   │     │ ↑ │     │ 1 │ 2 │ 3 │   │
  ├─────┬──┴─┬─┴──┬┴───┴───┴───┴───┴───┴──┬┴───┼───┴┬────┬────┤ ┌───┼───┼───┐ ├───┴───┼───┤ E││
  │ Ctrl│    │Alt │         Space         │ Alt│    │    │Ctrl│ │ ← │ ↓ │ → │ │   0   │ . │←─┘│
  └─────┴────┴────┴───────────────────────┴────┴────┴────┴────┘ └───┴───┴───┘ └───────┴───┴───┘
";
            Log(asciiArt);
            Log("开始执行 UI 自动化测试");
            Log($"测试环境: {AppSettings.Environment}");
            Log($"测试人员: {AppSettings.Tester}");
        }

        /// <summary>
        /// 记录浏览器操作信息
        /// </summary>
        /// <param name="action">操作描述</param>
        /// <param name="duration">操作耗时（秒）</param>
        public static void LogBrowserAction(string action, double duration)
        {
            Log($"{action}, 花费 {duration:F4} 秒", LogLevel.Info);
        }

        /// <summary>
        /// 记录UI测试会话开始
        /// </summary>
        public static void LogUiTestSessionStart()
        {
            var separator = new string('=', 80);
            Log($"\n{separator}");
            // 添加Banner记录
            Log(AppSettings.Banner);
            Log($"UI测试会话开始 - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Log($"测试环境: {AppSettings.Environment}");
            Log($"测试人员: {AppSettings.Tester}");
            Log(separator);
        }

        /// <summary>
        /// 记录UI测试调试信息
        /// </summary>
        /// <param name="message">调试信息</param>
        /// <param name="methodName">方法名</param>
        public static void LogUiDebug(string message, string methodName = null)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var prefix = string.IsNullOrEmpty(methodName) ? "[DEBUG]" : $"[DEBUG][{methodName}]";
            Log($"{timestamp} {prefix} {message}", LogLevel.Debug);
        }

        /// <summary>
        /// 记录UI操作结果
        /// </summary>
        /// <param name="action">操作描述</param>
        /// <param name="isSuccess">是否成功</param>
        /// <param name="duration">操作耗时</param>
        /// <param name="error">错误信息</param>
        /// <param name="screenshotPath">截图路径</param>
        public static void LogUiAction(string action, bool isSuccess, double duration, string error = null, string screenshotPath = null)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var status = isSuccess ? "SUCCESS" : "FAIL";
            var message = new StringBuilder();
            message.AppendLine($"{timestamp} {status}==> {action}, 花费 {duration:F4} 秒");
            
            if (!isSuccess && !string.IsNullOrEmpty(error))
            {
                // 添加详细的错误信息
                message.AppendLine($"异常详细信息:");
                message.AppendLine($"----------------------------------------");
                message.AppendLine(error);
                message.AppendLine($"----------------------------------------");
                
                if (!string.IsNullOrEmpty(screenshotPath))
                {
                    message.AppendLine($"失败截图已保存: {screenshotPath}");
                }
            }
            
            Log(message.ToString(), isSuccess ? LogLevel.Success : LogLevel.Error);
        }
    }
    
    /// <summary>
    /// 日志级别枚举
    /// </summary>
    public enum LogLevel
    {
        Debug,
        Info,
        Success,
        Warning,
        Error,
        Test
    }
    
    /// <summary>
    /// 日志配置类 - 对标pytest.ini中的日志配置
    /// </summary>
    public class LogConfig
    {
        /// <summary>
        /// 日志级别，默认为Debug
        /// </summary>
        public LogLevel LogLevel { get; set; } = LogLevel.Debug;
        
        /// <summary>
        /// 日志格式
        /// </summary>
        public string LogFormat { get; set; } = "%(asctime)s | %(levelname)s | %(filename)s - %(funcName)s[line:%(lineno)d]: %(message)s";
        
        /// <summary>
        /// 日期格式
        /// </summary>
        public string DateFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
        
        /// <summary>
        /// 是否启用控制台输出
        /// </summary>
        public bool EnableConsoleOutput { get; set; } = true;
        
        /// <summary>
        /// 控制台日志模式
        /// </summary>
        public ConsoleLogMode ConsoleLogMode { get; set; } = ConsoleLogMode.Simple;
        
        /// <summary>
        /// 是否包含调用者信息
        /// </summary>
        public bool IncludeCallerInfo { get; set; } = true;
        
        /// <summary>
        /// 最大日志文件大小，单位KB，设置为0表示不限制
        /// </summary>
        public int MaxLogSizeKB { get; set; } = 0;
        
        /// <summary>
        /// 最大日志文件数量，设置为1表示只保留当天的日志
        /// </summary>
        public int MaxLogFiles { get; set; } = 1;
        
        /// <summary>
        /// 覆盖模式，设置为false使用追加模式
        /// </summary>
        public bool OverwriteMode { get; set; } = false;
        
        /// <summary>
        /// 文件名是否包含时间戳，设置为false只使用日期
        /// </summary>
        public bool TimeStampInFileName { get; set; } = false;
    }
    
    /// <summary>
    /// 日志文件管理器 - 负责日志文件的滚动和清理
    /// </summary>
    public class LogFileManager
    {
        private readonly string _logDirectory;
        private readonly int _maxLogFiles;
        private readonly int _maxFileSizeKB;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logDirectory">日志目录路径</param>
        /// <param name="maxLogFiles">最大日志文件数量，0表示不限制</param>
        /// <param name="maxFileSizeKB">最大日志文件大小，0表示不限制</param>
        public LogFileManager(string logDirectory, int maxLogFiles = 0, int maxFileSizeKB = 0)
        {
            _logDirectory = logDirectory;
            _maxLogFiles = maxLogFiles;
            _maxFileSizeKB = maxFileSizeKB;
        }
        
        /// <summary>
        /// 管理日志文件，清理过时的日志
        /// </summary>
        public void ManageLogFiles()
        {
            try
            {
                if (!Directory.Exists(_logDirectory))
                {
                    return;
                }
                
                // 获取所有日志文件
                var logFiles = Directory.GetFiles(_logDirectory, "*.log")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.LastWriteTime)
                    .ToList();
                
                // 如果没有日志文件，直接返回
                if (!logFiles.Any())
                {
                    return;
                }
                
                // 获取当前日期的日志文件名（不包含时间戳）
                string currentDateLogFile = Path.Combine(_logDirectory, $"{DateTime.Now:yyyy-MM-dd}.log");
                
                // 如果存在多个当天的日志文件（带时间戳的），合并它们
                var todayLogs = logFiles
                    .Where(f => f.LastWriteTime.Date == DateTime.Now.Date)
                    .OrderBy(f => f.LastWriteTime)
                    .ToList();
                
                if (todayLogs.Count > 1)
                {
                    // 合并所有当天的日志文件到一个文件中
                    StringBuilder mergedContent = new StringBuilder();
                    foreach (var logFile in todayLogs)
                    {
                        try
                        {
                            mergedContent.Append(File.ReadAllText(logFile.FullName));
                            // 删除已合并的文件（除了当前主日志文件）
                            if (logFile.FullName != currentDateLogFile)
                            {
                                logFile.Delete();
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"合并日志文件失败 {logFile.FullName}: {ex.Message}");
                        }
                    }
                    
                    // 写入合并后的内容
                    File.WriteAllText(currentDateLogFile, mergedContent.ToString());
                }
                
                // 重新获取日志文件列表（合并后的）
                logFiles = Directory.GetFiles(_logDirectory, "*.log")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.LastWriteTime)
                    .ToList();
                
                // 如果设置了最大文件数，删除多余的文件
                if (_maxLogFiles > 0 && logFiles.Count > _maxLogFiles)
                {
                    foreach (var file in logFiles.Skip(_maxLogFiles))
                    {
                        try
                        {
                            file.Delete();
                            Console.WriteLine($"已删除旧日志文件: {file.FullName}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"删除旧日志文件失败 {file.FullName}: {ex.Message}");
                        }
                    }
                }
                
                // 检查当前日志文件大小
                var currentLogFile = new FileInfo(currentDateLogFile);
                if (currentLogFile.Exists && _maxFileSizeKB > 0 && currentLogFile.Length > _maxFileSizeKB * 1024)
                {
                    // 如果超过大小限制，进行滚动
                    RollFile(currentDateLogFile);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"管理日志文件失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 检查文件是否需要滚动
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否需要滚动</returns>
        public bool ShouldRollFile(string filePath)
        {
            try
            {
                // 如果未设置最大文件大小，则不进行滚动
                if (_maxFileSizeKB <= 0)
                {
                    return false;
                }
                
                var fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists)
                {
                    return false;
                }
                
                // 检查文件大小是否超过限制
                return fileInfo.Length > _maxFileSizeKB * 1024;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 滚动日志文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        public void RollFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return;
                }
                
                // 获取日志文件名和目录
                string directory = Path.GetDirectoryName(filePath);
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
                string extension = Path.GetExtension(filePath);
                
                // 生成新的文件名，加上时间戳
                string newFileName = $"{fileNameWithoutExt}_{DateTime.Now:HHmmss}{extension}";
                string newFilePath = Path.Combine(directory, newFileName);
                
                // 移动文件
                File.Move(filePath, newFilePath);
                Console.WriteLine($"日志文件已滚动: {filePath} -> {newFilePath}");
                
                // 清理旧文件
                ManageLogFiles();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"滚动日志文件失败 {filePath}: {ex.Message}");
            }
        }
    }
} 