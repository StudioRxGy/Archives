using Microsoft.EntityFrameworkCore;
using BlogApi.Domain.Entities;
using BlogApi.Domain.Interfaces;
using BlogApi.Infrastructure.Data;

namespace BlogApi.Infrastructure.Repositories;

/// <summary>
/// 用户仓储实现
/// </summary>
public class UserRepository : BaseRepository<User, int>, IUserRepository
{
    public UserRepository(BlogDbContext context) : base(context)
    {
    }

    /// <summary>
    /// 根据邮箱地址获取用户
    /// </summary>
    public async Task<User?> GetByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        return await _dbSet
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    }

    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    public async Task<User?> GetByUsernameAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return null;

        return await _dbSet
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
    }

    /// <summary>
    /// 检查邮箱地址是否已被使用
    /// </summary>
    public async Task<bool> EmailExistsAsync(string email, int? excludeUserId = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var query = _dbSet.Where(u => u.Email.ToLower() == email.ToLower());
        
        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.Id != excludeUserId.Value);
        }

        return await query.AnyAsync();
    }

    /// <summary>
    /// 检查用户名是否已被使用
    /// </summary>
    public async Task<bool> UsernameExistsAsync(string username, int? excludeUserId = null)
    {
        if (string.IsNullOrWhiteSpace(username))
            return false;

        var query = _dbSet.Where(u => u.Username.ToLower() == username.ToLower());
        
        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.Id != excludeUserId.Value);
        }

        return await query.AnyAsync();
    }

    /// <summary>
    /// 重写GetByIdAsync以包含相关数据
    /// </summary>
    public override async Task<User?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(u => u.Blogs)
            .Include(u => u.Files)
            .FirstOrDefaultAsync(u => u.Id == id);
    }
}