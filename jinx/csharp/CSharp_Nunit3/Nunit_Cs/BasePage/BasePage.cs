using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using Nunit_Cs.Common;
using NUnit.Framework;
using Nunit_Cs.Config;
using static NUnit.Framework.Assert;

namespace Nunit_Cs.BasePage
{
    /// <summary>
    /// 页面基础类，封装UI自动化常用操作
    /// </summary>
    public class BasePage
    {
        // 成功和失败标记
        protected const string SUCCESS = "SUCCESS";
        protected const string FAIL = "FAIL";
        
        // WebDriver实例
        protected IWebDriver Driver { get; private set; }
        
        // 超时时间
        protected int DefaultTimeout { get; set; } = 20;
        
        // 测试结果统计
        protected int PassCount { get; set; } = 0;
        protected int FailCount { get; set; } = 0;
        
        protected WebDriverWait Wait { get; private set; }
        protected Actions Actions { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="driver">WebDriver实例</param>
        /// <param name="parent">父页面，用于页面继承</param>
        public BasePage(IWebDriver driver, BasePage parent = null)
        {
            Driver = driver;
            Wait = Browser.GetWait(driver);
            Actions = new Actions(driver);
            if (parent != null)
            {
                // 继承父页面的属性
                PassCount = parent.PassCount;
                FailCount = parent.FailCount;
            }
        }

        #region 基本页面操作
        
        /// <summary>
        /// 打开网址
        /// </summary>
        /// <param name="url">要打开的URL</param>
        public void OpenUrl(string url)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                Driver.Navigate().GoToUrl(url);
                TestContext.WriteLine($"{SUCCESS}==> 打开网址 {url}, 花费 {(DateTime.Now - startTime).TotalSeconds:F4} 秒");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"{FAIL}==> 无法加载 {url}, 花费 {(DateTime.Now - startTime).TotalSeconds:F4} 秒");
                TestContext.WriteLine($"异常: {ex.Message}");
                FailImg();
                throw;
            }
        }
        
        /// <summary>
        /// 获取当前页面URL
        /// </summary>
        /// <returns>当前页面URL</returns>
        public string GetNowpageUrl()
        {
            return Driver.Url;
        }
        
        /// <summary>
        /// 获取当前页面标题
        /// </summary>
        /// <returns>当前页面标题</returns>
        public string GetTitle()
        {
            return Driver.Title;
        }
        
        /// <summary>
        /// 刷新页面
        /// </summary>
        public void F5()
        {
            Driver.Navigate().Refresh();
            TestContext.WriteLine("页面已刷新");
        }
        
        /// <summary>
        /// 最大化窗口
        /// </summary>
        public void MaxWindow()
        {
            Driver.Manage().Window.Maximize();
            TestContext.WriteLine("窗口已最大化");
        }
        
        /// <summary>
        /// 设置窗口大小
        /// </summary>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        public void SetWindow(int width, int height)
        {
            Driver.Manage().Window.Size = new System.Drawing.Size(width, height);
            TestContext.WriteLine($"窗口大小已设置为: {width}x{height}");
        }
        
        /// <summary>
        /// 关闭当前窗口
        /// </summary>
        public void Close()
        {
            Driver.Close();
            TestContext.WriteLine("当前窗口已关闭");
        }
        
        /// <summary>
        /// 退出浏览器
        /// </summary>
        public void Quit()
        {
            Driver.Quit();
            TestContext.WriteLine("浏览器已退出");
        }
        
        /// <summary>
        /// 隐式等待
        /// </summary>
        /// <param name="seconds">等待秒数</param>
        public void ImplicitlyWait(int seconds)
        {
            Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(seconds);
            TestContext.WriteLine($"隐式等待已设置为: {seconds}秒");
        }
        
        /// <summary>
        /// 线程休眠等待
        /// </summary>
        /// <param name="seconds">等待秒数</param>
        public void SleepWait(double seconds)
        {
            Thread.Sleep(TimeSpan.FromSeconds(seconds));
            TestContext.WriteLine($"线程休眠: {seconds}秒");
        }
        
        #endregion

        #region 元素定位与操作

        /// <summary>
        /// 查找元素
        /// </summary>
        /// <param name="by">元素定位方式</param>
        /// <param name="timeoutSeconds">超时时间</param>
        /// <returns>元素对象</returns>
        protected IWebElement FindElement(By by, int timeoutSeconds = Constants.TimeoutSeconds.DEFAULT)
        {
            try
            {
                return new WebDriverWait(Driver, TimeSpan.FromSeconds(timeoutSeconds))
                    .Until(driver => driver.FindElement(by));
            }
            catch (Exception ex)
            {
                LogError($"查找元素失败: {by}, 错误: {ex.Message}");
                TakeScreenshot();
                throw;
            }
        }

        /// <summary>
        /// 查找多个元素
        /// </summary>
        /// <param name="by">元素定位方式</param>
        /// <param name="timeoutSeconds">超时时间</param>
        /// <returns>元素集合</returns>
        protected IReadOnlyCollection<IWebElement> FindElements(By by, int timeoutSeconds = Constants.TimeoutSeconds.DEFAULT)
        {
            try
            {
                return new WebDriverWait(Driver, TimeSpan.FromSeconds(timeoutSeconds))
                    .Until(driver => 
                    {
                        var elements = driver.FindElements(by);
                        return elements.Count > 0 ? elements : null;
                    });
            }
            catch (Exception ex)
            {
                LogError($"查找元素集合失败: {by}, 错误: {ex.Message}");
                return new List<IWebElement>().AsReadOnly();
            }
        }

        /// <summary>
        /// 点击元素
        /// </summary>
        /// <param name="by">元素定位方式</param>
        /// <param name="timeoutSeconds">超时时间</param>
        protected void Click(By by, int timeoutSeconds = Constants.TimeoutSeconds.DEFAULT)
        {
            try
            {
                var element = FindElement(by, timeoutSeconds);
                Wait.Until(driver => element.Displayed && element.Enabled);
                
                try
                {
                    // 先尝试普通点击
                    element.Click();
                }
                catch
                {
                    // 如果普通点击失败，尝试使用JS点击
                    ExecuteScript("arguments[0].click();", element);
                }
                
                Log($"点击元素: {by}");
            }
            catch (Exception ex)
            {
                LogError($"点击元素失败: {by}, 错误: {ex.Message}");
                TakeScreenshot();
                throw;
            }
        }

        /// <summary>
        /// 输入文本
        /// </summary>
        /// <param name="by">元素定位方式</param>
        /// <param name="text">输入的文本</param>
        /// <param name="clearFirst">是否先清空</param>
        /// <param name="timeoutSeconds">超时时间</param>
        protected void Input(By by, string text, bool clearFirst = true, int timeoutSeconds = Constants.TimeoutSeconds.DEFAULT)
        {
            try
            {
                var element = FindElement(by, timeoutSeconds);
                Wait.Until(driver => element.Displayed && element.Enabled);
                
                if (clearFirst)
                {
                    element.Clear();
                }
                
                element.SendKeys(text);
                Log($"输入文本: {by}, 文本: {text}");
            }
            catch (Exception ex)
            {
                LogError($"输入文本失败: {by}, 文本: {text}, 错误: {ex.Message}");
                TakeScreenshot();
                throw;
            }
        }

        /// <summary>
        /// 获取元素文本
        /// </summary>
        /// <param name="by">元素定位方式</param>
        /// <param name="timeoutSeconds">超时时间</param>
        /// <returns>元素文本</returns>
        protected string GetText(By by, int timeoutSeconds = Constants.TimeoutSeconds.DEFAULT)
        {
            try
            {
                var element = FindElement(by, timeoutSeconds);
                return element.Text;
            }
            catch (Exception ex)
            {
                LogError($"获取元素文本失败: {by}, 错误: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 获取元素属性
        /// </summary>
        /// <param name="by">元素定位方式</param>
        /// <param name="attributeName">属性名</param>
        /// <param name="timeoutSeconds">超时时间</param>
        /// <returns>属性值</returns>
        protected string GetAttribute(By by, string attributeName, int timeoutSeconds = Constants.TimeoutSeconds.DEFAULT)
        {
            try
            {
                var element = FindElement(by, timeoutSeconds);
                return element.GetAttribute(attributeName);
            }
            catch (Exception ex)
            {
                LogError($"获取元素属性失败: {by}, 属性: {attributeName}, 错误: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 等待元素出现
        /// </summary>
        /// <param name="locator">元素定位器</param>
        /// <param name="seconds">等待秒数</param>
        /// <returns>元素是否存在</returns>
        protected bool ElementWait(By locator, int seconds = 2)
        {
            try
            {
                var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(seconds));
                wait.Until(ExpectedConditions.ElementExists(locator));
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 获取元素
        /// </summary>
        /// <param name="locator">元素定位器</param>
        /// <returns>元素对象</returns>
        protected IWebElement GetElement(By locator)
        {
            if (ElementWait(locator, DefaultTimeout))
            {
                return Driver.FindElement(locator);
            }
            
            TestContext.WriteLine($"未找到元素: {locator}");
            FailImg();
            throw new NoSuchElementException($"未找到元素: {locator}");
        }
        
        /// <summary>
        /// 查找元素集合
        /// </summary>
        /// <param name="locator">元素定位器</param>
        /// <returns>元素集合</returns>
        public ReadOnlyCollection<IWebElement> FindElements(By locator)
        {
            return Driver.FindElements(locator);
        }
        
        /// <summary>
        /// 点击元素
        /// </summary>
        /// <param name="locator">元素定位器</param>
        public void Click(By locator)
        {
            try
            {
                var element = GetElement(locator);
                element.Click();
                TestContext.WriteLine($"{SUCCESS}==> 点击了元素: {locator}");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"{FAIL}==> 点击元素失败: {locator}");
                TestContext.WriteLine($"异常: {ex.Message}");
                FailImg();
                throw;
            }
        }
        
        /// <summary>
        /// 双击元素
        /// </summary>
        /// <param name="locator">元素定位器</param>
        public void DoubleClick(By locator)
        {
            try
            {
                var element = GetElement(locator);
                var action = new Actions(Driver);
                action.DoubleClick(element).Perform();
                TestContext.WriteLine($"{SUCCESS}==> 双击了元素: {locator}");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"{FAIL}==> 双击元素失败: {locator}");
                TestContext.WriteLine($"异常: {ex.Message}");
                FailImg();
                throw;
            }
        }
        
        /// <summary>
        /// 右键点击元素
        /// </summary>
        /// <param name="locator">元素定位器</param>
        public void RightClick(By locator)
        {
            try
            {
                var element = GetElement(locator);
                var action = new Actions(Driver);
                action.ContextClick(element).Perform();
                TestContext.WriteLine($"{SUCCESS}==> 右键点击了元素: {locator}");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"{FAIL}==> 右键点击元素失败: {locator}");
                TestContext.WriteLine($"异常: {ex.Message}");
                FailImg();
                throw;
            }
        }

        #endregion

        #region 页面操作

        /// <summary>
        /// 执行JavaScript脚本
        /// </summary>
        /// <param name="script">JS脚本</param>
        /// <param name="arguments">参数</param>
        /// <returns>脚本执行结果</returns>
        protected object ExecuteScript(string script, params object[] arguments)
        {
            try
            {
                var jsExecutor = (IJavaScriptExecutor)Driver;
                return jsExecutor.ExecuteScript(script, arguments);
            }
            catch (Exception ex)
            {
                LogError($"执行JS脚本失败: {script}, 错误: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 等待元素可见
        /// </summary>
        /// <param name="by">元素定位方式</param>
        /// <param name="timeoutSeconds">超时时间</param>
        /// <returns>元素是否可见</returns>
        protected bool WaitForElementVisible(By by, int timeoutSeconds = Constants.TimeoutSeconds.DEFAULT)
        {
            try
            {
                new WebDriverWait(Driver, TimeSpan.FromSeconds(timeoutSeconds))
                    .Until(ExpectedConditions.ElementIsVisible(by));
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 等待元素不可见
        /// </summary>
        /// <param name="by">元素定位方式</param>
        /// <param name="timeoutSeconds">超时时间</param>
        /// <returns>元素是否不可见</returns>
        protected bool WaitForElementInvisible(By by, int timeoutSeconds = Constants.TimeoutSeconds.DEFAULT)
        {
            try
            {
                new WebDriverWait(Driver, TimeSpan.FromSeconds(timeoutSeconds))
                    .Until(ExpectedConditions.InvisibilityOfElementLocated(by));
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 等待元素可点击
        /// </summary>
        /// <param name="by">元素定位方式</param>
        /// <param name="timeoutSeconds">超时时间</param>
        /// <returns>元素是否可点击</returns>
        protected bool WaitForElementClickable(By by, int timeoutSeconds = Constants.TimeoutSeconds.DEFAULT)
        {
            try
            {
                new WebDriverWait(Driver, TimeSpan.FromSeconds(timeoutSeconds))
                    .Until(ExpectedConditions.ElementToBeClickable(by));
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 等待页面加载完成
        /// </summary>
        /// <param name="timeoutSeconds">超时时间</param>
        protected void WaitForPageLoaded(int timeoutSeconds = Constants.TimeoutSeconds.LONG)
        {
            try
            {
                var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(timeoutSeconds));
                wait.Until(driver => ((IJavaScriptExecutor)driver)
                    .ExecuteScript("return document.readyState").Equals("complete"));
                
                // 给页面JS执行留出时间
                Thread.Sleep(500);
            }
            catch (Exception ex)
            {
                LogError($"等待页面加载超时: {ex.Message}");
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 截图
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>截图保存路径</returns>
        public string TakeScreenshot(string fileName = null)
        {
            return Browser.TakeScreenshot(Driver, fileName);
        }

        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="message">日志信息</param>
        protected void Log(string message)
        {
            TestContext.WriteLine($"[INFO] {message}");
        }

        /// <summary>
        /// 记录错误日志
        /// </summary>
        /// <param name="message">错误信息</param>
        protected void LogError(string message)
        {
            TestContext.WriteLine($"[ERROR] {message}");
        }

        /// <summary>
        /// 判断元素是否存在
        /// </summary>
        /// <param name="by">元素定位方式</param>
        /// <param name="timeoutSeconds">超时时间</param>
        /// <returns>元素是否存在</returns>
        protected bool IsElementExist(By by, int timeoutSeconds = Constants.TimeoutSeconds.SHORT)
        {
            try
            {
                FindElement(by, timeoutSeconds);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 判断元素是否显示
        /// </summary>
        /// <param name="by">元素定位方式</param>
        /// <param name="timeoutSeconds">超时时间</param>
        /// <returns>元素是否显示</returns>
        protected bool IsElementDisplayed(By by, int timeoutSeconds = Constants.TimeoutSeconds.SHORT)
        {
            try
            {
                return FindElement(by, timeoutSeconds).Displayed;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region 新方法

        /// <summary>
        /// 将鼠标移动到元素上
        /// </summary>
        /// <param name="locator">元素定位器</param>
        public void MoveToElement(By locator)
        {
            try
            {
                var element = GetElement(locator);
                var action = new Actions(Driver);
                action.MoveToElement(element).Perform();
                TestContext.WriteLine($"{SUCCESS}==> 将鼠标移动到元素上: {locator}");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"{FAIL}==> 将鼠标移动到元素上失败: {locator}");
                TestContext.WriteLine($"异常: {ex.Message}");
                FailImg();
                throw;
            }
        }
        
        /// <summary>
        /// 拖拽元素
        /// </summary>
        /// <param name="sourceLocator">源元素定位器</param>
        /// <param name="targetLocator">目标元素定位器</param>
        public void DragAndDrop(By sourceLocator, By targetLocator)
        {
            try
            {
                var sourceElement = GetElement(sourceLocator);
                var targetElement = GetElement(targetLocator);
                var action = new Actions(Driver);
                action.DragAndDrop(sourceElement, targetElement).Perform();
                TestContext.WriteLine($"{SUCCESS}==> 拖拽元素: 从 {sourceLocator} 到 {targetLocator}");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"{FAIL}==> 拖拽元素失败: 从 {sourceLocator} 到 {targetLocator}");
                TestContext.WriteLine($"异常: {ex.Message}");
                FailImg();
                throw;
            }
        }
        
        /// <summary>
        /// 提交表单
        /// </summary>
        /// <param name="locator">元素定位器</param>
        public void Submit(By locator)
        {
            try
            {
                var element = GetElement(locator);
                element.Submit();
                TestContext.WriteLine($"{SUCCESS}==> 提交表单: {locator}");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"{FAIL}==> 提交表单失败: {locator}");
                TestContext.WriteLine($"异常: {ex.Message}");
                FailImg();
                throw;
            }
        }
        
        /// <summary>
        /// 输入文本
        /// </summary>
        /// <param name="locator">元素定位器</param>
        /// <param name="text">文本内容</param>
        public void TextInput(By locator, string text)
        {
            try
            {
                var element = GetElement(locator);
                element.SendKeys(text);
                TestContext.WriteLine($"{SUCCESS}==> 输入文本: {text}, 元素: {locator}");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"{FAIL}==> 输入文本失败: {text}, 元素: {locator}");
                TestContext.WriteLine($"异常: {ex.Message}");
                FailImg();
                throw;
            }
        }
        
        /// <summary>
        /// 清除文本并输入
        /// </summary>
        /// <param name="locator">元素定位器</param>
        /// <param name="text">文本内容</param>
        public void ClearType(By locator, string text)
        {
            try
            {
                var element = GetElement(locator);
                element.Clear();
                element.SendKeys(text);
                TestContext.WriteLine($"{SUCCESS}==> 清除并输入文本: {text}, 元素: {locator}");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"{FAIL}==> 清除并输入文本失败: {text}, 元素: {locator}");
                TestContext.WriteLine($"异常: {ex.Message}");
                FailImg();
                throw;
            }
        }
        
        /// <summary>
        /// 使用JS清除文本
        /// </summary>
        /// <param name="locator">元素定位器</param>
        public void JsClearText(By locator)
        {
            try
            {
                var element = GetElement(locator);
                var js = (IJavaScriptExecutor)Driver;
                js.ExecuteScript("arguments[0].value = '';", element);
                TestContext.WriteLine($"{SUCCESS}==> 使用JS清除文本: {locator}");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"{FAIL}==> 使用JS清除文本失败: {locator}");
                TestContext.WriteLine($"异常: {ex.Message}");
                FailImg();
                throw;
            }
        }
        
        /// <summary>
        /// 输入文本并按回车键
        /// </summary>
        /// <param name="locator">元素定位器</param>
        /// <param name="text">文本内容</param>
        /// <param name="seconds">输入后等待秒数</param>
        public void TypeAndEnter(By locator, string text, double seconds = 0.5)
        {
            try
            {
                var element = GetElement(locator);
                element.SendKeys(text);
                SleepWait(seconds);
                element.SendKeys(Keys.Enter);
                TestContext.WriteLine($"{SUCCESS}==> 输入文本并按回车: {text}, 元素: {locator}");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"{FAIL}==> 输入文本并按回车失败: {text}, 元素: {locator}");
                TestContext.WriteLine($"异常: {ex.Message}");
                FailImg();
                throw;
            }
        }
        
        /// <summary>
        /// 根据文本点击元素
        /// </summary>
        /// <param name="text">要点击的文本</param>
        public void ClickText(string text)
        {
            try
            {
                var xpath = $"//*[text()='{text}' or contains(text(),'{text}')]";
                var element = Driver.FindElement(By.XPath(xpath));
                element.Click();
                TestContext.WriteLine($"{SUCCESS}==> 点击文本: {text}");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"{FAIL}==> 点击文本失败: {text}");
                TestContext.WriteLine($"异常: {ex.Message}");
                FailImg();
                throw;
            }
        }
        
        /// <summary>
        /// 使用JS点击元素
        /// </summary>
        /// <param name="locator">元素定位器</param>
        public void JsClick(By locator)
        {
            try
            {
                var element = GetElement(locator);
                var js = (IJavaScriptExecutor)Driver;
                js.ExecuteScript("arguments[0].click();", element);
                TestContext.WriteLine($"{SUCCESS}==> 使用JS点击元素: {locator}");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"{FAIL}==> 使用JS点击元素失败: {locator}");
                TestContext.WriteLine($"异常: {ex.Message}");
                FailImg();
                throw;
            }
        }
        
        #endregion
        
        #region JavaScript操作
        
        /// <summary>
        /// 执行JavaScript脚本
        /// </summary>
        /// <param name="script">JavaScript脚本</param>
        /// <returns>脚本执行结果</returns>
        public object Js(string script)
        {
            try
            {
                var js = (IJavaScriptExecutor)Driver;
                var result = js.ExecuteScript(script);
                TestContext.WriteLine($"{SUCCESS}==> 执行JavaScript脚本: {script}");
                return result;
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"{FAIL}==> 执行JavaScript脚本失败: {script}");
                TestContext.WriteLine($"异常: {ex.Message}");
                FailImg();
                throw;
            }
        }
        
        /// <summary>
        /// 滚动条操作
        /// </summary>
        /// <param name="horizontal">水平位置</param>
        /// <param name="vertical">垂直位置</param>
        public void ScrollBar(int horizontal, int vertical)
        {
            var js = $"window.scrollTo({horizontal}, {vertical})";
            Js(js);
            TestContext.WriteLine($"滚动到: 水平={horizontal}, 垂直={vertical}");
        }
        
        #endregion
        
        #region 元素信息获取
        
        /// <summary>
        /// 获取元素文本
        /// </summary>
        /// <param name="locator">元素定位器</param>
        /// <returns>元素文本</returns>
        public string GetText(By locator)
        {
            try
            {
                var element = GetElement(locator);
                var text = element.Text;
                TestContext.WriteLine($"{SUCCESS}==> 获取元素文本: {text}, 元素: {locator}");
                return text;
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"{FAIL}==> 获取元素文本失败: {locator}");
                TestContext.WriteLine($"异常: {ex.Message}");
                FailImg();
                throw;
            }
        }
        
        /// <summary>
        /// 获取元素属性
        /// </summary>
        /// <param name="locator">元素定位器</param>
        /// <param name="attribute">属性名</param>
        /// <returns>属性值</returns>
        public string GetAttribute(By locator, string attribute)
        {
            try
            {
                var element = GetElement(locator);
                var value = element.GetAttribute(attribute);
                TestContext.WriteLine($"{SUCCESS}==> 获取元素属性: {attribute}={value}, 元素: {locator}");
                return value;
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"{FAIL}==> 获取元素属性失败: {attribute}, 元素: {locator}");
                TestContext.WriteLine($"异常: {ex.Message}");
                FailImg();
                throw;
            }
        }
        
        #endregion
        
        #region 窗口和Frame操作
        
        /// <summary>
        /// 切换到iframe
        /// </summary>
        /// <param name="locator">iframe元素定位器</param>
        public void SwitchToFrame(By locator)
        {
            try
            {
                var iframe = GetElement(locator);
                Driver.SwitchTo().Frame(iframe);
                TestContext.WriteLine($"{SUCCESS}==> 切换到iframe: {locator}");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"{FAIL}==> 切换到iframe失败: {locator}");
                TestContext.WriteLine($"异常: {ex.Message}");
                FailImg();
                throw;
            }
        }
        
        /// <summary>
        /// 切换回父frame
        /// </summary>
        public void SwitchToFrameOut()
        {
            try
            {
                Driver.SwitchTo().ParentFrame();
                TestContext.WriteLine($"{SUCCESS}==> 切换回父frame");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"{FAIL}==> 切换回父frame失败");
                TestContext.WriteLine($"异常: {ex.Message}");
                FailImg();
                throw;
            }
        }
        
        /// <summary>
        /// 打开新窗口
        /// </summary>
        /// <param name="locator">触发新窗口的元素定位器</param>
        public void OpenNewWindow(By locator)
        {
            string currentHandle = Driver.CurrentWindowHandle;
            int handlesCount = Driver.WindowHandles.Count;
            
            try
            {
                Click(locator);
                
                // 等待新窗口打开
                var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
                wait.Until(driver => driver.WindowHandles.Count > handlesCount);
                
                TestContext.WriteLine($"{SUCCESS}==> 打开新窗口: {locator}");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"{FAIL}==> 打开新窗口失败: {locator}");
                TestContext.WriteLine($"异常: {ex.Message}");
                FailImg();
                throw;
            }
        }
        
        /// <summary>
        /// 切换到新窗口
        /// </summary>
        public void IntoNewWindow()
        {
            try
            {
                string currentHandle = Driver.CurrentWindowHandle;
                foreach (var handle in Driver.WindowHandles)
                {
                    if (handle != currentHandle)
                    {
                        Driver.SwitchTo().Window(handle);
                        break;
                    }
                }
                TestContext.WriteLine($"{SUCCESS}==> 切换到新窗口, URL: {Driver.Url}");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"{FAIL}==> 切换到新窗口失败");
                TestContext.WriteLine($"异常: {ex.Message}");
                FailImg();
                throw;
            }
        }
        
        #endregion
        
        #region 截图
        
        /// <summary>
        /// 失败时截图
        /// </summary>
        /// <returns>截图文件路径</returns>
        public string FailImg()
        {
            try
            {
                string filename = $"fail_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.png";
                string filepath = Path.Combine(AppSettings.UiImgPath, filename);
                
                // 确保目录存在
                Directory.CreateDirectory(AppSettings.UiImgPath);
                
                // 截图
                Screenshot screenshot = ((ITakesScreenshot)Driver).GetScreenshot();
                screenshot.SaveAsFile(filepath);
                
                TestContext.WriteLine($"失败截图已保存: {filepath}");
                return filepath;
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"截图失败: {ex.Message}");
                return string.Empty;
            }
        }
        
        /// <summary>
        /// 断言时截图
        /// </summary>
        /// <returns>截图文件路径</returns>
        public string AssertImg()
        {
            try
            {
                string filename = $"assert_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.png";
                string filepath = Path.Combine(AppSettings.UiImgPath, filename);
                
                // 确保目录存在
                Directory.CreateDirectory(AppSettings.UiImgPath);
                
                // 截图
                Screenshot screenshot = ((ITakesScreenshot)Driver).GetScreenshot();
                screenshot.SaveAsFile(filepath);
                
                TestContext.WriteLine($"断言截图已保存: {filepath}");
                return filepath;
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"截图失败: {ex.Message}");
                return string.Empty;
            }
        }
        
        /// <summary>
        /// 当前页面截图
        /// </summary>
        /// <returns>截图文件路径</returns>
        public string TakeNowpageScreenshot()
        {
            try
            {
                string filename = $"screenshot_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.png";
                string filepath = Path.Combine(AppSettings.UiImgPath, filename);
                
                // 确保目录存在
                Directory.CreateDirectory(AppSettings.UiImgPath);
                
                // 截图
                Screenshot screenshot = ((ITakesScreenshot)Driver).GetScreenshot();
                screenshot.SaveAsFile(filepath);
                
                TestContext.WriteLine($"页面截图已保存: {filepath}");
                return filepath;
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"截图失败: {ex.Message}");
                return string.Empty;
            }
        }
        
        #endregion
        
        #region 对话框操作
        
        /// <summary>
        /// 接受对话框
        /// </summary>
        public void AcceptAlert()
        {
            try
            {
                Driver.SwitchTo().Alert().Accept();
                TestContext.WriteLine($"{SUCCESS}==> 接受对话框");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"{FAIL}==> 接受对话框失败");
                TestContext.WriteLine($"异常: {ex.Message}");
                FailImg();
                throw;
            }
        }
        
        /// <summary>
        /// 取消对话框
        /// </summary>
        public void DismissAlert()
        {
            try
            {
                Driver.SwitchTo().Alert().Dismiss();
                TestContext.WriteLine($"{SUCCESS}==> 取消对话框");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"{FAIL}==> 取消对话框失败");
                TestContext.WriteLine($"异常: {ex.Message}");
                FailImg();
                throw;
            }
        }
        
        #endregion
        
        #region 断言
        
        /// <summary>
        /// 判断元素是否存在
        /// </summary>
        /// <param name="locator">元素定位器</param>
        /// <returns>元素是否存在</returns>
        public bool ElementExist(By locator)
        {
            try
            {
                Driver.FindElement(locator);
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 判断元素文本是否包含预期文本
        /// </summary>
        /// <param name="locator">元素定位器</param>
        /// <param name="text">预期文本</param>
        /// <returns>是否包含</returns>
        public bool IsTextInElement(By locator, string text)
        {
            try
            {
                var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(DefaultTimeout));
                return wait.Until(ExpectedConditions.TextToBePresentInElementLocated(locator, text));
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 判断元素值是否包含预期文本
        /// </summary>
        /// <param name="locator">元素定位器</param>
        /// <param name="value">预期值</param>
        /// <returns>是否包含</returns>
        public bool IsTextInValue(By locator, string value)
        {
            try
            {
                var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(DefaultTimeout));
                return wait.Until(ExpectedConditions.TextToBePresentInElementValue(locator, value));
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 判断标题是否等于预期值
        /// </summary>
        /// <param name="title">预期标题</param>
        /// <returns>是否等于</returns>
        public bool TitleIsValue(string title)
        {
            try
            {
                var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(DefaultTimeout));
                return wait.Until(ExpectedConditions.TitleIs(title));
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 判断标题是否包含预期文本
        /// </summary>
        /// <param name="text">预期文本</param>
        /// <returns>是否包含</returns>
        public bool ValueInTitle(string text)
        {
            try
            {
                var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(DefaultTimeout));
                return wait.Until(ExpectedConditions.TitleContains(text));
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 断言元素文本等于预期
        /// </summary>
        /// <param name="locator">元素定位器</param>
        /// <param name="text">预期文本</param>
        public void AssertEqual(By locator, string text)
        {
            try
            {
                string actualText = GetText(locator);
                Assert.That(actualText, Is.EqualTo(text));
                TestContext.WriteLine($"{SUCCESS}==> 断言成功: 元素文本 '{actualText}' 等于预期值 '{text}'");
                PassCount++;
            }
            catch (Exception)
            {
                string actualText = GetText(locator);
                TestContext.WriteLine($"{FAIL}==> 断言失败: 元素文本 '{actualText}' 不等于预期值 '{text}'");
                AssertImg();
                FailCount++;
                throw;
            }
        }
        
        /// <summary>
        /// 断言元素文本不等于预期
        /// </summary>
        /// <param name="locator">元素定位器</param>
        /// <param name="text">预期文本</param>
        public void AssertNotEqual(By locator, string text)
        {
            try
            {
                string actualText = GetText(locator);
                Assert.That(actualText, Is.Not.EqualTo(text));
                TestContext.WriteLine($"{SUCCESS}==> 断言成功: 元素文本 '{actualText}' 不等于预期值 '{text}'");
                PassCount++;
            }
            catch (Exception)
            {
                string actualText = GetText(locator);
                TestContext.WriteLine($"{FAIL}==> 断言失败: 元素文本 '{actualText}' 等于预期值 '{text}'");
                AssertImg();
                FailCount++;
                throw;
            }
        }
        
        #endregion
    }
} 