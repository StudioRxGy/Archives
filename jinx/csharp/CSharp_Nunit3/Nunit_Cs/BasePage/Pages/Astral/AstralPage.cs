using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using NUnit.Framework;
using Nunit_Cs.Tools;
using Nunit_Cs.Config;
using Nunit_Cs.Common;

namespace Nunit_Cs.BasePage.Pages
{
    /// <summary>
    /// Astral页面类，提供Astral网站的页面操作
    /// 对应Python的astral.py
    /// </summary>
    public class AstralPage : BasePage
    {
        private readonly ElementCheckTool _elementTool;
        
        // 元素定位
        private readonly By _switchToEmailLogin;
        private readonly By _usernameInput;
        private readonly By _passwordInput;
        private readonly By _loginButton;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="driver">WebDriver实例</param>
        public AstralPage(IWebDriver driver) : base(driver)
        {
            _elementTool = new ElementCheckTool(driver);
            
            // 初始化元素定位
            _switchToEmailLogin = _elementTool.GetElementBy("切换邮箱登录");
            _usernameInput = _elementTool.GetElementBy("账号输入框");
            _passwordInput = _elementTool.GetElementBy("密码输入框");
            _loginButton = _elementTool.GetElementBy("登录按钮");
        }
        
        /// <summary>
        /// 登录方法
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="password">密码</param>
        /// <returns>登录结果：pass或fail</returns>
        public string Login(string username, string password)
        {
            string url = "https://www.ast1001.com/zh-cn/login";
            TestContext.WriteLine("[DEBUG] ==== 进入 Login 方法 ====");
            Tools.LogTool.LogUiDebug("==== 进入 Login 方法 ====", "Login");
            
            // 打开登录页面
            Tools.LogTool.LogUiDebug($"目标 URL: {url}", "Login");
            Tools.LogTool.LogUiDebug("正在调用 OpenUrl...", "Login");
            
            var startTime = DateTime.Now;
            OpenUrl(url);
            Tools.LogTool.LogUiAction("打开网址 " + url, true, (DateTime.Now - startTime).TotalSeconds);
            
            Tools.LogTool.LogUiDebug("OpenUrl 调用完成", "Login");
            
            // 开始定位元素
            Tools.LogTool.LogUiDebug("开始定位元素...", "Login");
            var element = FindElement(_switchToEmailLogin);
            Tools.LogTool.LogUiDebug("元素定位完成", "Login");
            
            // 点击邮箱登录按钮
            Tools.LogTool.LogUiDebug("点击邮箱登录按钮", "Login");
            try
            {
                // 先尝试滚动到元素位置
                ExecuteScript("arguments[0].scrollIntoView({block: 'center', inline: 'center'});", element);
                
                // 尝试使用JavaScript点击
                ExecuteScript("arguments[0].click();", element);
                Tools.LogTool.LogUiAction("点击邮箱登录按钮", true, 0);
            }
            catch (Exception ex)
            {
                var screenshotPath = TakeScreenshot($"fail_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.png");
                Tools.LogTool.LogUiAction("点击元素", false, 0, 
                    $"点击元素失败: {_switchToEmailLogin}\n{ex.Message}", 
                    screenshotPath);
                throw;
            }
            
            TestContext.WriteLine($"[DEBUG] 输入账号: {username}");
            TextInput(_usernameInput, username);
            
            TestContext.WriteLine($"[DEBUG] 输入密码: {password}");
            TextInput(_passwordInput, password);
            
            TestContext.WriteLine("[DEBUG] 点击登录按钮");
            Click(_loginButton);
            
            // 验证登录结果
            TestContext.WriteLine("[DEBUG] 等待登录成功页面");
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            wait.Until(ExpectedConditions.TitleContains("Astral"));
            
            TestContext.WriteLine("[DEBUG] 登录成功");
            return Constants.TestStatus.PASS;
        }
        
        /// <summary>
        /// 注册方法
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="password">密码</param>
        /// <param name="email">邮箱</param>
        /// <returns>注册结果：pass或fail</returns>
        public string Register(string username, string password, string email)
        {
            string url = "https://www.ast1001.com/zh-cn/register";
            TestContext.WriteLine("[DEBUG] ==== 进入 Register 方法 ====");
            
            try
            {
                OpenUrl(url);
                
                // 这里需要根据实际页面元素进行定位和操作
                // 假设有以下元素定位
                var usernameInput = _elementTool.GetElementBy("注册用户名输入框");
                var passwordInput = _elementTool.GetElementBy("注册密码输入框");
                var emailInput = _elementTool.GetElementBy("注册邮箱输入框");
                var registerButton = _elementTool.GetElementBy("注册按钮");
                
                TextInput(usernameInput, username);
                TextInput(passwordInput, password);
                TextInput(emailInput, email);
                Click(registerButton);
                
                // 验证注册结果，假设注册成功后会有成功提示信息
                var successMessage = _elementTool.GetElementBy("注册成功提示");
                
                // 等待成功消息显示
                var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
                wait.Until(ExpectedConditions.ElementIsVisible(successMessage));
                
                return Constants.TestStatus.PASS;
            }
            catch (WebDriverTimeoutException ex)
            {
                TestContext.WriteLine($"[ERROR] 注册操作超时: {ex.Message}");
                TakeScreenshot();
                return Constants.TestStatus.FAIL;
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"[ERROR] 注册异常: {ex.Message}");
                TakeScreenshot();
                return Constants.TestStatus.FAIL;
            }
        }
    }
} 