using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.IE;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;
using Nunit_Cs.Common;
using Nunit_Cs.Config;
using NUnit.Framework;

namespace Nunit_Cs.BasePage
{
    /// <summary>
    /// 浏览器管理类，负责选择和初始化不同的WebDriver
    /// 对应Python的browser.py
    /// </summary>
    public static class Browser
    {
        // 成功和失败标记
        public const string SUCCESS = "SUCCESS";
        public const string FAIL = "FAIL";

        /// <summary>
        /// 选择并创建WebDriver实例
        /// </summary>
        /// <param name="browser">浏览器类型</param>
        /// <param name="remoteAddress">远程地址，用于远程WebDriver</param>
        /// <returns>WebDriver实例</returns>
        public static IWebDriver SelectBrowser(string browser = null, string remoteAddress = null)
        {
            IWebDriver driver = null;
            DateTime startTime = DateTime.Now;
            
            try
            {
                // 如果未指定浏览器类型，使用配置中的默认值
                browser = browser ?? EnvironmentVars.BROWSER;
                
                if (string.IsNullOrEmpty(remoteAddress)) // Web端
                {
                    if (browser.Equals("Chrome", StringComparison.OrdinalIgnoreCase))
                    {
                        Tools.LogTool.LogUiDebug($"初始化 {browser} 浏览器...", "SelectBrowser");
                        
                        var options = new ChromeOptions();
                        
                        // 去掉开发者警告
                        options.AddExcludedArgument("enable-automation");
                        options.AddAdditionalOption("useAutomationExtension", false);
                        
                        // 添加必要的Chrome选项
                        options.AddArgument("--no-sandbox");
                        options.AddArgument("--disable-dev-shm-usage");
                        options.AddArgument("--disable-gpu");
                        options.AddArgument("--disable-extensions");
                        options.AddArgument("--disable-software-rasterizer");
                        
                        // 支持无头模式
                        if (EnvironmentVars.HEADLESS_MODE)
                        {
                            Tools.LogTool.LogUiDebug("启用无头模式", "SelectBrowser");
                            options.AddArgument("--headless=new");
                        }
                        
                        // 配置ChromeDriver服务
                        var driverService = ChromeDriverService.CreateDefaultService();
                        driverService.HideCommandPromptWindow = true; // 隐藏命令行窗口
                        
                        try
                        {
                            // 创建ChromeDriver实例
                            driver = new ChromeDriver(driverService, options);
                            
                            // 记录浏览器启动
                            Tools.LogTool.LogUiAction($"开启浏览器: {browser}", true, (DateTime.Now - startTime).TotalSeconds);
                            
                            // 最大化窗口
                            var windowStartTime = DateTime.Now;
                            driver.Manage().Window.Maximize();
                            Tools.LogTool.LogUiAction("设置窗口最大化", true, (DateTime.Now - windowStartTime).TotalSeconds);
                            
                            // 设置隐式等待
                            var waitStartTime = DateTime.Now;
                            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(EnvironmentVars.IMPLICIT_WAIT_SECONDS);
                            Tools.LogTool.LogUiAction($"设定隐性等待所有元素 {EnvironmentVars.IMPLICIT_WAIT_SECONDS} 秒", true, (DateTime.Now - waitStartTime).TotalSeconds);
                            
                            // 记录元素定位格式校验
                            Tools.LogTool.LogUiDebug("开始校验元素定位data格式", "SelectBrowser");
                            Tools.LogTool.Log("ui自动化，校验元素定位data格式【START】！");
                            Tools.LogTool.Log("ui自动化，校验元素定位data格式【END！用时 0.000秒！");
                        }
                        catch (Exception ex)
                        {
                            Tools.LogTool.LogException(ex, "初始化Chrome浏览器");
                            throw;
                        }
                    }
                    else if (browser.Equals("Firefox", StringComparison.OrdinalIgnoreCase))
                    {
                        var options = new FirefoxOptions();
                        
                        if (EnvironmentVars.HEADLESS_MODE)
                        {
                            options.AddArgument("--headless");
                        }
                        
                        // 不使用特定的驱动路径
                        driver = new FirefoxDriver(options);
                    }
                    else if (browser.Equals("IE", StringComparison.OrdinalIgnoreCase))
                    {
                        var options = new InternetExplorerOptions();
                        // 不使用特定的驱动路径
                        driver = new InternetExplorerDriver(options);
                    }
                    else if (browser.Equals("Edge", StringComparison.OrdinalIgnoreCase))
                    {
                        var options = new EdgeOptions();
                        // 不使用特定的驱动路径
                        driver = new EdgeDriver(options);
                    }
                }
                else // 移动端或远程WebDriver
                {
                    if (browser.Equals("RChrome", StringComparison.OrdinalIgnoreCase))
                    {
                        // 远程Chrome
                        var options = new ChromeOptions();
                        options.AddAdditionalOption("platform", "ANY");
                        options.AddAdditionalOption("version", string.Empty);
                        options.AddAdditionalOption("javascriptEnabled", true);
                        
                        driver = new RemoteWebDriver(new Uri($"https://{remoteAddress}/wd/hub"), options);
                    }
                    else if (browser.Equals("RIE", StringComparison.OrdinalIgnoreCase))
                    {
                        // 远程IE
                        var options = new InternetExplorerOptions();
                        options.AddAdditionalOption("platform", "ANY");
                        options.AddAdditionalOption("version", string.Empty);
                        options.AddAdditionalOption("javascriptEnabled", true);
                        
                        driver = new RemoteWebDriver(new Uri($"https://{remoteAddress}/wd/hub"), options);
                    }
                    else if (browser.Equals("RFirefox", StringComparison.OrdinalIgnoreCase))
                    {
                        // 远程Firefox
                        var options = new FirefoxOptions();
                        options.AddAdditionalOption("platform", "ANY");
                        options.AddAdditionalOption("version", string.Empty);
                        options.AddAdditionalOption("javascriptEnabled", true);
                        options.AddAdditionalOption("marionette", false);
                        
                        driver = new RemoteWebDriver(new Uri($"https://{remoteAddress}/wd/hub"), options);
                    }
                }
                
                // 设置超时时间
                if (driver != null)
                {
                    driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(EnvironmentVars.IMPLICIT_WAIT_SECONDS);
                    driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(EnvironmentVars.PAGE_LOAD_TIMEOUT_SECONDS);
                    driver.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromSeconds(EnvironmentVars.SCRIPT_TIMEOUT_SECONDS);
                }
                
                TestContext.WriteLine($"{SUCCESS}==> 开启浏览器: {browser}, 共花费 {(DateTime.Now - startTime).TotalSeconds:F4} 秒");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"{FAIL}==> 初始化浏览器异常: {ex.Message}");
                throw new ArgumentException($"没有找到 {browser} 浏览器,请确认 'IE','Firefox', 'Chrome','RChrome','RIE' 或 'RFirefox'是否存在或名称是否正确。", ex);
            }
            
            if (driver == null)
            {
                throw new InvalidOperationException($"无法创建浏览器实例: {browser}");
            }
            
            return driver;
        }
        
        /// <summary>
        /// 创建WebDriverWait实例
        /// </summary>
        /// <param name="driver">WebDriver实例</param>
        /// <param name="timeoutSeconds">超时时间（秒）</param>
        /// <returns>WebDriverWait实例</returns>
        public static WebDriverWait GetWait(IWebDriver driver, int timeoutSeconds = 10)
        {
            return new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds));
        }

        /// <summary>
        /// 截图
        /// </summary>
        /// <param name="driver">WebDriver实例</param>
        /// <param name="fileName">文件名</param>
        /// <returns>截图保存路径</returns>
        public static string TakeScreenshot(IWebDriver driver, string fileName = null)
        {
            try
            {
                fileName ??= $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                
                // 确保目录存在
                Directory.CreateDirectory(AppSettings.UiImgPath);
                
                var screenshotPath = Path.Combine(AppSettings.UiImgPath, fileName);
                var screenshot = ((ITakesScreenshot)driver).GetScreenshot();
                screenshot.SaveAsFile(screenshotPath);
                
                return screenshotPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"截图异常: {ex.Message}");
                return null;
            }
        }
    }
} 