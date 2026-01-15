using System;

namespace Nunit_Cs.Common
{
    /// <summary>
    /// 全局常量定义
    /// </summary>
    public static class Constants
    {
        // HTTP方法常量
        public static class HttpMethods
        {
            public const string GET = "GET";
            public const string POST = "POST";
            public const string PUT = "PUT";
            public const string DELETE = "DELETE";
            public const string PATCH = "PATCH";
        }

        // 数据类型常量
        public static class DataTypes
        {
            public const string JSON = "json";
            public const string FORM = "form";
            public const string XML = "xml";
        }

        // 测试状态常量
        public static class TestStatus
        {
            public const string PASS = "pass";
            public const string FAIL = "fail";
            public const string SKIP = "skip";
            public const string ERROR = "error";
        }

        // 浏览器类型常量
        public static class BrowserTypes
        {
            public const string CHROME = "Chrome";
            public const string FIREFOX = "Firefox";
            public const string EDGE = "Edge";
            public const string IE = "IE";
        }

        // 超时时间（秒）
        public static class TimeoutSeconds
        {
            public const int DEFAULT = 10;
            public const int SHORT = 5;
            public const int LONG = 30;
        }
    }
} 