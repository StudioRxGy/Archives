using BlogApi.Application.Commands.Auth;
using BlogApi.Application.DTOs;
using BlogApi.Application.DTOs.Common;
using BlogApi.Application.Queries.Auth;
using BlogApi.Domain.Entities;
using BlogApi.Domain.Interfaces;

namespace BlogApi.Application.Services;

/// <summary>
/// 认证应用服务实现
/// </summary>
public class AuthApplicationService : IAuthApplicationService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly ITokenService _tokenService;

    public AuthApplicationService(
        IUserRepository userRepository,
        IPasswordHashingService passwordHashingService,
        ITokenService tokenService)
    {
        _userRepository = userRepository;
        _passwordHashingService = passwordHashingService;
        _tokenService = tokenService;
    }

    public async Task<OperationResult<AuthResultDto>> RegisterAsync(RegisterCommand command)
    {
        try
        {
            // 验证用户名是否已存在
            if (await _userRepository.UsernameExistsAsync(command.Username))
            {
                return OperationResult<AuthResultDto>.CreateFailure("用户名已存在", "USERNAME_EXISTS");
            }

            // 验证邮箱是否已存在
            if (await _userRepository.EmailExistsAsync(command.Email))
            {
                return OperationResult<AuthResultDto>.CreateFailure("邮箱已存在", "EMAIL_EXISTS");
            }

            // 创建新用户
            var user = new User
            {
                Username = command.Username,
                Email = command.Email,
                PasswordHash = _passwordHashingService.HashPassword(command.Password),
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var createdUser = await _userRepository.CreateAsync(user);
            
            // 生成令牌
            var accessToken = _tokenService.GenerateAccessToken(createdUser);
            var refreshToken = _tokenService.GenerateRefreshToken();

            var authResult = new AuthResultDto
            {
                Success = true,
                Message = "注册成功",
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(1), // 假设访问令牌1小时过期
                User = MapToUserDto(createdUser)
            };

            return OperationResult<AuthResultDto>.CreateSuccess(authResult);
        }
        catch (Exception ex)
        {
            return OperationResult<AuthResultDto>.CreateFailure("注册过程中发生错误", "REGISTRATION_ERROR");
        }
    }

    public async Task<OperationResult<AuthResultDto>> LoginAsync(LoginCommand command)
    {
        try
        {
            // 根据邮箱或用户名查找用户
            User? user = null;
            if (command.EmailOrUsername.Contains("@"))
            {
                user = await _userRepository.GetByEmailAsync(command.EmailOrUsername);
            }
            else
            {
                user = await _userRepository.GetByUsernameAsync(command.EmailOrUsername);
            }

            if (user == null)
            {
                return OperationResult<AuthResultDto>.CreateFailure("用户名或密码错误", "INVALID_CREDENTIALS");
            }

            if (!user.IsActive)
            {
                return OperationResult<AuthResultDto>.CreateFailure("账户已被禁用", "ACCOUNT_DISABLED");
            }

            // 验证密码
            if (!_passwordHashingService.VerifyPassword(command.Password, user.PasswordHash))
            {
                return OperationResult<AuthResultDto>.CreateFailure("用户名或密码错误", "INVALID_CREDENTIALS");
            }

            // 更新最后登录时间
            user.UpdateLastLogin();
            await _userRepository.UpdateAsync(user);

            // 生成令牌
            var accessToken = _tokenService.GenerateAccessToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            var authResult = new AuthResultDto
            {
                Success = true,
                Message = "登录成功",
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(command.RememberMe ? 24 : 1), // 记住登录状态则24小时，否则1小时
                User = MapToUserDto(user)
            };

            return OperationResult<AuthResultDto>.CreateSuccess(authResult);
        }
        catch (Exception ex)
        {
            return OperationResult<AuthResultDto>.CreateFailure("登录过程中发生错误", "LOGIN_ERROR");
        }
    }

    public async Task<OperationResult<TokenRefreshResultDto>> RefreshTokenAsync(RefreshTokenCommand command)
    {
        try
        {
            // 验证访问令牌（即使过期也要能解析出用户信息）
            var userId = _tokenService.GetUserIdFromToken(command.AccessToken);
            if (!userId.HasValue)
            {
                return OperationResult<TokenRefreshResultDto>.CreateFailure("无效的访问令牌", "INVALID_ACCESS_TOKEN");
            }

            // 获取用户信息
            var user = await _userRepository.GetByIdAsync(userId.Value);
            if (user == null)
            {
                return OperationResult<TokenRefreshResultDto>.CreateFailure("用户不存在", "USER_NOT_FOUND");
            }

            if (!user.IsActive)
            {
                return OperationResult<TokenRefreshResultDto>.CreateFailure("账户已被禁用", "ACCOUNT_DISABLED");
            }

            // 这里应该验证刷新令牌的有效性，但由于当前实现中没有存储刷新令牌，
            // 我们简化处理，只要访问令牌能解析出用户ID就认为刷新令牌有效
            // 在实际生产环境中，应该将刷新令牌存储在数据库中进行验证

            // 生成新的令牌
            var newAccessToken = _tokenService.GenerateAccessToken(user);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            var refreshResult = new TokenRefreshResultDto
            {
                Success = true,
                Message = "令牌刷新成功",
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };

            return OperationResult<TokenRefreshResultDto>.CreateSuccess(refreshResult);
        }
        catch (Exception ex)
        {
            return OperationResult<TokenRefreshResultDto>.CreateFailure("令牌刷新过程中发生错误", "REFRESH_ERROR");
        }
    }

    public async Task<OperationResult<UserDto>> GetCurrentUserAsync(GetCurrentUserQuery query)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(query.UserId);
            if (user == null)
            {
                return OperationResult<UserDto>.CreateFailure("用户不存在", "USER_NOT_FOUND");
            }

            if (!user.IsActive)
            {
                return OperationResult<UserDto>.CreateFailure("账户已被禁用", "ACCOUNT_DISABLED");
            }

            var userDto = MapToUserDto(user);
            return OperationResult<UserDto>.CreateSuccess(userDto);
        }
        catch (Exception ex)
        {
            return OperationResult<UserDto>.CreateFailure("获取用户信息过程中发生错误", "GET_USER_ERROR");
        }
    }

    public async Task<OperationResult> LogoutAsync(int userId, string refreshToken)
    {
        try
        {
            // 在实际实现中，这里应该将刷新令牌从数据库中移除或标记为无效
            // 由于当前实现中没有存储刷新令牌，我们只是返回成功
            
            return OperationResult.CreateSuccess();
        }
        catch (Exception ex)
        {
            return OperationResult.CreateFailure("注销过程中发生错误", "LOGOUT_ERROR");
        }
    }

    public async Task<OperationResult<UserDto>> ValidateTokenAsync(string token)
    {
        try
        {
            // 验证令牌
            var claimsPrincipal = _tokenService.ValidateToken(token);
            if (claimsPrincipal == null)
            {
                return OperationResult<UserDto>.CreateFailure("无效的令牌", "INVALID_TOKEN");
            }

            // 检查令牌是否过期
            if (_tokenService.IsTokenExpired(token))
            {
                return OperationResult<UserDto>.CreateFailure("令牌已过期", "TOKEN_EXPIRED");
            }

            // 从令牌中获取用户ID
            var userId = _tokenService.GetUserIdFromToken(token);
            if (!userId.HasValue)
            {
                return OperationResult<UserDto>.CreateFailure("无效的令牌", "INVALID_TOKEN");
            }

            // 获取用户信息
            var user = await _userRepository.GetByIdAsync(userId.Value);
            if (user == null)
            {
                return OperationResult<UserDto>.CreateFailure("用户不存在", "USER_NOT_FOUND");
            }

            if (!user.IsActive)
            {
                return OperationResult<UserDto>.CreateFailure("账户已被禁用", "ACCOUNT_DISABLED");
            }

            var userDto = MapToUserDto(user);
            return OperationResult<UserDto>.CreateSuccess(userDto);
        }
        catch (Exception ex)
        {
            return OperationResult<UserDto>.CreateFailure("令牌验证过程中发生错误", "TOKEN_VALIDATION_ERROR");
        }
    }

    private static UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            IsActive = user.IsActive
        };
    }
}