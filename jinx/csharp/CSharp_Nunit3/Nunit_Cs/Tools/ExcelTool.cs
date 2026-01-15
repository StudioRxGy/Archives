using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Data;
using OfficeOpenXml;
using Nunit_Cs.Config;
using NUnit.Framework;

namespace Nunit_Cs.Tools
{
    /// <summary>
    /// Excel数据处理工具
    /// </summary>
    public class ExcelTool
    {
        private readonly string _filePath;
        
        public ExcelTool(string filePath = null)
        {
            _filePath = filePath ?? AppSettings.ApiExcelFile;
            
            // 注册EPPlus许可证
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        /// <summary>
        /// 从Excel加载测试用例
        /// </summary>
        /// <returns>测试用例列表</returns>
        public List<Dictionary<string, object>> LoadTestCases()
        {
            try
            {
                var testCases = new List<Dictionary<string, object>>();
                
                if (!File.Exists(_filePath))
                {
                    TestContext.WriteLine($"Excel文件不存在: {_filePath}");
                    return testCases;
                }

                using (var package = new ExcelPackage(new FileInfo(_filePath)))
                {
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                    if (worksheet == null)
                    {
                        TestContext.WriteLine("Excel工作表为空");
                        return testCases;
                    }

                    // 获取列名
                    var columnNames = new List<string>();
                    for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                    {
                        var columnName = worksheet.Cells[1, col].Value?.ToString();
                        if (!string.IsNullOrEmpty(columnName))
                        {
                            columnNames.Add(columnName);
                        }
                    }

                    // 读取测试用例数据
                    for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                    {
                        var testCase = new Dictionary<string, object>();
                        testCase["row"] = row; // 保存行号，用于写回结果

                        // 读取基本字段
                        for (int col = 1; col <= columnNames.Count; col++)
                        {
                            var columnName = columnNames[col - 1];
                            var cellValue = worksheet.Cells[row, col].Value;
                            
                            if (columnName == "request" || columnName == "expected" || columnName == "data" || columnName == "headers")
                            {
                                // 尝试解析JSON或将其视为对象
                                testCase[columnName] = ParseJsonOrObject(cellValue?.ToString());
                            }
                            else
                            {
                                testCase[columnName] = cellValue;
                            }
                        }

                        testCases.Add(testCase);
                    }
                }

                return testCases;
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"加载Excel测试用例异常: {ex.Message}");
                return new List<Dictionary<string, object>>();
            }
        }

        /// <summary>
        /// 将测试结果写回Excel
        /// </summary>
        /// <param name="row">行号</param>
        /// <param name="result">测试结果</param>
        public void WriteResultToExcel(int row, Dictionary<string, object> result)
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    TestContext.WriteLine($"Excel文件不存在: {_filePath}");
                    return;
                }

                using (var package = new ExcelPackage(new FileInfo(_filePath)))
                {
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                    if (worksheet == null)
                    {
                        TestContext.WriteLine("Excel工作表为空");
                        return;
                    }

                    // 查找结果列
                    int resultColumn = 0;
                    for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                    {
                        if (worksheet.Cells[1, col].Value?.ToString().ToLower() == "result")
                        {
                            resultColumn = col;
                            break;
                        }
                    }

                    // 如果没有结果列，则添加一个
                    if (resultColumn == 0)
                    {
                        resultColumn = worksheet.Dimension.End.Column + 1;
                        worksheet.Cells[1, resultColumn].Value = "result";
                    }

                    // 写入测试结果
                    worksheet.Cells[row, resultColumn].Value = result["result"]?.ToString();
                    
                    // 保存Excel
                    package.Save();
                }
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"写入Excel结果异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 解析JSON字符串或对象
        /// </summary>
        private object ParseJsonOrObject(string value)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            try
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(value);
            }
            catch
            {
                return value;
            }
        }
    }
} 