using System;
using Nunit_Cs.Config;

namespace Nunit_Cs.Common
{
    /// <summary>
    /// 环境变量和全局配置
    /// 对应Python的consts.py
    /// </summary>
    public static class EnvironmentVars
    {
        // 环境和测试类型
        public static readonly string TEST_TYPE = AppSettings.GetValue("environment:type");
        public static readonly string ENVIRONMENT = AppSettings.GetValue("environment:name");
        public static readonly string API_HOST = AppSettings.GetValue("environment:host");
        public static readonly string TOKEN = AppSettings.GetValue("environment:token");

        // 测试人员
        public static readonly string TESTER = AppSettings.GetValue("testers:tester");

        // 通用开关选项
        public static readonly string DELETE_ON_OFF = AppSettings.GetValue("common:delete_on_off");
        public static readonly string EMAIL_ON_OFF = AppSettings.GetValue("common:email_on_off");
        public static readonly string DINGDING_ON_OFF = AppSettings.GetValue("common:dingding_on_off");
        public static readonly string REPORT_URL = AppSettings.GetValue("common:report_url");
        public static readonly string JENKINS_URL = AppSettings.GetValue("common:jenkins_url");

        // 钉钉相关配置
        public static readonly string DINGDING_SECRET = AppSettings.GetValue("dingding:secret");
        public static readonly string DINGDING_WEBHOOK = AppSettings.GetValue("dingding:webhook");
        public static readonly string DINGDING_AT_MOBILES = AppSettings.GetValue("dingding:at_mobiles");

        // 邮件相关配置
        public static readonly string EMAIL_FROMADDR = AppSettings.GetValue("email:sender");
        public static readonly string EMAIL_PASSWORD = AppSettings.GetValue("email:password");
        public static readonly string EMAIL_TOADDRS = AppSettings.GetValue("email:receiver");
        public static readonly string EMAIL_SERVER_HOST = AppSettings.GetValue("email:smtp_server");

        // 浏览器类型
        public static readonly string BROWSER = AppSettings.GetValue("browser:type");

        // MySQL数据库配置
        public static readonly string MYSQL_HOST = AppSettings.GetValue("mysql:host");
        public static readonly string MYSQL_PORT = AppSettings.GetValue("mysql:port");
        public static readonly string MYSQL_USER = AppSettings.GetValue("mysql:user");
        public static readonly string MYSQL_PASSWORD = AppSettings.GetValue("mysql:password");
        public static readonly string MYSQL_DB = AppSettings.GetValue("mysql:db");
        public static readonly string MYSQL_CHARSET = AppSettings.GetValue("mysql:charset");

        // Redis配置
        public static readonly string REDIS_HOST = AppSettings.GetValue("redis:host");
        public static readonly string REDIS_PORT = AppSettings.GetValue("redis:port");

        // 测试设置
        public static readonly bool HEADLESS_MODE = AppSettings.GetValue<bool>("TestSettings:HeadlessMode", false);
        public static readonly int IMPLICIT_WAIT_SECONDS = AppSettings.GetValue<int>("TestSettings:ImplicitWaitSeconds", 10);
        public static readonly int PAGE_LOAD_TIMEOUT_SECONDS = AppSettings.GetValue<int>("TestSettings:PageLoadTimeoutSeconds", 30);
        public static readonly int SCRIPT_TIMEOUT_SECONDS = AppSettings.GetValue<int>("TestSettings:ScriptTimeoutSeconds", 30);

        // WebDriver设置
        public static readonly string DRIVER_PATH = AppSettings.GetValue("WebDriverSettings:DriverPath", "./drivers");
    }
} 