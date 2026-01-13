using BlogApi.Application.Commands.Auth;
using BlogApi.Application.DTOs;
using BlogApi.Application.DTOs.Common;
using BlogApi.Application.Queries.Auth;

namespace BlogApi.Application.Services;

/// <summary>
/// 认证应用服务接口
/// </summary>
public interface IAuthApplicationService
{
    /// <summary>
    /// 用户注册
    /// </summary>
    /// <param name="command">注册命令</param>
    /// <returns>认证结果</returns>
    Task<OperationResult<AuthResultDto>> RegisterAsync(RegisterCommand command);

    /// <summary>
    /// 用户登录
    /// </summary>
    /// <param name="command">登录命令</param>
    /// <returns>认证结果</returns>
    Task<OperationResult<AuthResultDto>> LoginAsync(LoginCommand command);

    /// <summary>
    /// 刷新访问令牌
    /// </summary>
    /// <param name="command">刷新令牌命令</param>
    /// <returns>令牌刷新结果</returns>
    Task<OperationResult<TokenRefreshResultDto>> RefreshTokenAsync(RefreshTokenCommand command);

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    /// <param name="query">获取当前用户查询</param>
    /// <returns>用户信息</returns>
    Task<OperationResult<UserDto>> GetCurrentUserAsync(GetCurrentUserQuery query);

    /// <summary>
    /// 注销用户（使令牌失效）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="refreshToken">刷新令牌</param>
    /// <returns>操作结果</returns>
    Task<OperationResult> LogoutAsync(int userId, string refreshToken);

    /// <summary>
    /// 验证访问令牌
    /// </summary>
    /// <param name="token">访问令牌</param>
    /// <returns>验证结果</returns>
    Task<OperationResult<UserDto>> ValidateTokenAsync(string token);
}