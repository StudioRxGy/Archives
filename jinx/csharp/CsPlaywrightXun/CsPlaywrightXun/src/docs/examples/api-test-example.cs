using Xunit;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using CsPlaywrightXun.src.playwright.Core.Base;
using CsPlaywrightXun.src.playwright.Core.Interfaces;
using CsPlaywrightXun.src.playwright.Core.Configuration;
using CsPlaywrightXun.src.playwright.Core.Attributes;
using CsPlaywrightXun.src.playwright.Services.Api;

namespace CsPlaywrightXun.src.docs.examples
{
    /// <summary>
    /// API 测试示例，展示 API 自动化测试的最佳实践
    /// </summary>
    [APITest]
    [TestCategory(TestCategory.ApiClient)]
    [TestPriority(TestPriority.High)]
    public class ApiTestExample : BaseApiTest
    {
        public ApiTestExample(IApiClient apiClient, TestConfiguration configuration, ILogger logger) 
            : base(apiClient, configuration, logger)
        {
        }
        
        #region 基础 API 测试
        
        /// <summary>
        /// GET 请求测试示例
        /// </summary>
        [Fact]
        [TestTag("GET")]
        [FastTest]
        public async Task GetUsers_ShouldReturnUserList()
        {
            // Arrange
            var request = new ApiRequest
            {
                Method = "GET",
                Endpoint = "/api/users",
                Headers = new Dictionary<string, string>
                {
                    ["Accept"] = "application/json"
                }
            };
            
            // Act
            var response = await ExecuteApiTestAsync<List<User>>(request);
            
            // Assert
            AssertStatusCode(response, 200);
            Assert.NotNull(response.Data);
            Assert.NotEmpty(response.Data);
            
            // 验证响应时间
            AssertResponseTime(response, 2000); // 2秒内响应
            
            // 验证数据结构
            var firstUser = response.Data.First();
            Assert.NotNull(firstUser.Id);
            Assert.NotEmpty(firstUser.Name);
            Assert.NotEmpty(firstUser.Email);
            
            _logger.LogInformation($"获取到 {response.Data.Count} 个用户");
        }
        
        /// <summary>
        /// POST 请求测试示例
        /// </summary>
        [Fact]
        [TestTag("POST")]
        public async Task CreateUser_WithValidData_ShouldReturnCreatedUser()
        {
            // Arrange
            var newUser = new CreateUserRequest
            {
                Name = "测试用户",
                Email = $"test_{Guid.NewGuid():N}@example.com",
                Age = 25,
                Department = "测试部门"
            };
            
            var request = new ApiRequest
            {
                Method = "POST",
                Endpoint = "/api/users",
                Body = newUser,
                Headers = new Dictionary<string, string>
                {
                    ["Content-Type"] = "application/json",
                    ["Accept"] = "application/json"
                }
            };
            
            // Act
            var response = await ExecuteApiTestAsync<User>(request);
            
            // Assert
            AssertStatusCode(response, 201); // Created
            Assert.NotNull(response.Data);
            Assert.NotNull(response.Data.Id);
            Assert.Equal(newUser.Name, response.Data.Name);
            Assert.Equal(newUser.Email, response.Data.Email);
            Assert.Equal(newUser.Age, response.Data.Age);
            
            _logger.LogInformation($"成功创建用户：{response.Data.Id}");
        }
        
        /// <summary>
        /// PUT 请求测试示例
        /// </summary>
        [Fact]
        [TestTag("PUT")]
        public async Task UpdateUser_WithValidData_ShouldReturnUpdatedUser()
        {
            // Arrange - 首先创建一个用户
            var createRequest = new ApiRequest
            {
                Method = "POST",
                Endpoint = "/api/users",
                Body = new CreateUserRequest
                {
                    Name = "原始用户",
                    Email = $"original_{Guid.NewGuid():N}@example.com",
                    Age = 30
                }
            };
            
            var createResponse = await ExecuteApiTestAsync<User>(createRequest);
            var userId = createResponse.Data.Id;
            
            // 准备更新数据
            var updateData = new UpdateUserRequest
            {
                Name = "更新后的用户",
                Age = 35,
                Department = "新部门"
            };
            
            var updateRequest = new ApiRequest
            {
                Method = "PUT",
                Endpoint = $"/api/users/{userId}",
                Body = updateData
            };
            
            // Act
            var response = await ExecuteApiTestAsync<User>(updateRequest);
            
            // Assert
            AssertStatusCode(response, 200);
            Assert.Equal(userId, response.Data.Id);
            Assert.Equal(updateData.Name, response.Data.Name);
            Assert.Equal(updateData.Age, response.Data.Age);
            Assert.Equal(updateData.Department, response.Data.Department);
            
            _logger.LogInformation($"成功更新用户：{userId}");
        }
        
        /// <summary>
        /// DELETE 请求测试示例
        /// </summary>
        [Fact]
        [TestTag("DELETE")]
        public async Task DeleteUser_WithValidId_ShouldReturnSuccess()
        {
            // Arrange - 首先创建一个用户
            var createRequest = new ApiRequest
            {
                Method = "POST",
                Endpoint = "/api/users",
                Body = new CreateUserRequest
                {
                    Name = "待删除用户",
                    Email = $"todelete_{Guid.NewGuid():N}@example.com"
                }
            };
            
            var createResponse = await ExecuteApiTestAsync<User>(createRequest);
            var userId = createResponse.Data.Id;
            
            var deleteRequest = new ApiRequest
            {
                Method = "DELETE",
                Endpoint = $"/api/users/{userId}"
            };
            
            // Act
            var response = await ExecuteApiTestAsync<object>(deleteRequest);
            
            // Assert
            AssertStatusCode(response, 204); // No Content
            
            // 验证用户确实被删除
            var getRequest = new ApiRequest
            {
                Method = "GET",
                Endpoint = $"/api/users/{userId}"
            };
            
            var getResponse = await ExecuteApiTestAsync<User>(getRequest);
            AssertStatusCode(getResponse, 404); // Not Found
            
            _logger.LogInformation($"成功删除用户：{userId}");
        }
        
        #endregion
        
        #region 错误处理测试
        
        /// <summary>
        /// 测试无效请求的错误处理
        /// </summary>
        [Theory]
        [InlineData("", "姓名不能为空")]
        [InlineData("invalid-email", "邮箱格式无效")]
        [InlineData("test@example.com", "邮箱已存在")]
        [TestTag("ErrorHandling")]
        public async Task CreateUser_WithInvalidData_ShouldReturnValidationError(string email, string expectedError)
        {
            // Arrange
            var invalidUser = new CreateUserRequest
            {
                Name = string.IsNullOrEmpty(email) ? "" : "测试用户",
                Email = email,
                Age = email == "test@example.com" ? 25 : -1 // 使用已存在的邮箱或无效年龄
            };
            
            var request = new ApiRequest
            {
                Method = "POST",
                Endpoint = "/api/users",
                Body = invalidUser
            };
            
            // Act
            var response = await ExecuteApiTestAsync<ErrorResponse>(request);
            
            // Assert
            AssertStatusCode(response, 400); // Bad Request
            Assert.NotNull(response.Data);
            Assert.Contains(expectedError, response.Data.Message);
            
            _logger.LogInformation($"正确处理了无效数据：{expectedError}");
        }
        
        /// <summary>
        /// 测试资源不存在的错误处理
        /// </summary>
        [Fact]
        [TestTag("ErrorHandling")]
        public async Task GetUser_WithNonExistentId_ShouldReturn404()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid().ToString();
            var request = new ApiRequest
            {
                Method = "GET",
                Endpoint = $"/api/users/{nonExistentId}"
            };
            
            // Act
            var response = await ExecuteApiTestAsync<ErrorResponse>(request);
            
            // Assert
            AssertStatusCode(response, 404);
            Assert.NotNull(response.Data);
            Assert.Contains("用户不存在", response.Data.Message);
            
            _logger.LogInformation($"正确处理了不存在的资源：{nonExistentId}");
        }
        
        /// <summary>
        /// 测试认证失败的错误处理
        /// </summary>
        [Fact]
        [TestTag("Authentication")]
        public async Task AccessProtectedResource_WithoutAuth_ShouldReturn401()
        {
            // Arrange
            var request = new ApiRequest
            {
                Method = "GET",
                Endpoint = "/api/admin/users"
                // 故意不添加认证头
            };
            
            // Act
            var response = await ExecuteApiTestAsync<ErrorResponse>(request);
            
            // Assert
            AssertStatusCode(response, 401); // Unauthorized
            
            _logger.LogInformation("正确处理了未认证的请求");
        }
        
        #endregion
        
        #region 数据驱动测试
        
        /// <summary>
        /// 数据驱动的用户创建测试
        /// </summary>
        [Theory]
        [JsonData("src/config/date/API/user_test_data.json")]
        [TestTag("DataDriven")]
        public async Task CreateUser_WithVariousData_ShouldBehaveCorrectly(UserTestData testData)
        {
            // Arrange
            var request = new ApiRequest
            {
                Method = "POST",
                Endpoint = "/api/users",
                Body = new CreateUserRequest
                {
                    Name = testData.Name,
                    Email = testData.Email,
                    Age = testData.Age,
                    Department = testData.Department
                }
            };
            
            // Act
            var response = await ExecuteApiTestAsync<User>(request);
            
            // Assert
            if (testData.ShouldSucceed)
            {
                AssertStatusCode(response, 201);
                Assert.NotNull(response.Data);
                Assert.Equal(testData.Name, response.Data.Name);
                Assert.Equal(testData.Email, response.Data.Email);
            }
            else
            {
                AssertStatusCode(response, 400);
                var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(response.RawContent);
                Assert.Contains(testData.ExpectedError, errorResponse.Message);
            }
            
            _logger.LogInformation($"数据驱动测试完成：{testData.TestName}");
        }
        
        #endregion
        
        #region 性能测试
        
        /// <summary>
        /// API 性能测试示例
        /// </summary>
        [Fact]
        [TestTag("Performance")]
        [SlowTest]
        public async Task GetUsers_PerformanceTest_ShouldMeetRequirements()
        {
            // Arrange
            var request = new ApiRequest
            {
                Method = "GET",
                Endpoint = "/api/users?limit=100"
            };
            
            var maxResponseTime = 1000; // 1秒
            var concurrentRequests = 10;
            var tasks = new List<Task<ApiResponse<List<User>>>>();
            
            // Act - 并发请求测试
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            for (int i = 0; i < concurrentRequests; i++)
            {
                tasks.Add(ExecuteApiTestAsync<List<User>>(request));
            }
            
            var responses = await Task.WhenAll(tasks);
            stopwatch.Stop();
            
            // Assert
            var averageResponseTime = stopwatch.ElapsedMilliseconds / concurrentRequests;
            Assert.True(averageResponseTime <= maxResponseTime, 
                $"平均响应时间 {averageResponseTime}ms 超过了最大限制 {maxResponseTime}ms");
            
            // 验证所有请求都成功
            Assert.All(responses, response => AssertStatusCode(response, 200));
            
            // 验证响应时间分布
            var responseTimes = responses.Select(r => r.ResponseTime.TotalMilliseconds).ToList();
            var maxTime = responseTimes.Max();
            var minTime = responseTimes.Min();
            
            _logger.LogInformation($"性能测试结果：平均 {averageResponseTime}ms，最大 {maxTime}ms，最小 {minTime}ms");
        }
        
        #endregion
        
        #region 集成测试
        
        /// <summary>
        /// 完整的用户生命周期集成测试
        /// </summary>
        [Fact]
        [TestTag("Integration")]
        [TestPriority(TestPriority.High)]
        public async Task UserLifecycle_CompleteFlow_ShouldWorkEndToEnd()
        {
            var testEmail = $"lifecycle_{Guid.NewGuid():N}@example.com";
            string userId = null;
            
            try
            {
                // 步骤1：创建用户
                var createRequest = new ApiRequest
                {
                    Method = "POST",
                    Endpoint = "/api/users",
                    Body = new CreateUserRequest
                    {
                        Name = "生命周期测试用户",
                        Email = testEmail,
                        Age = 28,
                        Department = "测试部门"
                    }
                };
                
                var createResponse = await ExecuteApiTestAsync<User>(createRequest);
                AssertStatusCode(createResponse, 201);
                userId = createResponse.Data.Id;
                
                _logger.LogInformation($"步骤1完成：创建用户 {userId}");
                
                // 步骤2：获取用户详情
                var getRequest = new ApiRequest
                {
                    Method = "GET",
                    Endpoint = $"/api/users/{userId}"
                };
                
                var getResponse = await ExecuteApiTestAsync<User>(getRequest);
                AssertStatusCode(getResponse, 200);
                Assert.Equal(testEmail, getResponse.Data.Email);
                
                _logger.LogInformation($"步骤2完成：获取用户详情");
                
                // 步骤3：更新用户信息
                var updateRequest = new ApiRequest
                {
                    Method = "PUT",
                    Endpoint = $"/api/users/{userId}",
                    Body = new UpdateUserRequest
                    {
                        Name = "更新后的生命周期测试用户",
                        Age = 30,
                        Department = "更新后的部门"
                    }
                };
                
                var updateResponse = await ExecuteApiTestAsync<User>(updateRequest);
                AssertStatusCode(updateResponse, 200);
                Assert.Equal("更新后的生命周期测试用户", updateResponse.Data.Name);
                
                _logger.LogInformation($"步骤3完成：更新用户信息");
                
                // 步骤4：验证更新后的信息
                var verifyResponse = await ExecuteApiTestAsync<User>(getRequest);
                AssertStatusCode(verifyResponse, 200);
                Assert.Equal("更新后的生命周期测试用户", verifyResponse.Data.Name);
                Assert.Equal(30, verifyResponse.Data.Age);
                
                _logger.LogInformation($"步骤4完成：验证更新结果");
                
                // 步骤5：删除用户
                var deleteRequest = new ApiRequest
                {
                    Method = "DELETE",
                    Endpoint = $"/api/users/{userId}"
                };
                
                var deleteResponse = await ExecuteApiTestAsync<object>(deleteRequest);
                AssertStatusCode(deleteResponse, 204);
                
                _logger.LogInformation($"步骤5完成：删除用户");
                
                // 步骤6：验证用户已被删除
                var finalGetResponse = await ExecuteApiTestAsync<User>(getRequest);
                AssertStatusCode(finalGetResponse, 404);
                
                _logger.LogInformation($"步骤6完成：验证用户已删除");
                
                _logger.LogInformation("用户生命周期集成测试完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"用户生命周期测试失败，用户ID：{userId}");
                
                // 清理：如果测试失败，尝试删除创建的用户
                if (!string.IsNullOrEmpty(userId))
                {
                    try
                    {
                        var cleanupRequest = new ApiRequest
                        {
                            Method = "DELETE",
                            Endpoint = $"/api/users/{userId}"
                        };
                        await ExecuteApiTestAsync<object>(cleanupRequest);
                        _logger.LogInformation($"清理完成：删除用户 {userId}");
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogWarning(cleanupEx, $"清理失败：无法删除用户 {userId}");
                    }
                }
                
                throw;
            }
        }
        
        #endregion
        
        #region 辅助方法
        
        /// <summary>
        /// 发送 API 请求的具体实现
        /// </summary>
        protected override async Task<ApiResponse<T>> SendRequestAsync<T>(ApiRequest request)
        {
            var httpResponse = request.Method.ToUpper() switch
            {
                "GET" => await _apiClient.GetAsync(request.Endpoint, request.Headers),
                "POST" => await _apiClient.PostAsync(request.Endpoint, request.Body, request.Headers),
                "PUT" => await _apiClient.PutAsync(request.Endpoint, request.Body, request.Headers),
                "DELETE" => await _apiClient.DeleteAsync(request.Endpoint, request.Headers),
                _ => throw new NotSupportedException($"不支持的 HTTP 方法: {request.Method}")
            };
            
            var content = await httpResponse.Content.ReadAsStringAsync();
            
            T data = default;
            if (!string.IsNullOrEmpty(content) && httpResponse.IsSuccessStatusCode)
            {
                try
                {
                    data = JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, $"JSON 反序列化失败: {content}");
                }
            }
            
            return new ApiResponse<T>
            {
                StatusCode = (int)httpResponse.StatusCode,
                Data = data,
                RawContent = content,
                Headers = httpResponse.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value))
            };
        }
        
        #endregion
    }
    
    #region 数据模型
    
    /// <summary>
    /// 用户模型
    /// </summary>
    public class User
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Department { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
    
    /// <summary>
    /// 创建用户请求模型
    /// </summary>
    public class CreateUserRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Department { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// 更新用户请求模型
    /// </summary>
    public class UpdateUserRequest
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Department { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// 错误响应模型
    /// </summary>
    public class ErrorResponse
    {
        public string Message { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public List<string> Details { get; set; } = new();
        public DateTime Timestamp { get; set; }
    }
    
    /// <summary>
    /// 用户测试数据模型
    /// </summary>
    public class UserTestData
    {
        public string TestName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Department { get; set; } = string.Empty;
        public bool ShouldSucceed { get; set; }
        public string ExpectedError { get; set; } = string.Empty;
    }
    
    #endregion
}