using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bogus;

namespace Nunit_Cs.Tools
{
    /// <summary>
    /// 随机数据生成工具
    /// </summary>
    public class RandomTool
    {
        private readonly Random _random;
        private readonly Faker _faker;

        public RandomTool()
        {
            _random = new Random();
            _faker = new Faker("zh_CN");
        }

        /// <summary>
        /// 获取当前时间戳
        /// </summary>
        public string GetTimestamp => DateTime.Now.ToString("yyyyMMddHHmmss");

        /// <summary>
        /// 获取当前日期
        /// </summary>
        public string GetDate => DateTime.Now.ToString("yyyy-MM-dd");

        /// <summary>
        /// 获取当前日期时间
        /// </summary>
        public string GetDateTime => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        /// <summary>
        /// 获取当前日期时间（用于文件名）
        /// </summary>
        public string GetDayTime => DateTime.Now.ToString("yyyyMMdd_HHmmss");

        /// <summary>
        /// 生成随机整数
        /// </summary>
        /// <param name="min">最小值</param>
        /// <param name="max">最大值</param>
        public int RandomInt(int min = 0, int max = 100) => _random.Next(min, max);

        /// <summary>
        /// 生成随机小数
        /// </summary>
        /// <param name="min">最小值</param>
        /// <param name="max">最大值</param>
        /// <param name="decimals">小数位数</param>
        public double RandomDouble(double min = 0, double max = 1, int decimals = 2)
        {
            var value = min + (_random.NextDouble() * (max - min));
            return Math.Round(value, decimals);
        }

        /// <summary>
        /// 生成随机布尔值
        /// </summary>
        public bool RandomBool() => _random.Next(2) == 1;

        /// <summary>
        /// 从列表中随机选择一项
        /// </summary>
        /// <typeparam name="T">列表元素类型</typeparam>
        /// <param name="items">列表</param>
        public T RandomChoice<T>(IList<T> items) => items[_random.Next(items.Count)];

        /// <summary>
        /// 生成随机字符串
        /// </summary>
        /// <param name="length">长度</param>
        /// <param name="chars">可用字符，默认为字母和数字</param>
        public string RandomString(int length = 10, string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789")
        {
            var stringBuilder = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                stringBuilder.Append(chars[_random.Next(chars.Length)]);
            }
            return stringBuilder.ToString();
        }

        /// <summary>
        /// 生成随机手机号
        /// </summary>
        public string RandomPhone() => _faker.Phone.PhoneNumber("1##########");

        /// <summary>
        /// 生成随机邮箱
        /// </summary>
        /// <param name="domain">可选域名</param>
        public string RandomEmail(string domain = null)
        {
            if (string.IsNullOrEmpty(domain))
            {
                return _faker.Internet.Email();
            }
            else
            {
                var username = _faker.Internet.UserName();
                return $"{username}@{domain}";
            }
        }

        /// <summary>
        /// 生成随机中文名
        /// </summary>
        public string RandomChineseName() => _faker.Name.FullName();

        /// <summary>
        /// 生成随机英文名
        /// </summary>
        public string RandomEnglishName()
        {
            var englishFaker = new Faker("en");
            return englishFaker.Name.FullName();
        }

        /// <summary>
        /// 生成随机地址
        /// </summary>
        public string RandomAddress() => _faker.Address.FullAddress();

        /// <summary>
        /// 生成随机IP地址
        /// </summary>
        public string RandomIp() => _faker.Internet.Ip();

        /// <summary>
        /// 生成随机IPv6地址
        /// </summary>
        public string RandomIpv6() => _faker.Internet.Ipv6();

        /// <summary>
        /// 生成随机MAC地址
        /// </summary>
        public string RandomMac() => _faker.Internet.Mac();

        /// <summary>
        /// 生成随机用户代理
        /// </summary>
        public string RandomUserAgent() => _faker.Internet.UserAgent();

        /// <summary>
        /// 生成随机日期
        /// </summary>
        /// <param name="startYear">起始年份</param>
        /// <param name="endYear">结束年份</param>
        public DateTime RandomDate(int startYear = 2000, int endYear = 2023)
        {
            return _faker.Date.Between(
                new DateTime(startYear, 1, 1),
                new DateTime(endYear, 12, 31));
        }

        /// <summary>
        /// 生成随机日期字符串
        /// </summary>
        /// <param name="format">日期格式</param>
        /// <param name="startYear">起始年份</param>
        /// <param name="endYear">结束年份</param>
        public string RandomDateString(string format = "yyyy-MM-dd", int startYear = 2000, int endYear = 2023)
        {
            return RandomDate(startYear, endYear).ToString(format);
        }

        /// <summary>
        /// 生成随机公司名
        /// </summary>
        public string RandomCompany() => _faker.Company.CompanyName();

        /// <summary>
        /// 生成随机颜色
        /// </summary>
        /// <param name="format">颜色格式</param>
        public string RandomColor(string format = "hex")
        {
            switch (format.ToLower())
            {
                case "hex":
                    return _faker.Internet.Color();
                case "rgb":
                    var r = _random.Next(256);
                    var g = _random.Next(256);
                    var b = _random.Next(256);
                    return $"rgb({r},{g},{b})";
                default:
                    return _faker.Internet.Color();
            }
        }
    }
} 