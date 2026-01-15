using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using NUnit.Framework;
using Nunit_Cs.Common;

namespace Nunit_Cs.Tools
{
    /// <summary>
    /// 邮件发送工具
    /// 对应Python的email_tool.py
    /// </summary>
    public class EmailTool
    {
        private readonly string _smtpServer;
        private readonly string _fromAddress;
        private readonly string _password;
        private readonly string _toAddresses;
        private readonly SmtpClient _smtpClient;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="smtpServer">SMTP服务器地址</param>
        /// <param name="fromAddress">发件人邮箱</param>
        /// <param name="password">发件人密码或授权码</param>
        /// <param name="toAddresses">收件人邮箱列表，逗号分隔</param>
        public EmailTool(string smtpServer = null, string fromAddress = null, string password = null, string toAddresses = null)
        {
            _smtpServer = smtpServer ?? EnvironmentVars.EMAIL_SERVER_HOST;
            _fromAddress = fromAddress ?? EnvironmentVars.EMAIL_FROMADDR;
            _password = password ?? EnvironmentVars.EMAIL_PASSWORD;
            _toAddresses = toAddresses ?? EnvironmentVars.EMAIL_TOADDRS;

            // 创建SMTP客户端
            _smtpClient = new SmtpClient(_smtpServer)
            {
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_fromAddress, _password),
                EnableSsl = true
            };
        }

        /// <summary>
        /// 发送邮件
        /// </summary>
        /// <param name="subject">邮件主题</param>
        /// <param name="content">邮件内容</param>
        /// <param name="isBodyHtml">是否是HTML格式</param>
        /// <param name="attachments">附件列表</param>
        /// <returns>发送结果</returns>
        public bool SendEmail(string subject, string content, bool isBodyHtml = false, List<string> attachments = null)
        {
            try
            {
                // 创建邮件消息
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_fromAddress),
                    Subject = subject,
                    Body = content,
                    IsBodyHtml = isBodyHtml,
                    BodyEncoding = Encoding.UTF8,
                    SubjectEncoding = Encoding.UTF8
                };

                // 添加收件人
                foreach (var toAddress in _toAddresses.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    mailMessage.To.Add(toAddress.Trim());
                }

                // 添加附件
                if (attachments != null && attachments.Count > 0)
                {
                    foreach (var attachment in attachments)
                    {
                        if (File.Exists(attachment))
                        {
                            mailMessage.Attachments.Add(new Attachment(attachment));
                        }
                        else
                        {
                            TestContext.WriteLine($"附件不存在: {attachment}");
                        }
                    }
                }

                // 发送邮件
                _smtpClient.Send(mailMessage);
                TestContext.WriteLine($"邮件发送成功: {subject}");
                return true;
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"邮件发送异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 发送带有测试报告的邮件
        /// </summary>
        /// <param name="reportPath">测试报告路径</param>
        /// <param name="extraContent">额外的邮件内容</param>
        /// <returns>发送结果</returns>
        public bool SendReportEmail(string reportPath, string extraContent = null)
        {
            try
            {
                // 检查报告文件是否存在
                if (!File.Exists(reportPath))
                {
                    TestContext.WriteLine($"测试报告文件不存在: {reportPath}");
                    return false;
                }

                // 准备邮件内容
                string subject = $"自动化测试报告 - {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                StringBuilder contentBuilder = new StringBuilder();
                contentBuilder.AppendLine("<h2>自动化测试报告</h2>");
                contentBuilder.AppendLine($"<p>测试环境: {EnvironmentVars.ENVIRONMENT}</p>");
                contentBuilder.AppendLine($"<p>测试类型: {EnvironmentVars.TEST_TYPE}</p>");
                contentBuilder.AppendLine($"<p>测试人员: {EnvironmentVars.TESTER}</p>");
                contentBuilder.AppendLine($"<p>测试时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
                
                if (!string.IsNullOrEmpty(extraContent))
                {
                    contentBuilder.AppendLine(extraContent);
                }
                
                contentBuilder.AppendLine("<p>测试报告已作为附件发送，请查收。</p>");
                
                // 准备附件
                List<string> attachments = new List<string> { reportPath };
                
                // 发送邮件
                return SendEmail(subject, contentBuilder.ToString(), true, attachments);
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"发送测试报告邮件异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 发送测试结果邮件
        /// </summary>
        public bool SendEmail(
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
                // 构建邮件内容
                StringBuilder contentBuilder = new StringBuilder();
                contentBuilder.AppendLine("<h2>自动化测试报告</h2>");
                contentBuilder.AppendLine($"<p>测试类型: {title}</p>");
                contentBuilder.AppendLine($"<p>测试环境: {environment}</p>");
                contentBuilder.AppendLine($"<p>测试人员: {tester}</p>");
                contentBuilder.AppendLine($"<p>测试结果摘要:</p>");
                contentBuilder.AppendLine("<ul>");
                contentBuilder.AppendLine($"<li>总用例数: {total}</li>");
                contentBuilder.AppendLine($"<li>通过数量: {passed}</li>");
                contentBuilder.AppendLine($"<li>失败数量: {failed}</li>");
                contentBuilder.AppendLine($"<li>跳过数量: {skipped}</li>");
                contentBuilder.AppendLine($"<li>错误数量: {error}</li>");
                contentBuilder.AppendLine($"<li>成功率: {successRate}</li>");
                contentBuilder.AppendLine($"<li>总耗时: {duration}</li>");
                contentBuilder.AppendLine("</ul>");

                if (!string.IsNullOrEmpty(reportUrl))
                {
                    contentBuilder.AppendLine($"<p>详细报告: <a href='{reportUrl}'>查看报告</a></p>");
                }

                if (!string.IsNullOrEmpty(jenkinsUrl))
                {
                    contentBuilder.AppendLine($"<p>Jenkins链接: <a href='{jenkinsUrl}'>查看构建</a></p>");
                }

                // 构建邮件主题
                string subject = $"{title}自动化测试报告 - {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

                // 发送邮件
                return SendEmail(subject, contentBuilder.ToString(), true);
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"发送测试结果邮件异常: {ex.Message}");
                return false;
            }
        }
    }
} 