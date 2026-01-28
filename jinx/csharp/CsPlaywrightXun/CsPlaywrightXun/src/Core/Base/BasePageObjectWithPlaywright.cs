using Microsoft.Playwright;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CsPlaywrightXun.src.playwright.Core.Exceptions;
using CsPlaywrightXun.src.playwright.Core.Interfaces;
using CsPlaywrightXun.src.playwright.Services.Data;

namespace CsPlaywrightXun.src.playwright.Core.Base
{
    /// <summary>
    /// Playwright 页面对象基类，提供常用的页面操作方法
    /// 等价于 Python Selenium 的 Page 基类
    /// </summary>
    public abstract class BasePageObjectWithPlaywright : IPageObject
    {
        protected readonly IPage _page;
        protected readonly ILogger _logger;
        protected readonly YamlElementReader _elementReader;
        protected readonly int _defaultTimeout = 30000; // 30秒默认超时
        
        // 测试统计
        protected int _passCount = 0;
        protected int _failCount = 0;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="page">Playwright页面实例</param>
        /// <param name="logger">日志记录器</param>
        /// <param name="elementReader">元素读取器</param>
        protected BasePageObjectWithPlaywright(IPage page, ILogger logger, YamlElementReader elementReader = null)
        {
            _page = page ?? throw new ArgumentNullException(nameof(page));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _elementReader = elementReader;
        }

        #region 导航和页面操作

        /// <summary>
        /// 打开网址
        /// </summary>
        /// <param name="url">目标URL</param>
        public virtual async Task NavigateAsync(string url)
        {
            var startTime = DateTime.Now;
            try
            {
                await _page.GotoAsync(url, new PageGotoOptions { Timeout = _defaultTimeout });
                var duration = DateTime.Now - startTime;
                _logger.LogInformation("SUCCESS ==> 打开网址 {Url}, 花费 {Duration:F4} 秒", url, duration.TotalSeconds);
            }
            catch (Exception ex)
            {
                var duration = DateTime.Now - startTime;
                _logger.LogError("FAIL ==> 无法加载 {Url}, 花费 {Duration:F4} 秒. 错误: {Error}", url, duration.TotalSeconds, ex.Message);
                await TakeFailureScreenshotAsync();
                throw new TestFrameworkException("NavigateAsync", "BasePageObject", $"无法导航到 {url}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取当前页面URL
        /// </summary>
        /// <returns>当前页面URL</returns>
        public virtual string GetCurrentUrl()
        {
            return _page.Url;
        }

        /// <summary>
        /// 刷新页面
        /// </summary>
        public virtual async Task RefreshAsync()
        {
            var startTime = DateTime.Now;
            await _page.ReloadAsync();
            var duration = DateTime.Now - startTime;
            _logger.LogInformation("SUCCESS ==> 刷新网页, 花费 {Duration:F4} 秒", duration.TotalSeconds);
        }

        /// <summary>
        /// 关闭当前页面
        /// </summary>
        public virtual async Task CloseAsync()
        {
            var startTime = DateTime.Now;
            await _page.CloseAsync();
            var duration = DateTime.Now - startTime;
            _logger.LogInformation("SUCCESS ==> 关闭当前窗口, 花费 {Duration:F4} 秒", duration.TotalSeconds);
        }

        #endregion

        #region 元素定位和等待

        /// <summary>
        /// 等待元素出现
        /// </summary>
        /// <param name="selector">元素选择器</param>
        /// <param name="timeoutMs">超时时间（毫秒）</param>
        public virtual async Task WaitForElementAsync(string selector, int timeoutMs = 0)
        {
            var timeout = timeoutMs > 0 ? timeoutMs : _defaultTimeout;
            try
            {
                await _page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions { Timeout = timeout });
            }
            catch (TimeoutException)
            {
                var message = $"元素: {selector} 没有在 {timeout / 1000} 秒内找到，尝试重新调整定位时间";
                _logger.LogError(message);
                throw new ElementNotFoundException("WaitForElement", selector, message);
            }
        }

        /// <summary>
        /// 检查元素是否存在
        /// </summary>
        /// <param name="selector">元素选择器</param>
        /// <param name="timeoutMs">超时时间（毫秒）</param>
        /// <returns>元素是否存在</returns>
        public virtual async Task<bool> IsElementExistAsync(string selector, int timeoutMs = 5000)
        {
            var startTime = DateTime.Now;
            try
            {
                await _page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions { Timeout = timeoutMs });
                var duration = DateTime.Now - startTime;
                _logger.LogInformation("SUCCESS ==> 动态等待定位元素: <{Selector}> 存在, 花费 {Duration:F4} 秒", selector, duration.TotalSeconds);
                return true;
            }
            catch (TimeoutException)
            {
                var duration = DateTime.Now - startTime;
                _logger.LogError("FAIL ==> 动态等待定位元素: <{Selector}> 不存在, 花费 {Duration:F4} 秒", selector, duration.TotalSeconds);
                await TakeFailureScreenshotAsync();
                return false;
            }
        }

        /// <summary>
        /// 强制等待
        /// </summary>
        /// <param name="seconds">等待秒数</param>
        public virtual async Task SleepAsync(int seconds)
        {
            await Task.Delay(seconds * 1000);
            _logger.LogInformation("SUCCESS ==> 强制等待 {Seconds} 秒", seconds);
        }

        #endregion

        #region 输入操作

        /// <summary>
        /// 输入文本
        /// </summary>
        /// <param name="selector">元素选择器</param>
        /// <param name="text">输入文本</param>
        public virtual async Task TypeAsync(string selector, string text)
        {
            var startTime = DateTime.Now;
            try
            {
                await WaitForElementAsync(selector);
                await _page.FillAsync(selector, text);
                var duration = DateTime.Now - startTime;
                _logger.LogInformation("SUCCESS ==> 定位元素: <{Selector}> 输入内容: {Text}, 花费 {Duration:F4} 秒", selector, text, duration.TotalSeconds);
            }
            catch (Exception ex)
            {
                var duration = DateTime.Now - startTime;
                _logger.LogError("FAIL ==> 无法定位元素: <{Selector}> 输入内容: {Text}, 花费 {Duration:F4} 秒", selector, text, duration.TotalSeconds);
                await TakeFailureScreenshotAsync();
                throw new ElementNotFoundException("TypeAsync", selector, $"无法输入文本到元素 {selector}: {ex.Message}");
            }
        }

        /// <summary>
        /// 清除并输入文本
        /// </summary>
        /// <param name="selector">元素选择器</param>
        /// <param name="text">输入文本</param>
        public virtual async Task ClearAndTypeAsync(string selector, string text)
        {
            var startTime = DateTime.Now;
            try
            {
                await WaitForElementAsync(selector);
                await _page.FillAsync(selector, ""); // 清空
                await _page.FillAsync(selector, text); // 输入
                var duration = DateTime.Now - startTime;
                _logger.LogInformation("SUCCESS ==> 清空文本定位元素: <{Selector}> 输入内容: {Text}, 花费 {Duration:F4} 秒", selector, text, duration.TotalSeconds);
            }
            catch (Exception ex)
            {
                var duration = DateTime.Now - startTime;
                _logger.LogError("FAIL ==> 无法定位清空文本元素: <{Selector}> 输入内容: {Text}, 花费 {Duration:F4} 秒", selector, text, duration.TotalSeconds);
                await TakeFailureScreenshotAsync();
                throw new ElementNotFoundException("ClearAndTypeAsync", selector, $"无法清除并输入文本到元素 {selector}: {ex.Message}");
            }
        }

        /// <summary>
        /// 输入文本并按回车
        /// </summary>
        /// <param name="selector">元素选择器</param>
        /// <param name="text">输入文本</param>
        /// <param name="delayMs">输入后等待时间（毫秒）</param>
        public virtual async Task TypeAndEnterAsync(string selector, string text, int delayMs = 500)
        {
            var startTime = DateTime.Now;
            try
            {
                await WaitForElementAsync(selector);
                await _page.FillAsync(selector, text);
                await Task.Delay(delayMs);
                await _page.PressAsync(selector, "Enter");
                var duration = DateTime.Now - startTime;
                _logger.LogInformation("SUCCESS ==> 定位元素 <{Selector}> 输入内容: {Text}, 等待时间 {Delay} 毫秒, 点击回车, 花费 {Duration:F4} 秒", 
                    selector, text, delayMs, duration.TotalSeconds);
            }
            catch (Exception ex)
            {
                var duration = DateTime.Now - startTime;
                _logger.LogError("FAIL ==> 无法找到定位元素 <{Selector}> 输入内容: {Text}, 等待时间 {Delay} 毫秒, 点击回车, 花费 {Duration:F4} 秒", 
                    selector, text, delayMs, duration.TotalSeconds);
                await TakeFailureScreenshotAsync();
                throw new ElementNotFoundException("TypeAndEnterAsync", selector, $"无法输入文本并回车到元素 {selector}: {ex.Message}");
            }
        }

        #endregion

        #region 点击操作

        /// <summary>
        /// 点击元素
        /// </summary>
        /// <param name="selector">元素选择器</param>
        public virtual async Task ClickAsync(string selector)
        {
            var startTime = DateTime.Now;
            try
            {
                await WaitForElementAsync(selector);
                await _page.ClickAsync(selector);
                var duration = DateTime.Now - startTime;
                _logger.LogInformation("SUCCESS ==> 点击元素: <{Selector}>, 花费 {Duration:F4} 秒", selector, duration.TotalSeconds);
            }
            catch (Exception ex)
            {
                var duration = DateTime.Now - startTime;
                _logger.LogError("FAIL ==> 无法找到点击元素: <{Selector}>, 花费 {Duration:F4} 秒", selector, duration.TotalSeconds);
                await TakeFailureScreenshotAsync();
                throw new ElementNotFoundException("ClickAsync", selector, $"无法点击元素 {selector}: {ex.Message}");
            }
        }

        /// <summary>
        /// 右键点击元素
        /// </summary>
        /// <param name="selector">元素选择器</param>
        public virtual async Task RightClickAsync(string selector)
        {
            var startTime = DateTime.Now;
            try
            {
                await WaitForElementAsync(selector);
                await _page.ClickAsync(selector, new PageClickOptions { Button = MouseButton.Right });
                var duration = DateTime.Now - startTime;
                _logger.LogInformation("SUCCESS ==> 鼠标右击定位元素: <{Selector}>, 花费 {Duration:F4} 秒", selector, duration.TotalSeconds);
            }
            catch (Exception ex)
            {
                var duration = DateTime.Now - startTime;
                _logger.LogError("FAIL ==> 无法找到鼠标右击定位元素: <{Selector}>, 花费 {Duration:F4} 秒", selector, duration.TotalSeconds);
                await TakeFailureScreenshotAsync();
                throw new ElementNotFoundException("RightClickAsync", selector, $"无法右键点击元素 {selector}: {ex.Message}");
            }
        }

        /// <summary>
        /// 双击元素
        /// </summary>
        /// <param name="selector">元素选择器</param>
        public virtual async Task DoubleClickAsync(string selector)
        {
            var startTime = DateTime.Now;
            try
            {
                await WaitForElementAsync(selector);
                await _page.DblClickAsync(selector);
                var duration = DateTime.Now - startTime;
                _logger.LogInformation("SUCCESS ==> 双击元素: <{Selector}>, 花费 {Duration:F4} 秒", selector, duration.TotalSeconds);
            }
            catch (Exception ex)
            {
                var duration = DateTime.Now - startTime;
                _logger.LogError("FAIL ==> 无法找到双击元素: <{Selector}>, 花费 {Duration:F4} 秒", selector, duration.TotalSeconds);
                await TakeFailureScreenshotAsync();
                throw new ElementNotFoundException("DoubleClickAsync", selector, $"无法双击元素 {selector}: {ex.Message}");
            }
        }

        /// <summary>
        /// 点击链接文本
        /// </summary>
        /// <param name="text">链接文本</param>
        public virtual async Task ClickLinkTextAsync(string text)
        {
            var startTime = DateTime.Now;
            try
            {
                await _page.ClickAsync($"text={text}");
                var duration = DateTime.Now - startTime;
                _logger.LogInformation("SUCCESS ==> 点击超链接内容: {Text}, 花费 {Duration:F4} 秒", text, duration.TotalSeconds);
            }
            catch (Exception ex)
            {
                var duration = DateTime.Now - startTime;
                _logger.LogError("FAIL ==> 无法找到可以点击的超链接内容: {Text}, 花费 {Duration:F4} 秒", text, duration.TotalSeconds);
                await TakeFailureScreenshotAsync();
                throw new ElementNotFoundException("ClickLinkTextAsync", $"text={text}", $"无法点击链接文本 {text}: {ex.Message}");
            }
        }

        #endregion

        #region 鼠标操作

        /// <summary>
        /// 悬停到元素
        /// </summary>
        /// <param name="selector">元素选择器</param>
        public virtual async Task HoverAsync(string selector)
        {
            var startTime = DateTime.Now;
            try
            {
                await WaitForElementAsync(selector);
                await _page.HoverAsync(selector);
                var duration = DateTime.Now - startTime;
                _logger.LogInformation("SUCCESS ==> 移动元素: <{Selector}>, 花费 {Duration:F4} 秒", selector, duration.TotalSeconds);
            }
            catch (Exception ex)
            {
                var duration = DateTime.Now - startTime;
                _logger.LogError("FAIL ==> 无法找到移动元素: <{Selector}>, 花费 {Duration:F4} 秒", selector, duration.TotalSeconds);
                await TakeFailureScreenshotAsync();
                throw new ElementNotFoundException("HoverAsync", selector, $"无法悬停到元素 {selector}: {ex.Message}");
            }
        }

        /// <summary>
        /// 拖拽元素
        /// </summary>
        /// <param name="sourceSelector">源元素选择器</param>
        /// <param name="targetSelector">目标元素选择器</param>
        public virtual async Task DragAndDropAsync(string sourceSelector, string targetSelector)
        {
            var startTime = DateTime.Now;
            try
            {
                await WaitForElementAsync(sourceSelector);
                await WaitForElementAsync(targetSelector);
                await _page.DragAndDropAsync(sourceSelector, targetSelector);
                var duration = DateTime.Now - startTime;
                _logger.LogInformation("SUCCESS ==> 拖动元素: <{Source}> 到元素: <{Target}>, 花费 {Duration:F4} 秒", 
                    sourceSelector, targetSelector, duration.TotalSeconds);
            }
            catch (Exception ex)
            {
                var duration = DateTime.Now - startTime;
                _logger.LogError("FAIL ==> 无法找到拖动元素: <{Source}> 到元素: <{Target}>, 花费 {Duration:F4} 秒", 
                    sourceSelector, targetSelector, duration.TotalSeconds);
                await TakeFailureScreenshotAsync();
                throw new ElementNotFoundException("DragAndDropAsync", $"{sourceSelector} -> {targetSelector}", 
                    $"无法拖拽元素从 {sourceSelector} 到 {targetSelector}: {ex.Message}");
            }
        }

        #endregion

        #region 获取元素信息

        /// <summary>
        /// 获取元素文本
        /// </summary>
        /// <param name="selector">元素选择器</param>
        /// <returns>元素文本</returns>
        public virtual async Task<string> GetTextAsync(string selector)
        {
            var startTime = DateTime.Now;
            try
            {
                await WaitForElementAsync(selector);
                var text = await _page.TextContentAsync(selector);
                var duration = DateTime.Now - startTime;
                _logger.LogInformation("SUCCESS ==> 获取元素文本: <{Selector}>, 花费 {Duration:F4} 秒", selector, duration.TotalSeconds);
                return text ?? string.Empty;
            }
            catch (Exception ex)
            {
                var duration = DateTime.Now - startTime;
                _logger.LogError("FAIL ==> 无法找到获取元素文本元素: <{Selector}>, 花费 {Duration:F4} 秒", selector, duration.TotalSeconds);
                await TakeFailureScreenshotAsync();
                throw new ElementNotFoundException("GetTextAsync", selector, $"无法获取元素文本 {selector}: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取元素属性
        /// </summary>
        /// <param name="selector">元素选择器</param>
        /// <param name="attributeName">属性名</param>
        /// <returns>属性值</returns>
        public virtual async Task<string> GetAttributeAsync(string selector, string attributeName)
        {
            var startTime = DateTime.Now;
            try
            {
                await WaitForElementAsync(selector);
                var attribute = await _page.GetAttributeAsync(selector, attributeName);
                var duration = DateTime.Now - startTime;
                _logger.LogInformation("SUCCESS ==> 获取属性元素: <{Selector}>, 属性为: {Attribute}, 花费 {Duration:F4} 秒", 
                    selector, attributeName, duration.TotalSeconds);
                return attribute ?? string.Empty;
            }
            catch (Exception ex)
            {
                var duration = DateTime.Now - startTime;
                _logger.LogError("FAIL ==> 无法找到获取属性元素: <{Selector}>, 属性为: {Attribute}, 花费 {Duration:F4} 秒", 
                    selector, attributeName, duration.TotalSeconds);
                await TakeFailureScreenshotAsync();
                throw new ElementNotFoundException("GetAttributeAsync", selector, $"无法获取元素属性 {selector}.{attributeName}: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取页面标题
        /// </summary>
        /// <returns>页面标题</returns>
        public virtual async Task<string> GetTitleAsync()
        {
            var startTime = DateTime.Now;
            var title = await _page.TitleAsync();
            var duration = DateTime.Now - startTime;
            _logger.LogInformation("SUCCESS ==> 获取网页标题, 花费 {Duration:F4} 秒", duration.TotalSeconds);
            return title;
        }

        /// <summary>
        /// 获取页面URL
        /// </summary>
        /// <returns>页面URL</returns>
        public virtual string GetUrl()
        {
            var startTime = DateTime.Now;
            var url = _page.Url;
            var duration = DateTime.Now - startTime;
            _logger.LogInformation("SUCCESS ==> 获取网页地址, 花费 {Duration:F4} 秒", duration.TotalSeconds);
            return url;
        }

        #endregion

        #region 窗口和框架操作

        /// <summary>
        /// 设置窗口大小
        /// </summary>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        public virtual async Task SetViewportSizeAsync(int width, int height)
        {
            var startTime = DateTime.Now;
            await _page.SetViewportSizeAsync(width, height);
            var duration = DateTime.Now - startTime;
            _logger.LogInformation("SUCCESS ==> 设置窗口尺寸, 宽: {Width}, 高: {Height}, 花费 {Duration:F4} 秒", 
                width, height, duration.TotalSeconds);
        }

        /// <summary>
        /// 切换到框架
        /// </summary>
        /// <param name="selector">框架选择器</param>
        public virtual async Task SwitchToFrameAsync(string selector)
        {
            var startTime = DateTime.Now;
            try
            {
                await WaitForElementAsync(selector);
                var frameElement = await _page.QuerySelectorAsync(selector);
                var frame = await frameElement.ContentFrameAsync();
                // 注意：Playwright 中框架切换的处理方式与 Selenium 不同
                // 需要使用返回的 frame 对象进行后续操作
                var duration = DateTime.Now - startTime;
                _logger.LogInformation("SUCCESS ==> 进入窗口所在框架: <{Selector}>, 花费 {Duration:F4} 秒", selector, duration.TotalSeconds);
            }
            catch (Exception ex)
            {
                var duration = DateTime.Now - startTime;
                _logger.LogError("FAIL ==> 无法找到进入窗口所在框架元素: <{Selector}>, 花费 {Duration:F4} 秒", selector, duration.TotalSeconds);
                await TakeFailureScreenshotAsync();
                throw new ElementNotFoundException("SwitchToFrameAsync", selector, $"无法切换到框架 {selector}: {ex.Message}");
            }
        }

        #endregion

        #region 弹窗处理

        /// <summary>
        /// 接受弹窗
        /// </summary>
        public virtual async Task AcceptAlertAsync()
        {
            var startTime = DateTime.Now;
            _page.Dialog += async (_, dialog) => await dialog.AcceptAsync();
            var duration = DateTime.Now - startTime;
            _logger.LogInformation("SUCCESS ==> 弹框点击确认, 花费 {Duration:F4} 秒", duration.TotalSeconds);
        }

        /// <summary>
        /// 取消弹窗
        /// </summary>
        public virtual async Task DismissAlertAsync()
        {
            var startTime = DateTime.Now;
            _page.Dialog += async (_, dialog) => await dialog.DismissAsync();
            var duration = DateTime.Now - startTime;
            _logger.LogInformation("SUCCESS ==> 弹框点击取消, 花费 {Duration:F4} 秒", duration.TotalSeconds);
        }

        #endregion

        #region JavaScript 执行

        /// <summary>
        /// 执行JavaScript脚本
        /// </summary>
        /// <param name="script">JavaScript脚本</param>
        /// <returns>执行结果</returns>
        public virtual async Task<object> ExecuteJavaScriptAsync(string script)
        {
            var startTime = DateTime.Now;
            try
            {
                var result = await _page.EvaluateAsync(script);
                var duration = DateTime.Now - startTime;
                _logger.LogInformation("SUCCESS ==> 执行js脚本: {Script}, 花费 {Duration:F4} 秒", script, duration.TotalSeconds);
                return result;
            }
            catch (Exception ex)
            {
                var duration = DateTime.Now - startTime;
                _logger.LogError("FAIL ==> 该执行js脚本无效: {Script}, 花费 {Duration:F4} 秒", script, duration.TotalSeconds);
                await TakeFailureScreenshotAsync();
                throw new TestFrameworkException("ExecuteJavaScriptAsync", "BasePageObject", $"JavaScript执行失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 通过JavaScript点击元素
        /// </summary>
        /// <param name="selector">CSS选择器</param>
        public virtual async Task ClickByJavaScriptAsync(string selector)
        {
            var startTime = DateTime.Now;
            try
            {
                var script = $"document.querySelector('{selector}').click()";
                await _page.EvaluateAsync(script);
                var duration = DateTime.Now - startTime;
                _logger.LogInformation("SUCCESS ==> 通过js脚本定位点击，js脚本内容: {Script}, 花费 {Duration:F4} 秒", script, duration.TotalSeconds);
            }
            catch (Exception ex)
            {
                var duration = DateTime.Now - startTime;
                var script = $"document.querySelector('{selector}').click()";
                _logger.LogError("FAIL ==> 无法通过js脚本定位点击，js脚本内容: {Script}, 花费 {Duration:F4} 秒", script, duration.TotalSeconds);
                await TakeFailureScreenshotAsync();
                throw new TestFrameworkException("ClickByJavaScriptAsync", "BasePageObject", $"JavaScript点击失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 滚动到指定位置
        /// </summary>
        /// <param name="x">水平位置</param>
        /// <param name="y">垂直位置</param>
        public virtual async Task ScrollToAsync(int x, int y)
        {
            var startTime = DateTime.Now;
            try
            {
                var script = $"window.scrollTo({x}, {y})";
                await _page.EvaluateAsync(script);
                var duration = DateTime.Now - startTime;
                _logger.LogInformation("SUCCESS ==> 通过js脚本设定滚动条，滚动条位置: ({X}, {Y}), 花费 {Duration:F4} 秒", x, y, duration.TotalSeconds);
            }
            catch (Exception ex)
            {
                var duration = DateTime.Now - startTime;
                _logger.LogError("FAIL ==> 无法通过js脚本设定滚动条，滚动条位置: ({X}, {Y}), 花费 {Duration:F4} 秒", x, y, duration.TotalSeconds);
                await TakeFailureScreenshotAsync();
                throw new TestFrameworkException("ScrollToAsync", "BasePageObject", $"滚动失败: {ex.Message}", ex);
            }
        }

        #endregion

        #region 截图功能

        /// <summary>
        /// 截取当前页面截图
        /// </summary>
        /// <param name="fileName">文件名（可选）</param>
        /// <returns>截图文件路径</returns>
        public virtual async Task<string> TakeScreenshotAsync(string fileName = null)
        {
            var startTime = DateTime.Now;
            try
            {
                fileName ??= $"ordinary_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.png";
                var screenshotPath = Path.Combine("Screenshots", fileName);
                
                // 确保目录存在
                Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath));
                
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath });
                var duration = DateTime.Now - startTime;
                _logger.LogInformation("SUCCESS ==> 截图当前页并保存, 截图路径: {Path}, 花费 {Duration:F4} 秒", screenshotPath, duration.TotalSeconds);
                return screenshotPath;
            }
            catch (Exception ex)
            {
                var duration = DateTime.Now - startTime;
                _logger.LogError("FAIL ==> 无法截图当前页并保存, 花费 {Duration:F4} 秒", duration.TotalSeconds);
                throw new TestFrameworkException("TakeScreenshotAsync", "BasePageObject", $"截图失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 失败时截图
        /// </summary>
        /// <returns>截图文件路径</returns>
        protected virtual async Task<string> TakeFailureScreenshotAsync()
        {
            try
            {
                var fileName = $"fail_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.png";
                return await TakeScreenshotAsync(fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError("截图失败: {Error}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 断言时截图
        /// </summary>
        /// <returns>截图文件路径</returns>
        protected virtual async Task<string> TakeAssertScreenshotAsync()
        {
            try
            {
                var fileName = $"assert_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.png";
                return await TakeScreenshotAsync(fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError("断言截图失败: {Error}", ex.Message);
                return null;
            }
        }

        #endregion

        #region 断言方法

        /// <summary>
        /// 断言相等
        /// </summary>
        /// <param name="actual">实际值</param>
        /// <param name="expected">期望值</param>
        /// <returns>断言结果</returns>
        public virtual async Task<string> AssertEqualAsync(object actual, object expected)
        {
            var startTime = DateTime.Now;
            try
            {
                if (Equals(actual, expected))
                {
                    var duration = DateTime.Now - startTime;
                    _logger.LogInformation("SUCCESS ==> 断言: {Actual} == {Expected}, 花费 {Duration:F4} 秒", actual, expected, duration.TotalSeconds);
                    _passCount++;
                    return "pass";
                }
                else
                {
                    var duration = DateTime.Now - startTime;
                    _logger.LogError("FAIL ==> 断言: {Actual} != {Expected}, 花费 {Duration:F4} 秒", actual, expected, duration.TotalSeconds);
                    await TakeAssertScreenshotAsync();
                    _failCount++;
                    return "fail";
                }
            }
            catch (Exception ex)
            {
                var duration = DateTime.Now - startTime;
                _logger.LogError("FAIL ==> 断言异常: {Error}, 花费 {Duration:F4} 秒", ex.Message, duration.TotalSeconds);
                await TakeAssertScreenshotAsync();
                _failCount++;
                return "fail";
            }
        }

        /// <summary>
        /// 断言不相等
        /// </summary>
        /// <param name="actual">实际值</param>
        /// <param name="expected">期望值</param>
        /// <returns>断言结果</returns>
        public virtual async Task<string> AssertNotEqualAsync(object actual, object expected)
        {
            var startTime = DateTime.Now;
            try
            {
                if (!Equals(actual, expected))
                {
                    var duration = DateTime.Now - startTime;
                    _logger.LogInformation("SUCCESS ==> 断言: {Actual} != {Expected}, 花费 {Duration:F4} 秒", actual, expected, duration.TotalSeconds);
                    _passCount++;
                    return "pass";
                }
                else
                {
                    var duration = DateTime.Now - startTime;
                    _logger.LogError("FAIL ==> 断言: {Actual} == {Expected}, 花费 {Duration:F4} 秒", actual, expected, duration.TotalSeconds);
                    await TakeAssertScreenshotAsync();
                    _failCount++;
                    return "fail";
                }
            }
            catch (Exception ex)
            {
                var duration = DateTime.Now - startTime;
                _logger.LogError("FAIL ==> 断言异常: {Error}, 花费 {Duration:F4} 秒", ex.Message, duration.TotalSeconds);
                await TakeAssertScreenshotAsync();
                _failCount++;
                return "fail";
            }
        }

        /// <summary>
        /// 检查文本是否在元素中
        /// </summary>
        /// <param name="selector">元素选择器</param>
        /// <param name="expectedText">期望文本</param>
        /// <returns>检查结果</returns>
        public virtual async Task<string> IsTextInElementAsync(string selector, string expectedText)
        {
            var startTime = DateTime.Now;
            try
            {
                await WaitForElementAsync(selector);
                var actualText = await GetTextAsync(selector);
                
                if (actualText.Contains(expectedText))
                {
                    var duration = DateTime.Now - startTime;
                    _logger.LogInformation("SUCCESS ==> 定位到元素: <{Selector}>, 花费 {Duration:F4} 秒", selector, duration.TotalSeconds);
                    _passCount++;
                    return "pass";
                }
                else
                {
                    var duration = DateTime.Now - startTime;
                    _logger.LogError("FAIL ==> 元素文本不匹配: <{Selector}>, 期望: {Expected}, 实际: {Actual}, 花费 {Duration:F4} 秒", 
                        selector, expectedText, actualText, duration.TotalSeconds);
                    await TakeFailureScreenshotAsync();
                    _failCount++;
                    return "fail";
                }
            }
            catch (Exception)
            {
                var duration = DateTime.Now - startTime;
                _logger.LogError("FAIL ==> 元素无法定位: <{Selector}>, 花费 {Duration:F4} 秒", selector, duration.TotalSeconds);
                await TakeFailureScreenshotAsync();
                _failCount++;
                return "fail";
            }
        }

        /// <summary>
        /// 检查标题是否匹配
        /// </summary>
        /// <param name="expectedTitle">期望标题</param>
        /// <returns>检查结果</returns>
        public virtual async Task<string> IsTitleEqualAsync(string expectedTitle)
        {
            var startTime = DateTime.Now;
            try
            {
                var actualTitle = await GetTitleAsync();
                
                if (actualTitle == expectedTitle)
                {
                    var duration = DateTime.Now - startTime;
                    _logger.LogInformation("SUCCESS ==> 判断网页标题: <{Expected}>, 实际标题: <{Actual}>, 花费 {Duration:F4} 秒", 
                        expectedTitle, actualTitle, duration.TotalSeconds);
                    _passCount++;
                    return "pass";
                }
                else
                {
                    var duration = DateTime.Now - startTime;
                    _logger.LogError("FAIL ==> 判断网页标题: <{Expected}>, 实际标题: <{Actual}>, 花费 {Duration:F4} 秒", 
                        expectedTitle, actualTitle, duration.TotalSeconds);
                    await TakeFailureScreenshotAsync();
                    _failCount++;
                    return "fail";
                }
            }
            catch (Exception ex)
            {
                var duration = DateTime.Now - startTime;
                _logger.LogError("FAIL ==> 获取标题异常: {Error}, 花费 {Duration:F4} 秒", ex.Message, duration.TotalSeconds);
                await TakeFailureScreenshotAsync();
                _failCount++;
                return "fail";
            }
        }

        /// <summary>
        /// 检查标题是否包含指定文本
        /// </summary>
        /// <param name="expectedText">期望包含的文本</param>
        /// <returns>检查结果</returns>
        public virtual async Task<string> IsTitleContainsAsync(string expectedText)
        {
            var startTime = DateTime.Now;
            try
            {
                var actualTitle = await GetTitleAsync();
                
                if (actualTitle.Contains(expectedText))
                {
                    var duration = DateTime.Now - startTime;
                    _logger.LogInformation("SUCCESS ==> 判断网页标题包含: <{Expected}>, 花费 {Duration:F4} 秒", expectedText, duration.TotalSeconds);
                    _passCount++;
                    return "pass";
                }
                else
                {
                    var duration = DateTime.Now - startTime;
                    _logger.LogError("FAIL ==> 判断网页标题包含: <{Expected}>, 实际标题: <{Actual}>, 花费 {Duration:F4} 秒", 
                        expectedText, actualTitle, duration.TotalSeconds);
                    await TakeFailureScreenshotAsync();
                    _failCount++;
                    return "fail";
                }
            }
            catch (Exception ex)
            {
                var duration = DateTime.Now - startTime;
                _logger.LogError("FAIL ==> 获取标题异常: {Error}, 花费 {Duration:F4} 秒", ex.Message, duration.TotalSeconds);
                await TakeFailureScreenshotAsync();
                _failCount++;
                return "fail";
            }
        }

        #endregion

        #region 多元素操作

        /// <summary>
        /// 查找多个元素
        /// </summary>
        /// <param name="selector">元素选择器</param>
        /// <returns>元素列表</returns>
        public virtual async Task<IReadOnlyList<IElementHandle>> FindElementsAsync(string selector)
        {
            try
            {
                var elements = await _page.QuerySelectorAllAsync(selector);
                if (elements.Count > 0)
                {
                    return elements;
                }
                else
                {
                    _logger.LogInformation("无法找到定位元素 {Selector} 在页面中", selector);
                    throw new ElementNotFoundException("FindElementsAsync", selector, $"无法找到定位元素 {selector} 在页面中");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("查找多个元素失败: {Error}", ex.Message);
                throw;
            }
        }

        #endregion

        #region 抽象方法

        /// <summary>
        /// 检查页面是否已加载
        /// </summary>
        /// <returns>页面加载状态</returns>
        public abstract Task<bool> IsLoadedAsync();

        /// <summary>
        /// 等待页面加载完成
        /// </summary>
        /// <param name="timeoutMs">超时时间（毫秒）</param>
        public abstract Task WaitForLoadAsync(int timeoutMs = 30000);

        #endregion

        #region 统计信息

        /// <summary>
        /// 获取通过测试数量
        /// </summary>
        /// <returns>通过测试数量</returns>
        public virtual int GetPassCount() => _passCount;

        /// <summary>
        /// 获取失败测试数量
        /// </summary>
        /// <returns>失败测试数量</returns>
        public virtual int GetFailCount() => _failCount;

        /// <summary>
        /// 重置统计计数
        /// </summary>
        public virtual void ResetCounts()
        {
            _passCount = 0;
            _failCount = 0;
        }

        #endregion
    }
}