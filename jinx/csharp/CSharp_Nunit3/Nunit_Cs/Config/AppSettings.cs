using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace Nunit_Cs.Config
{
    /// <summary>
    /// 应用程序配置类，集中管理配置
    /// </summary>
    public static class AppSettings
    {
        // 配置实例
        private static IConfiguration _configuration;

        // 项目路径
        private static readonly string BasePath = GetBasePath();

        // 显示项目banner（修复转义问题）
        public static readonly string Banner = @"
  ┌───┐   ┌───┬───┬───┬───┐ ┌───┬───┬───┬───┐ ┌───┬───┬───┬───┐ ┌───┬───┬───┐
  │Esc│   │ F1│ F2│ F3│ F4│ │ F5│ F6│ F7│ F8│ │ F9│F10│F11│F12│ │P/S│S L│P/B│  ┌┐    ┌┐    ┌┐
  └───┘   └───┴───┴───┴───┘ └───┴───┴───┴───┘ └───┴───┴───┴───┘ └───┴───┴───┘  └┘    └┘    └┘
  ┌───┬───┬───┬───┬───┬───┬───┬───┬───┬───┬───┬───┬───┬───────┐ ┌───┬───┬───┐ ┌───┬───┬───┬───┐
  │~ `│! 1│@ 2│# 3│$ 4│% 5│^ 6│& 7│* 8│( 9│) 0│_ -│+ =│ BacSp │ │Ins│Hom│PUp│ │N L│ / │ * │ - │
  ├───┴─┬─┴─┬─┴─┬─┴─┬─┴─┬─┴─┬─┴─┬─┴─┬─┴─┬─┴─┬─┴─┬─┴─┬─┴─┬─────┤ ├───┼───┼───┤ ├───┼───┼───┼───┤
  │ Tab │ Q │ W │ E │ R │ T │ Y │ U │ I │ O │ P │{ [│} ]│ | \ │ │Del│End│PDn│ │ 7 │ 8 │ 9 │   │
  ├─────┴┬──┴┬──┴┬──┴┬──┴┬──┴┬──┴┬──┴┬──┴┬──┴┬──┴┬──┴┬──┴─────┤ └───┴───┴───┘ ├───┼───┼───┤ + │
  │ Caps │ A │ S │ D │ F │ G │ H │ J │ K │ L │: ;│"" '│ Enter │               │ 4 │ 5 │ 6 │   │
  ├──────┴─┬─┴─┬─┴─┬─┴─┬─┴─┬─┴─┬─┴─┬─┴─┬─┴─┬─┴─┬─┴─┬─┴────────┤     ┌───┐     ├───┼───┼───┼───┤
  │ Shift  │ Z │ X │ C │ V │ B │ N │ M │< ,│> .│? /│  Shift   │     │ ↑ │     │ 1 │ 2 │ 3 │   │
  ├─────┬──┴─┬─┴──┬┴───┴───┴───┴───┴───┴──┬┴───┼───┴┬────┬────┤ ┌───┼───┼───┐ ├───┴───┼───┤ E││
  │ Ctrl│    │Alt │         Space         │ Alt│    │    │Ctrl│ │ ← │ ↓ │ → │ │   0   │ . │←─┘│
  └─────┴────┴────┴───────────────────────┴────┴────┴────┴────┘ └───┴───┴───┘ └───────┴───┴───┘
";


        // 文件路径
        public static readonly string ConfigPath = Path.Combine(BasePath, "Config", "appSettings.json");
        public static readonly string CasePath = Path.Combine(BasePath, "Case");
        
        public static readonly string ReportPath = Path.Combine(BasePath, "Reports");
        public static readonly string HtmlReportPath = Path.Combine(ReportPath, $"{DateTime.Now:yyyy-MM-dd_HHmmss}.html");

        // API路径
        public static readonly string ApiExcelFile = Path.Combine(CasePath, "API", "case.xlsx");
        public static readonly string ApiYamlPath = Path.Combine(CasePath, "API", "case.yaml");
        public static readonly string ApiJsonPath = Path.Combine(CasePath, "API", "case.json");

        // UI路径
        public static readonly string UiYamlPath = Path.Combine(CasePath, "UI", "case.yaml");
        public static readonly string UiLoginCsvFile = Path.Combine(CasePath, "UI", "test_login.csv");
        public static readonly string UiRegisterCsvFile = Path.Combine(CasePath, "UI", "test_register.csv");
        public static readonly string UiImgPath = Path.Combine(ReportPath, "img");
        
        // 用于存储当前运行的日志文件路径
        private static string _currentLogPath;
        
        public static string LogPath 
        { 
            get 
            {
                try 
                {
                    // 如果已经设置了当前日志路径，直接返回
                    if (!string.IsNullOrEmpty(_currentLogPath))
                    {
                        return _currentLogPath;
                    }

                    string logDir = Path.Combine(BasePath, "Logs");
                    
                    // 根据日志模式返回不同的文件路径
                    if (Tools.LogTool.LogConfig.TimeStampInFileName)
                    {
                        // 每次运行生成新文件模式
                        _currentLogPath = Path.Combine(logDir, $"{DateTime.Now:yyyy-MM-dd_HHmmss}.log");
                    }
                    else
                    {
                        // 单个文件模式（无论是追加还是覆盖）
                        _currentLogPath = Path.Combine(logDir, "test.log");
                    }
                    
                    return _currentLogPath;
                }
                catch
                {
                    // 如果出现任何异常，返回默认路径
                    _currentLogPath = Path.Combine(BasePath, "Logs", "test.log");
                    return _currentLogPath;
                }
            }
        }
        
        /// <summary>
        /// 重置日志路径，用于在需要时重新生成日志文件路径
        /// </summary>
        public static void ResetLogPath()
        {
            _currentLogPath = null;
        }

        /// <summary>
        /// 测试环境
        /// </summary>
        public static string Environment
        {
            get
            {
                return GetValue("TestEnvironment", "测试环境");
            }
        }
        
        /// <summary>
        /// 测试人员
        /// </summary>
        public static string Tester
        {
            get
            {
                return GetValue("Tester", "jinx");
            }
        }

        /// <summary>
        /// 获取应用程序配置
        /// </summary>
        public static IConfiguration Configuration
        {
            get
            {
                if (_configuration == null)
                {
                    InitConfiguration();
                }
                return _configuration;
            }
        }

        /// <summary>
        /// 初始化配置
        /// </summary>
        private static void InitConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(BasePath)
                .AddJsonFile("Config/appSettings.json", optional: false, reloadOnChange: true);

            _configuration = builder.Build();
        }

        /// <summary>
        /// 获取指定配置节的子配置
        /// </summary>
        /// <param name="section">配置节名称</param>
        /// <returns>配置节</returns>
        public static IConfigurationSection GetSection(string section)
        {
            return Configuration.GetSection(section);
        }

        /// <summary>
        /// 获取指定配置项值
        /// </summary>
        /// <param name="key">配置键（支持冒号分隔的层级路径）</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>配置值</returns>
        public static string GetValue(string key, string defaultValue = null)
        {
            var value = Configuration[key];
            return string.IsNullOrEmpty(value) ? defaultValue : value;
        }

        /// <summary>
        /// 获取指定类型的配置项值
        /// </summary>
        /// <typeparam name="T">要转换的类型</typeparam>
        /// <param name="key">配置键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>配置值</returns>
        public static T GetValue<T>(string key, T defaultValue = default)
        {
            try
            {
                return Configuration.GetValue<T>(key, defaultValue);
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// 获取项目根路径
        /// </summary>
        /// <returns>根路径</returns>
        private static string GetBasePath()
        {
            // 首先获取程序集位置
            string assemblyLocation = Assembly.GetExecutingAssembly().Location;
            string directoryPath = Path.GetDirectoryName(assemblyLocation);
            
            // 输出当前路径信息
            Console.WriteLine($"[配置] 程序集位置: {assemblyLocation}");
            Console.WriteLine($"[配置] 目录路径: {directoryPath}");
            
            // 向上查找实际的项目根目录（而非bin目录）
            // 检测特征：存在.sln文件或.csproj文件，以及存在特定目录结构
            string currentPath = directoryPath;
            string projectRoot = null;
            
            // 最多向上查找8层目录，确保能找到项目根目录
            for (int i = 0; i < 8; i++)
            {
                // 先检查当前目录是否包含项目文件或解决方案文件
                bool hasProjectFile = Directory.GetFiles(currentPath, "*.csproj").Length > 0;
                bool hasSolutionFile = Directory.GetFiles(currentPath, "*.sln").Length > 0;
                
                // 检查是否具有典型的项目目录结构
                bool hasTypicalStructure = Directory.Exists(Path.Combine(currentPath, "Properties")) ||
                                          (Directory.Exists(Path.Combine(currentPath, "Case")) && 
                                           Directory.Exists(Path.Combine(currentPath, "Config")));
                
                if ((hasProjectFile || hasSolutionFile) && hasTypicalStructure)
                {
                    projectRoot = currentPath;
                    break;
                }
                
                // 获取父目录
                DirectoryInfo parentDir = Directory.GetParent(currentPath);
                if (parentDir == null)
                    break;
                    
                currentPath = parentDir.FullName;
            }
            
            // 如果找到了项目根目录，使用它
            // 否则回退到当前目录，但确保Case和Config目录存在
            string basePath = projectRoot ?? directoryPath;
            
            // 如果在bin目录中，尝试找到真正的项目根目录
            if (basePath.Contains("\\bin\\") && projectRoot == null)
            {
                Console.WriteLine("[配置] 检测到在bin目录中运行，尝试定位项目根目录...");
                string[] pathParts = basePath.Split(new[] { "\\bin\\" }, StringSplitOptions.None);
                if (pathParts.Length > 1)
                {
                    string possibleProjectRoot = pathParts[0];
                    if (Directory.Exists(possibleProjectRoot))
                    {
                        Console.WriteLine($"[配置] 尝试使用可能的项目根目录: {possibleProjectRoot}");
                        
                        // 检查这个目录是否像项目根目录
                        if (Directory.GetFiles(possibleProjectRoot, "*.csproj").Length > 0 || 
                            Directory.Exists(Path.Combine(possibleProjectRoot, "Properties")))
                        {
                            basePath = possibleProjectRoot;
                            Console.WriteLine("[配置] 成功找到项目根目录!");
                        }
                    }
                }
            }
            
            // 列出basePath下的目录，以便于调试
            try
            {
                Console.WriteLine($"[配置] 目录内容: {basePath}");
                foreach (var dir in Directory.GetDirectories(basePath))
                {
                    Console.WriteLine($"  - {Path.GetFileName(dir)}/");
                }
                foreach (var file in Directory.GetFiles(basePath).Take(10)) // 限制文件数量，避免输出过多
                {
                    Console.WriteLine($"  - {Path.GetFileName(file)}");
                }
                if (Directory.GetFiles(basePath).Length > 10)
                {
                    Console.WriteLine("  - ...(更多文件)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[配置] 无法列出目录内容: {ex.Message}");
            }
            
            // 确保必要的目录存在
            Directory.CreateDirectory(Path.Combine(basePath, "Logs"));
            Directory.CreateDirectory(Path.Combine(basePath, "Reports", "img"));
            
            // 输出最终使用的路径
            Console.WriteLine($"[配置] 最终使用的项目根路径: {basePath}");
            Console.WriteLine($"[配置] 是否为bin目录: {basePath.Contains("\\bin\\")}");
            
            return basePath;
        }
    }
}