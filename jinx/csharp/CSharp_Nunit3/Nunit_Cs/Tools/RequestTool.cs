using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Nunit_Cs.Common;

namespace Nunit_Cs.Tools
{
    /// <summary>
    /// HTTP请求工具类
    /// </summary>
    public class RequestTool
    {
        private readonly HttpClient _httpClient;

        public RequestTool()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// 发送HTTP请求
        /// </summary>
        /// <param name="url">请求URL</param>
        /// <param name="method">请求方法 (GET, POST, PUT, DELETE)</param>
        /// <param name="data">请求数据</param>
        /// <param name="headers">请求头</param>
        /// <param name="dataType">数据类型 (json, form)</param>
        /// <returns>HTTP响应</returns>
        public async Task<HttpResponseMessage> SendRequestAsync(
            string url,
            string method,
            object data = null,
            Dictionary<string, string> headers = null,
            string dataType = Constants.DataTypes.JSON)
        {
            // 创建请求消息
            var request = new HttpRequestMessage(new HttpMethod(method), url);

            // 添加请求头
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
            }

            // 添加请求体
            if (data != null)
            {
                switch (dataType.ToLower())
                {
                    case Constants.DataTypes.JSON:
                        var json = JsonConvert.SerializeObject(data);
                        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                        break;
                    case Constants.DataTypes.FORM:
                        var formData = data as Dictionary<string, string>;
                        request.Content = new FormUrlEncodedContent(formData);
                        break;
                    default:
                        throw new ArgumentException($"不支持的数据类型: {dataType}");
                }
            }

            // 发送请求
            return await _httpClient.SendAsync(request);
        }

        /// <summary>
        /// 发送同步HTTP请求
        /// </summary>
        public HttpResponseMessage SendRequest(
            string url,
            string method,
            object data = null,
            Dictionary<string, string> headers = null,
            string dataType = Constants.DataTypes.JSON)
        {
            return SendRequestAsync(url, method, data, headers, dataType).GetAwaiter().GetResult();
        }
    }
} 