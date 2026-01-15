using Microsoft.AspNetCore.Mvc;
using BlogApi.Application.Services;
using BlogApi.Application.Commands.Auth;
using BlogApi.Application.DTOs.Common;

namespace BlogApi.Api.Controllers;

/// <summary>
/// 认证控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthApplicationService _authApplicationService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthApplicationService authApplicationService,
        ILogger<AuthController> logger)
    {
        _authApplicationService = authApplicationService;
        _logger = logger;
    }

    /// <summary>
    /// 用户注册
    /// </summary>
    /// <param name="command">注册信息</param>
    /// <returns>注册结果</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
    {
        try
        {
            // 验证模型状态
            if (!ModelState.IsValid)
            {
                var validationErrors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .SelectMany(x => x.Value!.Errors.Select(e => $"{x.Key}: {e.ErrorMessage}"))
                    .ToList();

                var validationResponse = ApiResponse<object>.CreateValidationFailure(validationErrors);
                return BadRequest(validationResponse);
            }

            var result = await _authApplicationService.RegisterAsync(command);

            if (result.Success)
            {
                _logger.LogInformation("用户注册成功: {Username}", command.Username);
                var response = ApiResponse<object>.CreateSuccess(result.Data!, "注册成功");
                return Ok(response);
            }
            else
            {
                _logger.LogWarning("用户注册失败: {Username}, 错误: {Error}", command.Username, result.ErrorMessage);
                var response = ApiResponse<object>.CreateFailure(result.ErrorMessage, new List<string> { result.ErrorCode });
                return BadRequest(response);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "用户注册过程中发生异常: {Username}", command.Username);
            var response = ApiResponse<object>.CreateFailure("注册过程中发生错误", new List<string> { "REGISTRATION_ERROR" });
            return StatusCode(StatusCodes.Status500InternalServerError, response);
        }
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    /// <param name="command">登录信息</param>
    /// <returns>登录结果</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        try
        {
            // 验证模型状态
            if (!ModelState.IsValid)
            {
                var validationErrors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .SelectMany(x => x.Value!.Errors.Select(e => $"{x.Key}: {e.ErrorMessage}"))
                    .ToList();

                var validationResponse = ApiResponse<object>.CreateValidationFailure(validationErrors);
                return BadRequest(validationResponse);
            }

            var result = await _authApplicationService.LoginAsync(command);

            if (result.Success)
            {
                _logger.LogInformation("用户登录成功: {EmailOrUsername}", command.EmailOrUsername);
                var response = ApiResponse<object>.CreateSuccess(result.Data!, "登录成功");
                return Ok(response);
            }
            else
            {
                _logger.LogWarning("用户登录失败: {EmailOrUsername}, 错误: {Error}", command.EmailOrUsername, result.ErrorMessage);
                
                // 根据错误类型返回不同的状态码
                if (result.ErrorCode == "INVALID_CREDENTIALS")
                {
                    var response = ApiResponse<object>.CreateFailure(result.ErrorMessage, new List<string> { result.ErrorCode });
                    return Unauthorized(response);
                }
                else
                {
                    var response = ApiResponse<object>.CreateFailure(result.ErrorMessage, new List<string> { result.ErrorCode });
                    return BadRequest(response);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "用户登录过程中发生异常: {EmailOrUsername}", command.EmailOrUsername);
            var response = ApiResponse<object>.CreateFailure("登录过程中发生错误", new List<string> { "LOGIN_ERROR" });
            return StatusCode(StatusCodes.Status500InternalServerError, response);
        }
    }

    /// <summary>
    /// 刷新访问令牌
    /// </summary>
    /// <param name="command">刷新令牌信息</param>
    /// <returns>新的访问令牌</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command)
    {
        try
        {
            // 验证模型状态
            if (!ModelState.IsValid)
            {
                var validationErrors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .SelectMany(x => x.Value!.Errors.Select(e => $"{x.Key}: {e.ErrorMessage}"))
                    .ToList();

                var validationResponse = ApiResponse<object>.CreateValidationFailure(validationErrors);
                return BadRequest(validationResponse);
            }

            var result = await _authApplicationService.RefreshTokenAsync(command);

            if (result.Success)
            {
                _logger.LogInformation("令牌刷新成功");
                var response = ApiResponse<object>.CreateSuccess(result.Data!, "令牌刷新成功");
                return Ok(response);
            }
            else
            {
                _logger.LogWarning("令牌刷新失败, 错误: {Error}", result.ErrorMessage);
                
                // 根据错误类型返回不同的状态码
                if (result.ErrorCode == "INVALID_ACCESS_TOKEN" || result.ErrorCode == "TOKEN_EXPIRED")
                {
                    var response = ApiResponse<object>.CreateFailure(result.ErrorMessage, new List<string> { result.ErrorCode });
                    return Unauthorized(response);
                }
                else
                {
                    var response = ApiResponse<object>.CreateFailure(result.ErrorMessage, new List<string> { result.ErrorCode });
                    return BadRequest(response);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "令牌刷新过程中发生异常");
            var response = ApiResponse<object>.CreateFailure("令牌刷新过程中发生错误", new List<string> { "REFRESH_ERROR" });
            return StatusCode(StatusCodes.Status500InternalServerError, response);
        }
    }
}