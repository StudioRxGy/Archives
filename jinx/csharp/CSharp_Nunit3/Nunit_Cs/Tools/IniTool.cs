using System;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Nunit_Cs.Config;

namespace Nunit_Cs.Tools
{
    /// <summary>
    /// 配置文件读取工具类 (已废弃，推荐使用AppSettings)
    /// 对应Python的ini_tool.py
    /// </summary>
    [Obsolete("此类已废弃，请使用AppSettings类替代")]
    public class IniTool
    {
        private readonly IConfiguration _configuration;

        /// <summary>
        /// 使用指定配置文件初始化
        /// </summary>
        /// <param name="configPath">配置文件路径，如果为null则使用默认配置</param>
        public IniTool(string configPath = null)
        {
            // 默认使用AppSettings中的配置
            _configuration = AppSettings.Configuration;
            TestContext.WriteLine($"使用配置文件: {configPath ?? AppSettings.ConfigPath}");
        }

        /// <summary>
        /// 获取配置项值
        /// </summary>
        /// <param name="section">配置节</param>
        /// <param name="key">配置键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>配置值</returns>
        public string GetConfig(string section, string key, string defaultValue = null)
        {
            return AppSettings.GetValue($"{section}:{key}", defaultValue);
        }

        /// <summary>
        /// 获取指定类型的配置项值
        /// </summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="section">配置节</param>
        /// <param name="key">配置键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>配置值</returns>
        public T GetConfig<T>(string section, string key, T defaultValue = default)
        {
            return AppSettings.GetValue<T>($"{section}:{key}", defaultValue);
        }

        /// <summary>
        /// 获取完整配置节
        /// </summary>
        /// <param name="section">配置节</param>
        /// <returns>配置节对象</returns>
        public IConfigurationSection GetSection(string section)
        {
            return AppSettings.GetSection(section);
        }
    }
} 