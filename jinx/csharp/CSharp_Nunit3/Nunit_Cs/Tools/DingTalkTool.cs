using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Nunit_Cs.Common;

namespace Nunit_Cs.Tools
{
    /// <summary>
    /// 钉钉消息发送工具
    /// 对应Python的dingding_tool.py
    /// </summary>
    public class DingTalkTool
    {
        private readonly string _webhook;
        private readonly string _secret;
        private readonly string _atMobiles;
        private readonly HttpClient _httpClient;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="webhook">钉钉机器人webhook地址</param>
        /// <param name="secret">钉钉机器人安全密钥</param>
        /// <param name="atMobiles">需要@的手机号码列表，逗号分隔</param>
        public DingTalkTool(string webhook = null, string secret = null, string atMobiles = null)
        {
            _webhook = webhook ?? EnvironmentVars.DINGDING_WEBHOOK;
            _secret = secret ?? EnvironmentVars.DINGDING_SECRET;
            _atMobiles = atMobiles ?? EnvironmentVars.DINGDING_AT_MOBILES;
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// 发送普通文本消息
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="atAll">是否@所有人</param>
        /// <param name="atMobiles">需要@的手机号码列表，如果为null则使用构造函数中的值</param>
        /// <returns>发送结果</returns>
        public async Task<bool> SendTextAsync(string message, bool atAll = false, List<string> atMobiles = null)
        {
            try
            {
                string[] phoneNumbers = null;
                if (atMobiles != null)
                {
                    phoneNumbers = atMobiles.ToArray();
                }
                else if (!string.IsNullOrEmpty(_atMobiles))
                {
                    phoneNumbers = _atMobiles.Split(',', StringSplitOptions.RemoveEmptyEntries);
                }

                var payload = new
                {
                    msgtype = "text",
                    text = new
                    {
                        content = message
                    },
                    at = new
                    {
                        atMobiles = phoneNumbers,
                        isAtAll = atAll
                    }
                };

                return await SendMessageAsync(payload);
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"发送钉钉文本消息异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 发送链接消息
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="text">消息内容</param>
        /// <param name="messageUrl">链接URL</param>
        /// <param name="picUrl">图片URL</param>
        /// <returns>发送结果</returns>
        public async Task<bool> SendLinkAsync(string title, string text, string messageUrl, string picUrl)
        {
            try
            {
                var payload = new
                {
                    msgtype = "link",
                    link = new
                    {
                        title,
                        text,
                        messageUrl,
                        picUrl
                    }
                };

                return await SendMessageAsync(payload);
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"发送钉钉链接消息异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 发送Markdown消息
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="text">Markdown格式内容</param>
        /// <param name="atAll">是否@所有人</param>
        /// <param name="atMobiles">需要@的手机号码列表，如果为null则使用构造函数中的值</param>
        /// <returns>发送结果</returns>
        public async Task<bool> SendMarkdownAsync(string title, string text, bool atAll = false, List<string> atMobiles = null)
        {
            try
            {
                string[] phoneNumbers = null;
                if (atMobiles != null)
                {
                    phoneNumbers = atMobiles.ToArray();
                }
                else if (!string.IsNullOrEmpty(_atMobiles))
                {
                    phoneNumbers = _atMobiles.Split(',', StringSplitOptions.RemoveEmptyEntries);
                }

                var payload = new
                {
                    msgtype = "markdown",
                    markdown = new
                    {
                        title,
                        text
                    },
                    at = new
                    {
                        atMobiles = phoneNumbers,
                        isAtAll = atAll
                    }
                };

                return await SendMessageAsync(payload);
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"发送钉钉Markdown消息异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 发送消息到钉钉
        /// </summary>
        /// <param name="payload">消息内容</param>
        /// <returns>发送结果</returns>
        private async Task<bool> SendMessageAsync(object payload)
        {
            try
            {
                // 如果设置了密钥，则需要计算签名
                string url = _webhook;
                if (!string.IsNullOrEmpty(_secret))
                {
                    long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    string sign = ComputeSignature(timestamp, _secret);
                    url += $"&timestamp={timestamp}&sign={sign}";
                }

                // 序列化消息内容
                var jsonContent = JsonConvert.SerializeObject(payload);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // 发送消息
                var response = await _httpClient.PostAsync(url, httpContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                // 记录日志
                TestContext.WriteLine($"钉钉消息发送结果: {responseContent}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"发送钉钉消息异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 计算签名
        /// </summary>
        /// <param name="timestamp">时间戳</param>
        /// <param name="secret">密钥</param>
        /// <returns>签名</returns>
        private string ComputeSignature(long timestamp, string secret)
        {
            string stringToSign = $"{timestamp}\n{secret}";
            byte[] keyBytes = Encoding.UTF8.GetBytes(secret);
            byte[] messageBytes = Encoding.UTF8.GetBytes(stringToSign);

            using (HMACSHA256 hmac = new HMACSHA256(keyBytes))
            {
                byte[] hashBytes = hmac.ComputeHash(messageBytes);
                return Convert.ToBase64String(hashBytes);
            }
        }

        /// <summary>
        /// 发送测试结果到钉钉
        /// </summary>
        public bool SendDingding(
            string title,
            string environment,
            string tester,
            int total,
            int passed,
            int failed,
            int skipped,
            int error,
            string successRate,
            string duration,
            string reportUrl,
            string jenkinsUrl)
        {
            try
            {
                // 构建Markdown消息
                StringBuilder text = new StringBuilder();
                text.AppendLine($"### {title}自动化测试报告");
                text.AppendLine($"> **测试环境**: {environment}  ");
                text.AppendLine($"> **测试人员**: {tester}  ");
                
                // 添加测试结果摘要
                text.AppendLine("#### 测试结果");
                text.AppendLine($"- 总用例数: {total}");
                text.AppendLine($"- 通过数量: {passed}");
                text.AppendLine($"- 失败数量: {failed}");
                text.AppendLine($"- 跳过数量: {skipped}");
                text.AppendLine($"- 错误数量: {error}");
                text.AppendLine($"- 成功率: {successRate}");
                text.AppendLine($"- 总耗时: {duration}");
                
                // 添加链接
                text.AppendLine("#### 相关链接");
                if (!string.IsNullOrEmpty(reportUrl))
                {
                    text.AppendLine($"- [查看测试报告]({reportUrl})");
                }
                
                if (!string.IsNullOrEmpty(jenkinsUrl))
                {
                    text.AppendLine($"- [查看Jenkins构建]({jenkinsUrl})");
                }

                // 发送Markdown消息，这里使用异步方法但同步等待结果
                return SendMarkdownAsync(
                    $"{title}自动化测试报告", 
                    text.ToString()).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"发送钉钉测试结果消息异常: {ex.Message}");
                return false;
            }
        }
    }
} 