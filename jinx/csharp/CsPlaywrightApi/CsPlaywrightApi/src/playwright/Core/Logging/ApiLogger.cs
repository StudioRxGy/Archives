using System.Text.Json;

namespace CsPlaywrightApi.src.playwright.Core.Logging
{
    /// <summary>
    /// API日志记录器
    /// </summary>
    public class ApiLogger
    {
        private readonly string _logDirectory;
        private readonly bool _enableConsoleLog;
        private readonly List<ApiLogEntry> _logEntries = [];
        
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public ApiLogger(string logDirectory = "Logs", bool enableConsoleLog = true)
        {
            // 使用绝对路径，确保日志保存在程序运行目录
            _logDirectory = Path.IsPathRooted(logDirectory) 
                ? logDirectory 
                : Path.Combine(AppContext.BaseDirectory, logDirectory);
            
            _enableConsoleLog = enableConsoleLog;
            
            // 确保日志目录存在
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
                Console.WriteLine($"创建日志目录: {_logDirectory}");
            }
            else
            {
                Console.WriteLine($"日志目录: {_logDirectory}");
            }
        }

        /// <summary>
        /// 记录API请求和响应
        /// </summary>
        public async Task LogApiRequestAsync(ApiLogEntry logEntry)
        {
            // 添加到内存列表
            _logEntries.Add(logEntry);
            
            var logFileName = $"api_log_{DateTime.Now:yyyyMMdd}.json";
            var logFilePath = Path.Combine(_logDirectory, logFileName);

            // 控制台输出
            if (_enableConsoleLog)
            {
                Console.WriteLine("\n" + new string('=', 80));
                Console.WriteLine($"[{logEntry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] API请求日志");
                Console.WriteLine(new string('=', 80));
                Console.WriteLine($"请求地址: {logEntry.Url}");
                Console.WriteLine($"请求方法: {logEntry.Method}");
                Console.WriteLine($"请求参数: {logEntry.RequestBody}");
                Console.WriteLine($"响应状态码: {logEntry.StatusCode}");
                Console.WriteLine($"响应Code: {logEntry.ResponseCode}");
                Console.WriteLine($"响应结果: {logEntry.ResponseBody}");
                Console.WriteLine($"耗时: {logEntry.Duration}ms");
                Console.WriteLine(new string('=', 80) + "\n");
            }

            // 写入文件
            try
            {
                var logJson = JsonSerializer.Serialize(logEntry, JsonOptions);
                
                // 追加到日志文件
                await File.AppendAllTextAsync(logFilePath, logJson + ",\n");
                
                if (_enableConsoleLog)
                {
                    Console.WriteLine($"✓ 日志已保存到: {Path.GetFullPath(logFilePath)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 写入日志文件失败: {ex.Message}");
                Console.WriteLine($"  尝试写入路径: {Path.GetFullPath(logFilePath)}");
                Console.WriteLine($"  异常详情: {ex}");
            }
        }

        /// <summary>
        /// 生成HTML报告
        /// </summary>
        public async Task GenerateHtmlReportAsync()
        {
            var htmlFileName = $"api_report_{DateTime.Now:yyyyMMdd_HHmmss}.html";
            var htmlFilePath = Path.Combine(_logDirectory, htmlFileName);

            var html = GenerateHtmlContent();
            await File.WriteAllTextAsync(htmlFilePath, html);
            
            Console.WriteLine($"\n✓ HTML报告已生成: {Path.GetFullPath(htmlFilePath)}");
        }

        private string GenerateHtmlContent()
        {
            var totalRequests = _logEntries.Count;
            var successRequests = _logEntries.Count(e => e.StatusCode >= 200 && e.StatusCode < 300);
            var failedRequests = _logEntries.Count(e => e.StatusCode >= 400);
            var warningRequests = totalRequests - successRequests - failedRequests;
            var avgDuration = _logEntries.Count > 0 ? _logEntries.Average(e => e.Duration) : 0;
            var maxDuration = _logEntries.Count > 0 ? _logEntries.Max(e => e.Duration) : 0;
            var minDuration = _logEntries.Count > 0 ? _logEntries.Min(e => e.Duration) : 0;
            var totalDuration = _logEntries.Sum(e => e.Duration);
            var successRate = totalRequests > 0 ? (successRequests * 100.0 / totalRequests) : 0;
            
            // 统计各HTTP方法的数量
            var methodStats = _logEntries.GroupBy(e => e.Method)
                .Select(g => new { Method = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();
            
            // 生成图表数据
            var methodLabels = string.Join(",", methodStats.Select(m => $"'{m.Method}'"));
            var methodData = string.Join(",", methodStats.Select(m => m.Count));
            
            // 状态码分布统计
            var statusGroups = _logEntries.GroupBy(e => GetStatusCategory(e.StatusCode))
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToList();
            
            var successCount = statusGroups.FirstOrDefault(g => g.Category == "成功")?.Count ?? 0;
            var failedCount = statusGroups.FirstOrDefault(g => g.Category == "失败")?.Count ?? 0;
            var warningCount = statusGroups.FirstOrDefault(g => g.Category == "警告")?.Count ?? 0;
            
            // 响应时间数据（用于趋势图）
            var timeLabels = string.Join(",", _logEntries.Select((e, i) => $"'请求{i + 1}'"));
            var timeData = string.Join(",", _logEntries.Select(e => e.Duration));
            
            // 格式化总执行时间
            var totalDurationFormatted = FormatDuration(totalDuration);

            var requestRows = string.Join("\n", _logEntries.Select((entry, index) => $@"
                <tr class=""request-row"" onclick=""toggleDetails({index})"">
                    <td>{index + 1}</td>
                    <td>{entry.Timestamp:HH:mm:ss.fff}</td>
                    <td><span class=""method method-{entry.Method.ToLower()}"">{entry.Method}</span></td>
                    <td class=""url-cell"" title=""{System.Net.WebUtility.HtmlEncode(entry.Url)}"">{ShortenUrl(entry.Url)}</td>
                    <td>{GetStatusBadge(entry.StatusCode)}</td>
                    <td><span class=""execution-time"">{entry.Duration}ms</span></td>
                </tr>
                <tr id=""details-{index}"" class=""details-row"" style=""display: none;"">
                    <td colspan=""6"">
                        <div class=""details-content"">
                            <div class=""detail-section"">
                                <h4><i class=""fas fa-info-circle""></i> 请求信息</h4>
                                <p><strong>URL:</strong> {System.Net.WebUtility.HtmlEncode(entry.Url)}</p>
                                <p><strong>方法:</strong> {entry.Method}</p>
                                <p><strong>时间:</strong> {entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}</p>
                            </div>
                            {(entry.RequestHeaders != null && entry.RequestHeaders.Count > 0 ? $@"
                            <div class=""detail-section"">
                                <h4><i class=""fas fa-arrow-up""></i> 请求头</h4>
                                <pre>{FormatHeaders(entry.RequestHeaders)}</pre>
                            </div>" : "")}
                            {(!string.IsNullOrEmpty(entry.RequestBody) ? $@"
                            <div class=""detail-section"">
                                <h4><i class=""fas fa-file-code""></i> 请求参数</h4>
                                <pre>{System.Net.WebUtility.HtmlEncode(entry.RequestBody)}</pre>
                            </div>" : "")}
                            <div class=""detail-section"">
                                <h4><i class=""fas fa-check-circle""></i> 响应信息</h4>
                                <p><strong>状态码:</strong> {entry.StatusCode}</p>
                                {(!string.IsNullOrEmpty(entry.ResponseCode) ? $"<p><strong>业务Code:</strong> {entry.ResponseCode}</p>" : "")}
                                <p><strong>耗时:</strong> {entry.Duration}ms</p>
                            </div>
                            {(entry.ResponseHeaders != null && entry.ResponseHeaders.Count > 0 ? $@"
                            <div class=""detail-section"">
                                <h4><i class=""fas fa-arrow-down""></i> 响应头</h4>
                                <pre>{FormatHeaders(entry.ResponseHeaders)}</pre>
                            </div>" : "")}
                            <div class=""detail-section"">
                                <h4><i class=""fas fa-code""></i> 响应内容</h4>
                                <pre class=""json-content"">{FormatJson(entry.ResponseBody)}</pre>
                            </div>
                        </div>
                    </td>
                </tr>"));

            return $@"<!DOCTYPE html>
<html lang=""zh-CN"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>API测试报告 - {DateTime.Now:yyyy-MM-dd HH:mm:ss}</title>
    <link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css"">
    <script src=""https://cdn.jsdelivr.net/npm/chart.js""></script>
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; font-family: 'Segoe UI', 'Microsoft YaHei', sans-serif; }}
        body {{ background-color: #f5f7fa; color: #333; line-height: 1.6; padding: 20px; }}
        .container {{ max-width: 1200px; margin: 0 auto; background-color: white; border-radius: 12px; box-shadow: 0 5px 20px rgba(0, 0, 0, 0.08); overflow: hidden; }}
        .header {{ background: linear-gradient(135deg, #4b6cb7 0%, #182848 100%); color: white; padding: 30px 40px; border-bottom: 4px solid #3a56a4; }}
        .header h1 {{ font-size: 2.4rem; margin-bottom: 8px; display: flex; align-items: center; gap: 12px; }}
        .header h1 i {{ font-size: 2.2rem; }}
        .subtitle {{ font-size: 1.1rem; opacity: 0.9; font-weight: 300; }}
        .report-info {{ display: flex; justify-content: space-between; margin-top: 20px; flex-wrap: wrap; gap: 15px; }}
        .info-item {{ display: flex; flex-direction: column; }}
        .info-label {{ font-size: 0.9rem; opacity: 0.8; margin-bottom: 4px; }}
        .info-value {{ font-size: 1.2rem; font-weight: 600; }}
        .content {{ padding: 30px 40px; }}
        .section {{ margin-bottom: 40px; }}
        .section-title {{ font-size: 1.6rem; color: #2c3e50; padding-bottom: 12px; border-bottom: 2px solid #eaeaea; margin-bottom: 20px; display: flex; align-items: center; gap: 10px; }}
        .section-title i {{ color: #4b6cb7; }}
        .stats-cards {{ display: grid; grid-template-columns: repeat(auto-fit, minmax(240px, 1fr)); gap: 20px; margin-bottom: 30px; }}
        .stat-card {{ background-color: white; border-radius: 10px; padding: 20px; box-shadow: 0 4px 8px rgba(0, 0, 0, 0.05); border-left: 5px solid #4b6cb7; transition: transform 0.3s ease; }}
        .stat-card:hover {{ transform: translateY(-5px); }}
        .stat-card.passed {{ border-left-color: #2ecc71; }}
        .stat-card.failed {{ border-left-color: #e74c3c; }}
        .stat-card.warning {{ border-left-color: #f39c12; }}
        .stat-card.duration {{ border-left-color: #3498db; }}
        .stat-card h3 {{ font-size: 1rem; color: #7f8c8d; margin-bottom: 10px; }}
        .stat-value {{ font-size: 2.2rem; font-weight: 700; margin-bottom: 5px; }}
        .stat-value.passed {{ color: #2ecc71; }}
        .stat-value.failed {{ color: #e74c3c; }}
        .stat-value.warning {{ color: #f39c12; }}
        .stat-value.duration {{ color: #3498db; }}
        .charts-row {{ display: flex; flex-wrap: wrap; gap: 30px; margin-bottom: 30px; }}
        .chart-container {{ flex: 1; min-width: 300px; background-color: white; border-radius: 10px; padding: 20px; box-shadow: 0 4px 8px rgba(0, 0, 0, 0.05); }}
        .chart-title {{ font-size: 1.2rem; margin-bottom: 15px; color: #2c3e50; text-align: center; }}
        .test-results {{ overflow-x: auto; }}
        table {{ width: 100%; border-collapse: collapse; margin-top: 10px; }}
        thead {{ background-color: #f8f9fa; }}
        th {{ padding: 15px 12px; text-align: left; font-weight: 600; color: #2c3e50; border-bottom: 2px solid #eaeaea; }}
        td {{ padding: 14px 12px; border-bottom: 1px solid #f1f1f1; }}
        .request-row {{ cursor: pointer; transition: background 0.2s; }}
        .request-row:hover {{ background-color: #f9f9f9; }}
        .details-row td {{ padding: 0; border-bottom: 1px solid #dee2e6; }}
        .details-content {{ padding: 20px; background: #f8f9fa; }}
        .detail-section {{ margin-bottom: 20px; background: white; padding: 15px; border-radius: 6px; }}
        .detail-section h4 {{ color: #4b6cb7; margin-bottom: 10px; font-size: 16px; }}
        .detail-section p {{ margin: 5px 0; color: #666; }}
        pre {{ background: #f4f4f4; padding: 12px; border-radius: 4px; overflow-x: auto; font-size: 13px; line-height: 1.5; }}
        .json-content {{ max-height: 400px; overflow-y: auto; }}
        .status-badge {{ display: inline-block; padding: 5px 12px; border-radius: 20px; font-size: 0.85rem; font-weight: 600; }}
        .status-passed {{ background-color: rgba(46, 204, 113, 0.15); color: #27ae60; }}
        .status-failed {{ background-color: rgba(231, 76, 60, 0.15); color: #c0392b; }}
        .status-warning {{ background-color: rgba(243, 156, 18, 0.15); color: #d35400; }}
        .method {{ padding: 5px 12px; border-radius: 20px; font-size: 0.85rem; font-weight: 600; display: inline-block; }}
        .method-get {{ background-color: rgba(52, 152, 219, 0.15); color: #2980b9; }}
        .method-post {{ background-color: rgba(46, 204, 113, 0.15); color: #27ae60; }}
        .method-put {{ background-color: rgba(243, 156, 18, 0.15); color: #d35400; }}
        .method-delete {{ background-color: rgba(231, 76, 60, 0.15); color: #c0392b; }}
        .method-patch {{ background-color: rgba(155, 89, 182, 0.15); color: #8e44ad; }}
        .execution-time {{ font-family: monospace; background-color: #f8f9fa; padding: 3px 8px; border-radius: 4px; font-size: 0.9rem; }}
        .url-cell {{ max-width: 400px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }}
        .footer {{ background-color: #f8f9fa; padding: 20px 40px; text-align: center; color: #7f8c8d; border-top: 1px solid #eaeaea; font-size: 0.9rem; }}
        @media (max-width: 768px) {{
            .header, .content {{ padding: 20px; }}
            .header h1 {{ font-size: 1.8rem; }}
            .stats-cards {{ grid-template-columns: 1fr; }}
            .charts-row {{ flex-direction: column; }}
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1><i class=""fas fa-clipboard-check""></i> API测试报告</h1>
            <p class=""subtitle"">自动化接口测试执行结果摘要</p>
            
            <div class=""report-info"">
                <div class=""info-item"">
                    <span class=""info-label"">生成日期</span>
                    <span class=""info-value"">{DateTime.Now:yyyy年MM月dd日}</span>
                </div>
                <div class=""info-item"">
                    <span class=""info-label"">生成时间</span>
                    <span class=""info-value"">{DateTime.Now:HH:mm:ss}</span>
                </div>
                <div class=""info-item"">
                    <span class=""info-label"">测试环境</span>
                    <span class=""info-value"">.NET API</span>
                </div>
                <div class=""info-item"">
                    <span class=""info-label"">成功率</span>
                    <span class=""info-value"">{successRate:F1}%</span>
                </div>
            </div>
        </div>
        
        <div class=""content"">
            <div class=""section"">
                <h2 class=""section-title""><i class=""fas fa-chart-pie""></i> 测试摘要</h2>
                
                <div class=""stats-cards"">
                    <div class=""stat-card"">
                        <h3>总请求数</h3>
                        <div class=""stat-value"">{totalRequests}</div>
                        <p>Total Requests</p>
                    </div>
                    
                    <div class=""stat-card passed"">
                        <h3>成功的请求</h3>
                        <div class=""stat-value passed"">{successRequests}</div>
                        <p>占总数的 {successRate:F1}%</p>
                    </div>
                    
                    <div class=""stat-card failed"">
                        <h3>失败的请求</h3>
                        <div class=""stat-value failed"">{failedRequests}</div>
                        <p>占总数的 {(totalRequests > 0 ? failedRequests * 100.0 / totalRequests : 0):F1}%</p>
                    </div>
                    
                    <div class=""stat-card duration"">
                        <h3>总执行时间</h3>
                        <div class=""stat-value duration"">{totalDurationFormatted}</div>
                        <p>平均 {avgDuration:F0}ms/请求</p>
                    </div>
                </div>
                
                <div class=""charts-row"">
                    <div class=""chart-container"">
                        <h3 class=""chart-title"">请求结果分布</h3>
                        <canvas id=""resultsChart""></canvas>
                    </div>
                    
                    <div class=""chart-container"">
                        <h3 class=""chart-title"">HTTP方法统计</h3>
                        <canvas id=""methodsChart""></canvas>
                    </div>
                </div>
                
                <div class=""chart-container"">
                    <h3 class=""chart-title"">响应时间趋势</h3>
                    <canvas id=""timeChart""></canvas>
                </div>
            </div>
            
            <div class=""section"">
                <h2 class=""section-title""><i class=""fas fa-list-alt""></i> 详细测试结果</h2>
                
                <div class=""test-results"">
                    <table>
                        <thead>
                            <tr>
                                <th style=""width: 50px;"">#</th>
                                <th style=""width: 120px;"">时间</th>
                                <th style=""width: 100px;"">方法</th>
                                <th>URL</th>
                                <th style=""width: 100px;"">状态</th>
                                <th style=""width: 100px;"">耗时</th>
                            </tr>
                        </thead>
                        <tbody>
                            {requestRows}
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
        
        <div class=""footer"">
            <p>报告生成于 {DateTime.Now:yyyy-MM-dd HH:mm:ss} | 本报告由API自动化测试系统生成</p>
        </div>
    </div>

    <script>
        function toggleDetails(index) {{
            const detailsRow = document.getElementById('details-' + index);
            if (detailsRow.style.display === 'none') {{
                detailsRow.style.display = 'table-row';
            }} else {{
                detailsRow.style.display = 'none';
            }}
        }}
        
        // 初始化图表
        document.addEventListener('DOMContentLoaded', function() {{
            // 请求结果分布图表
            const resultsCtx = document.getElementById('resultsChart').getContext('2d');
            new Chart(resultsCtx, {{
                type: 'doughnut',
                data: {{
                    labels: ['成功', '失败', '警告'],
                    datasets: [{{
                        data: [{successCount}, {failedCount}, {warningCount}],
                        backgroundColor: ['#2ecc71', '#e74c3c', '#f39c12'],
                        borderWidth: 2,
                        borderColor: '#fff'
                    }}]
                }},
                options: {{
                    responsive: true,
                    plugins: {{
                        legend: {{
                            position: 'bottom',
                            labels: {{
                                padding: 20,
                                usePointStyle: true
                            }}
                        }}
                    }},
                    cutout: '65%'
                }}
            }});
            
            // HTTP方法统计图表
            const methodsCtx = document.getElementById('methodsChart').getContext('2d');
            new Chart(methodsCtx, {{
                type: 'bar',
                data: {{
                    labels: [{methodLabels}],
                    datasets: [{{
                        label: '请求数量',
                        data: [{methodData}],
                        backgroundColor: 'rgba(75, 108, 183, 0.7)',
                        borderColor: '#4b6cb7',
                        borderWidth: 1
                    }}]
                }},
                options: {{
                    responsive: true,
                    scales: {{
                        y: {{
                            beginAtZero: true,
                            ticks: {{
                                stepSize: 1
                            }}
                        }}
                    }},
                    plugins: {{
                        legend: {{
                            display: false
                        }}
                    }}
                }}
            }});
            
            // 响应时间趋势图表
            const timeCtx = document.getElementById('timeChart').getContext('2d');
            new Chart(timeCtx, {{
                type: 'line',
                data: {{
                    labels: [{timeLabels}],
                    datasets: [{{
                        label: '响应时间 (ms)',
                        data: [{timeData}],
                        borderColor: '#3498db',
                        backgroundColor: 'rgba(52, 152, 219, 0.1)',
                        tension: 0.4,
                        fill: true
                    }}]
                }},
                options: {{
                    responsive: true,
                    scales: {{
                        y: {{
                            beginAtZero: true
                        }}
                    }},
                    plugins: {{
                        legend: {{
                            display: true,
                            position: 'top'
                        }}
                    }}
                }}
            }});
        }});
    </script>
</body>
</html>";
        }

        private static string ShortenUrl(string url)
        {
            if (url.Length <= 60) return System.Net.WebUtility.HtmlEncode(url);
            return System.Net.WebUtility.HtmlEncode(string.Concat(url.AsSpan(0, 57), "..."));
        }

        private static string GetStatusClass(int statusCode)
        {
            if (statusCode >= 200 && statusCode < 300) return "success";
            if (statusCode >= 400) return "error";
            return "warning";
        }

        private static string GetStatusCategory(int statusCode)
        {
            if (statusCode >= 200 && statusCode < 300) return "成功";
            if (statusCode >= 400) return "失败";
            return "警告";
        }

        private static string GetStatusBadge(int statusCode)
        {
            var statusClass = GetStatusClass(statusCode);
            var icon = statusClass switch
            {
                "success" => "fa-check-circle",
                "error" => "fa-times-circle",
                _ => "fa-exclamation-circle"
            };
            return $@"<span class=""status-badge status-{statusClass}""><i class=""fas {icon}""></i> {statusCode}</span>";
        }

        private static string FormatDuration(long milliseconds)
        {
            if (milliseconds < 1000)
                return $"{milliseconds}ms";
            
            var seconds = milliseconds / 1000.0;
            if (seconds < 60)
                return $"{seconds:F1}s";
            
            var minutes = (int)(seconds / 60);
            var remainingSeconds = (int)(seconds % 60);
            return $"{minutes}m {remainingSeconds}s";
        }

        private static string FormatHeaders(Dictionary<string, string> headers)
        {
            return System.Net.WebUtility.HtmlEncode(string.Join("\n", headers.Select(h => $"{h.Key}: {h.Value}")));
        }

        private static string FormatJson(string json)
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(json);
                return System.Net.WebUtility.HtmlEncode(JsonSerializer.Serialize(jsonDoc, JsonOptions));
            }
            catch
            {
                return System.Net.WebUtility.HtmlEncode(json);
            }
        }
    }

    /// <summary>
    /// API日志条目
    /// </summary>
    public class ApiLogEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Url { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string RequestBody { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public string? ResponseCode { get; set; }
        public string ResponseBody { get; set; } = string.Empty;
        public long Duration { get; set; }
        public Dictionary<string, string>? RequestHeaders { get; set; }
        public Dictionary<string, string>? ResponseHeaders { get; set; }
    }
}
