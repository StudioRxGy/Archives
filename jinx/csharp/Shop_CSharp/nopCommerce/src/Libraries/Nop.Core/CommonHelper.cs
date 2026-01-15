using System.ComponentModel;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Nop.Core.Infrastructure;

namespace Nop.Core;

/// <summary>
/// 表示一个公共助手
/// </summary>
public partial class CommonHelper
{
    #region Fields

    //we use regular expression based on RFC 5322 Official Standard (see https://emailregex.com/)
    private const string EMAIL_EXPRESSION = @"^(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*|""(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21\x23-\x5b\x5d-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])*"")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\[(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?|[a-z0-9-]*[a-z0-9]:(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21-\x5a\x53-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])+)\])$";

    #endregion

    #region Methods

    /// <summary>
    /// 获取电子邮件验证正则表达式
    /// </summary>
    /// <returns>Regular expression</returns>
    [GeneratedRegex(EMAIL_EXPRESSION, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, "en-US")]
    public static partial Regex GetEmailRegex();

    /// <summary>
    /// 确保订阅者的电子邮件或抛出
    /// </summary>
    /// <param name="email">The email.</param>
    /// <returns></returns>
    public static string EnsureSubscriberEmailOrThrow(string email)
    {
        var output = EnsureNotNull(email);
        output = output.Trim();
        output = EnsureMaximumLength(output, 255);

        if (!IsValidEmail(output))
        {
            throw new NopException("Email is not valid.");
        }

        return output;
    }

    /// <summary>
    /// 验证字符串是否为有效的电子邮件格式
    /// </summary>
    /// <param name="email">Email to verify</param>
    /// <returns>true if the string is a valid e-mail address and false if it's not</returns>
    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            return false;

        email = email.Trim();

        return GetEmailRegex().IsMatch(email);
    }

    /// <summary>
    /// 验证该字符串是一个有效的ip地址
    /// </summary>
    /// <param name="ipAddress">IPAddress to verify</param>
    /// <returns>true if the string is a valid IpAddress and false if it's not</returns>
    public static bool IsValidIpAddress(string ipAddress)
    {
        return IPAddress.TryParse(ipAddress, out var _);
    }

    /// <summary>
    /// 生成随机数字代码
    /// </summary>
    /// <param name="length">Length</param>
    /// <returns>Result string</returns>
    public static string GenerateRandomDigitCode(int length)
    {
        using var random = new SecureRandomNumberGenerator();
        var str = string.Empty;
        for (var i = 0; i < length; i++)
            str = string.Concat(str, random.Next(10).ToString());
        return str;
    }

    /// <summary>
    /// 返回指定范围内的随机整数
    /// </summary>
    /// <param name="min">Minimum number</param>
    /// <param name="max">Maximum number</param>
    /// <returns>Result</returns>
    public static int GenerateRandomInteger(int min = 0, int max = int.MaxValue)
    {
        using var random = new SecureRandomNumberGenerator();
        return random.Next(min, max);
    }

    /// <summary>
    /// 确保字符串不超过最大允许长度
    /// </summary>
    /// <param name="str">Input string</param>
    /// <param name="maxLength">Maximum length</param>
    /// <param name="postfix">A string to add to the end if the original string was shorten</param>
    /// <returns>Input string if its length is OK; otherwise, truncated input string</returns>
    public static string EnsureMaximumLength(string str, int maxLength, string postfix = null)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        if (str.Length <= maxLength)
            return str;

        var pLen = postfix?.Length ?? 0;

        var result = str[0..(maxLength - pLen)];
        if (!string.IsNullOrEmpty(postfix))
        {
            result += postfix;
        }

        return result;
    }

    /// <summary>
    /// 确保字符串只包含数值
    /// </summary>
    /// <param name="str">Input string</param>
    /// <returns>Input string with only numeric values, empty string if input is null/empty</returns>
    public static string EnsureNumericOnly(string str)
    {
        return string.IsNullOrEmpty(str) ? string.Empty : new string(str.Where(char.IsDigit).ToArray());
    }

    /// <summary>
    /// 确保字符串不为空
    /// </summary>
    /// <param name="str">Input string</param>
    /// <returns>Result</returns>
    public static string EnsureNotNull(string str)
    {
        return str ?? string.Empty;
    }

    /// <summary>
    /// 指示指定的字符串是空字符串还是空字符串
    /// </summary>
    /// <param name="stringsToValidate">Array of strings to validate</param>
    /// <returns>Boolean</returns>
    public static bool AreNullOrEmpty(params string[] stringsToValidate)
    {
        return stringsToValidate.Any(string.IsNullOrEmpty);
    }

    /// <summary>
    /// 对比两个数组
    /// </summary>
    /// <typeparam name="T">Type</typeparam>
    /// <param name="a1">Array 1</param>
    /// <param name="a2">Array 2</param>
    /// <returns>Result</returns>
    public static bool ArraysEqual<T>(T[] a1, T[] a2)
    {
        //also see Enumerable.SequenceEqual(a1, a2);
        if (ReferenceEquals(a1, a2))
            return true;

        if (a1 == null || a2 == null)
            return false;

        if (a1.Length != a2.Length)
            return false;

        var comparer = EqualityComparer<T>.Default;
        return !a1.Where((t, i) => !comparer.Equals(t, a2[i])).Any();
    }

    /// <summary>
    /// 将对象的属性设置为一个值
    /// </summary>
    /// <param name="instance">The object whose property to set.</param>
    /// <param name="propertyName">The name of the property to set.</param>
    /// <param name="value">The value to set the property to.</param>
    public static void SetProperty(object instance, string propertyName, object value)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(propertyName);

        var instanceType = instance.GetType();
        var pi = instanceType.GetProperty(propertyName) 
                 ?? throw new NopException("No property '{0}' found on the instance of type '{1}'.", propertyName, instanceType);

        if (!pi.CanWrite)
            throw new NopException("The property '{0}' on the instance of type '{1}' does not have a setter.", propertyName, instanceType);
        if (value != null && !value.GetType().IsAssignableFrom(pi.PropertyType))
            value = To(value, pi.PropertyType);
        pi.SetValue(instance, value, Array.Empty<object>());
    }

    /// <summary>
    /// 将值转换为目标类型
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="destinationType">The type to convert the value to.</param>
    /// <returns>The converted value.</returns>
    public static object To(object value, Type destinationType)
    {
        return To(value, destinationType, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 将值转换为目标类型
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="destinationType">The type to convert the value to.</param>
    /// <param name="culture">Culture</param>
    /// <returns>The converted value.</returns>
    public static object To(object value, Type destinationType, CultureInfo culture)
    {
        if (value == null)
            return null;

        var sourceType = value.GetType();

        var destinationConverter = TypeDescriptor.GetConverter(destinationType);
        if (destinationConverter.CanConvertFrom(value.GetType()))
            return destinationConverter.ConvertFrom(null, culture, value);

        var sourceConverter = TypeDescriptor.GetConverter(sourceType);
        if (sourceConverter.CanConvertTo(destinationType))
            return sourceConverter.ConvertTo(null, culture, value, destinationType);

        if (destinationType.IsEnum && value is int)
            return Enum.ToObject(destinationType, (int)value);

        if (!destinationType.IsInstanceOfType(value))
            return Convert.ChangeType(value, destinationType, culture);

        return value;
    }

    /// <summary>
    /// 将值转换为目标类型
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <typeparam name="T">The type to convert the value to.</typeparam>
    /// <returns>The converted value.</returns>
    public static T To<T>(object value)
    {
        //return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
        return (T)To(value, typeof(T));
    }

    /// <summary>
    /// 将camel-case单词拆分为一个单独的单词
    /// </summary>
    /// <param name="str">Input string</param>
    /// <returns>Splitted string</returns>
    public static string SplitCamelCaseWord(string str)
    {
        if (string.IsNullOrEmpty(str))
            return string.Empty;

        var result = str.ToCharArray()
            .Select(p => p.ToString())
            .Aggregate(string.Empty, (current, c) => current + (c == c.ToUpperInvariant() ? $" {c}" : c));

        //确保没有空格（例如第一个字母是大写字母）
        result = result.TrimStart();

        return result;
    }

    /// <summary>
    /// 获取年份差异
    /// </summary>
    /// <param name="startDate"></param>
    /// <param name="endDate"></param>
    /// <returns></returns>
    public static int GetDifferenceInYears(DateTime startDate, DateTime endDate)
    {
        //source: http://stackoverflow.com/questions/9/how-do-i-calculate-someones-age-in-c
        //this assumes you are looking for the western idea of age and not using East Asian reckoning.
        var age = endDate.Year - startDate.Year;
        if (startDate > endDate.AddYears(-age))
            age--;
        return age;
    }

    /// <summary>
    /// 使用当前线程区域性的约定将DateTime获取为指定的年、月和日
    /// </summary>
    /// <param name="year">The year</param>
    /// <param name="month">The month</param>
    /// <param name="day">The day</param>
    /// <returns>An instance of the Nullable<System.DateTime></returns>
    public static DateTime? ParseDate(int? year, int? month, int? day)
    {
        if (!year.HasValue || !month.HasValue || !day.HasValue)
            return null;

        DateTime? date = null;
        try
        {
            date = new DateTime(year.Value, month.Value, day.Value, CultureInfo.CurrentCulture.Calendar);
        }
        catch { }
        return date;
    }

    #endregion

    #region Properties

    /// <summary>
    /// 获取或设置默认文件提供程序
    /// </summary>
    public static INopFileProvider DefaultFileProvider { get; set; }

    #endregion
}