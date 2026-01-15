using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Nunit_Cs.Tools
{
    /// <summary>
    /// HTTP响应处理工具
    /// </summary>
    public class ResponseTool
    {
        /// <summary>
        /// 处理HTTP响应，返回标准格式的结果
        /// </summary>
        /// <param name="response">HTTP响应消息</param>
        /// <returns>处理后的结果对象</returns>
        public async Task<dynamic> ProcessResponseAsync(HttpResponseMessage response)
        {
            var result = new
            {
                code = (int)response.StatusCode,
                status = response.IsSuccessStatusCode,
                body = await GetResponseBodyAsync(response)
            };

            return result;
        }

        /// <summary>
        /// 同步处理HTTP响应
        /// </summary>
        public dynamic ProcessResponse(HttpResponseMessage response)
        {
            return ProcessResponseAsync(response).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 获取响应内容
        /// </summary>
        private async Task<dynamic> GetResponseBodyAsync(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();

            try
            {
                // 尝试解析为JSON对象
                return JsonConvert.DeserializeObject<dynamic>(content);
            }
            catch
            {
                // 如果无法解析为JSON，则返回字符串
                return content;
            }
        }
    }
} 