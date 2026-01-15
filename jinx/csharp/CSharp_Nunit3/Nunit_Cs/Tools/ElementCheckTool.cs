using System;
using System.Collections.Generic;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using NUnit.Framework;
using Nunit_Cs.Config;
using static NUnit.Framework.Assert;

namespace Nunit_Cs.Tools
{
    /// <summary>
    /// 元素检查工具
    /// </summary>
    public class ElementCheckTool
    {
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;
        private readonly Dictionary<string, string> _locators;

        public ElementCheckTool(IWebDriver driver, int timeoutSeconds = 10)
        {
            _driver = driver;
            _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds));
            
            // 加载YAML元素定位
            var yamlTool = new YamlTool(AppSettings.UiYamlPath);
            _locators = yamlTool.LoadElementLocators();
        }

        /// <summary>
        /// 根据元素名称获取By对象
        /// </summary>
        /// <param name="elementName">元素名称</param>
        /// <returns>By对象</returns>
        public By GetElementBy(string elementName)
        {
            if (!_locators.ContainsKey(elementName))
            {
                throw new ArgumentException($"元素定位未找到: {elementName}");
            }
            
            var locator = _locators[elementName];
            var parts = locator.Split("==", 2);
            
            if (parts.Length != 2)
            {
                throw new ArgumentException($"元素定位格式错误: {locator}");
            }
            
            var locatorType = parts[0].ToLower();
            var locatorValue = parts[1];
            
            switch (locatorType)
            {
                case "id":
                    return By.Id(locatorValue);
                case "name":
                    return By.Name(locatorValue);
                case "xpath":
                    return By.XPath(locatorValue);
                case "css":
                    return By.CssSelector(locatorValue);
                case "class":
                    return By.ClassName(locatorValue);
                case "tag":
                    return By.TagName(locatorValue);
                case "link":
                    return By.LinkText(locatorValue);
                case "partiallink":
                    return By.PartialLinkText(locatorValue);
                default:
                    throw new ArgumentException($"不支持的定位类型: {locatorType}");
            }
        }

        /// <summary>
        /// 检查元素是否存在
        /// </summary>
        /// <param name="by">元素定位方式</param>
        /// <param name="timeoutSeconds">超时时间</param>
        /// <returns>元素是否存在</returns>
        public bool IsElementExist(By by, int timeoutSeconds = 5)
        {
            try
            {
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
                wait.Until(driver => driver.FindElement(by));
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 通过元素名称检查元素是否存在
        /// </summary>
        /// <param name="elementName">元素名称</param>
        /// <param name="timeoutSeconds">超时时间</param>
        /// <returns>元素是否存在</returns>
        public bool IsElementExistByName(string elementName, int timeoutSeconds = 5)
        {
            try
            {
                var by = GetElementBy(elementName);
                return IsElementExist(by, timeoutSeconds);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 检查元素是否可见
        /// </summary>
        /// <param name="by">元素定位方式</param>
        /// <param name="timeoutSeconds">超时时间</param>
        /// <returns>元素是否可见</returns>
        public bool IsElementVisible(By by, int timeoutSeconds = 5)
        {
            try
            {
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
                wait.Until(ExpectedConditions.ElementIsVisible(by));
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 通过元素名称检查元素是否可见
        /// </summary>
        /// <param name="elementName">元素名称</param>
        /// <param name="timeoutSeconds">超时时间</param>
        /// <returns>元素是否可见</returns>
        public bool IsElementVisibleByName(string elementName, int timeoutSeconds = 5)
        {
            try
            {
                var by = GetElementBy(elementName);
                return IsElementVisible(by, timeoutSeconds);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 检查元素是否可点击
        /// </summary>
        /// <param name="by">元素定位方式</param>
        /// <param name="timeoutSeconds">超时时间</param>
        /// <returns>元素是否可点击</returns>
        public bool IsElementClickable(By by, int timeoutSeconds = 5)
        {
            try
            {
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
                wait.Until(ExpectedConditions.ElementToBeClickable(by));
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 通过元素名称检查元素是否可点击
        /// </summary>
        /// <param name="elementName">元素名称</param>
        /// <param name="timeoutSeconds">超时时间</param>
        /// <returns>元素是否可点击</returns>
        public bool IsElementClickableByName(string elementName, int timeoutSeconds = 5)
        {
            try
            {
                var by = GetElementBy(elementName);
                return IsElementClickable(by, timeoutSeconds);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 检查元素是否包含特定文本
        /// </summary>
        /// <param name="by">元素定位方式</param>
        /// <param name="text">期望的文本</param>
        /// <param name="timeoutSeconds">超时时间</param>
        /// <returns>元素是否包含特定文本</returns>
        public bool IsElementContainsText(By by, string text, int timeoutSeconds = 5)
        {
            try
            {
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
                return wait.Until(driver => {
                    var element = driver.FindElement(by);
                    return element.Text.Contains(text);
                });
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 检查页面标题是否包含特定文本
        /// </summary>
        /// <param name="title">期望的标题</param>
        /// <param name="timeoutSeconds">超时时间</param>
        /// <returns>页面标题是否包含特定文本</returns>
        public bool IsTitleContains(string title, int timeoutSeconds = 5)
        {
            try
            {
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
                return wait.Until(ExpectedConditions.TitleContains(title));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 检查页面URL是否包含特定文本
        /// </summary>
        /// <param name="urlText">期望的URL文本</param>
        /// <param name="timeoutSeconds">超时时间</param>
        /// <returns>页面URL是否包含特定文本</returns>
        public bool IsUrlContains(string urlText, int timeoutSeconds = 5)
        {
            try
            {
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
                return wait.Until(ExpectedConditions.UrlContains(urlText));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 断言元素存在
        /// </summary>
        /// <param name="by">元素定位方式</param>
        /// <param name="message">断言消息</param>
        /// <param name="timeoutSeconds">超时时间</param>
        public void AssertElementExists(By by, string message = null, int timeoutSeconds = 5)
        {
            Assert.That(IsElementExist(by, timeoutSeconds), Is.True, 
                      message ?? $"元素未找到: {by}");
        }

        /// <summary>
        /// 断言元素可见
        /// </summary>
        /// <param name="by">元素定位方式</param>
        /// <param name="message">断言消息</param>
        /// <param name="timeoutSeconds">超时时间</param>
        public void AssertElementVisible(By by, string message = null, int timeoutSeconds = 5)
        {
            Assert.That(IsElementVisible(by, timeoutSeconds), Is.True, 
                      message ?? $"元素不可见: {by}");
        }

        /// <summary>
        /// 断言元素包含特定文本
        /// </summary>
        /// <param name="by">元素定位方式</param>
        /// <param name="text">期望的文本</param>
        /// <param name="message">断言消息</param>
        /// <param name="timeoutSeconds">超时时间</param>
        public void AssertElementContainsText(By by, string text, string message = null, int timeoutSeconds = 5)
        {
            Assert.That(IsElementContainsText(by, text, timeoutSeconds), Is.True, 
                      message ?? $"元素不包含文本 '{text}': {by}");
        }
    }
} 