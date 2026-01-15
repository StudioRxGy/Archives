using BlogApi.Domain.Entities;

namespace BlogApi.Domain.Interfaces;

/// <summary>
/// 用户实体仓储接口
/// </summary>
public interface IUserRepository : IRepository<User, int>
{
    /// <summary>
    /// 根据邮箱地址获取用户
    /// </summary>
    /// <param name="email">邮箱地址</param>
    /// <returns>找到的用户，如果不存在则返回null</returns>
    Task<User?> GetByEmailAsync(string email);
    
    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    /// <param name="username">用户名</param>
    /// <returns>找到的用户，如果不存在则返回null</returns>
    Task<User?> GetByUsernameAsync(string username);
    
    /// <summary>
    /// 检查邮箱地址是否已被使用
    /// </summary>
    /// <param name="email">要检查的邮箱</param>
    /// <param name="excludeUserId">可选的用户ID，用于排除检查</param>
    /// <returns>邮箱存在返回true，否则返回false</returns>
    Task<bool> EmailExistsAsync(string email, int? excludeUserId = null);
    
    /// <summary>
    /// 检查用户名是否已被使用
    /// </summary>
    /// <param name="username">要检查的用户名</param>
    /// <param name="excludeUserId">可选的用户ID，用于排除检查</param>
    /// <returns>用户名存在返回true，否则返回false</returns>
    Task<bool> UsernameExistsAsync(string username, int? excludeUserId = null);
}