using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace Nop.Core.Caching;

public partial class DistributedCacheLocker : ILocker
{
    #region Fields

    protected static readonly string _running = JsonConvert.SerializeObject(TaskStatus.Running);
    protected readonly IDistributedCache _distributedCache;

    #endregion

    #region Ctor

    public DistributedCacheLocker(IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
    }

    #endregion

    #region Methods

    /// <summary>
    /// 执行带有排他锁的异步任务
    /// </summary>
    /// <param name="resource">The key we are locking on</param>
    /// <param name="expirationTime">The time after which the lock will automatically be expired</param>
    /// <param name="action">Asynchronous task to be performed with locking</param>
    /// <returns>A task that resolves true if lock was acquired and action was performed; otherwise false</returns>
    public async Task<bool> PerformActionWithLockAsync(string resource, TimeSpan expirationTime, Func<Task> action)
    {
        //ensure that lock is acquired
        if (!string.IsNullOrEmpty(await _distributedCache.GetStringAsync(resource)))
            return false;

        try
        {
            await _distributedCache.SetStringAsync(resource, resource, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expirationTime
            });

            await action();

            return true;
        }
        finally
        {
            //release lock even if action fails
            await _distributedCache.RemoveAsync(resource);
        }
    }

    /// <summary>
    /// 启动一个带有“heartbeat”的后台任务：一个状态标志，将定期更新以向其发送信号
    /// 其他人知道任务正在运行，并阻止他们启动相同的任务
    /// </summary>
    /// <param name="key">The key of the background task</param>
    /// <param name="expirationTime">The time after which the heartbeat key will automatically be expired. Should be longer than <paramref name="heartbeatInterval"/></param>
    /// <param name="heartbeatInterval">The interval at which to update the heartbeat, if required by the implementation</param>
    /// <param name="action">Asynchronous background task to be performed</param>
    /// <param name="cancellationTokenSource">A CancellationTokenSource for manually canceling the task</param>
    /// <returns>如果获得锁并执行操作，则任务解析为true；否则false</returns>
    public async Task RunWithHeartbeatAsync(string key, TimeSpan expirationTime, TimeSpan heartbeatInterval, Func<CancellationToken, Task> action, CancellationTokenSource cancellationTokenSource = default)
    {
        if (!string.IsNullOrEmpty(await _distributedCache.GetStringAsync(key)))
            return;

        var tokenSource = cancellationTokenSource ?? new CancellationTokenSource();

        try
        {
            // 尽早运行heartbeat以最小化多次执行的风险
            await _distributedCache.SetStringAsync(
                key,
                _running,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expirationTime },
                token: tokenSource.Token);

            await using var timer = new Timer(
                callback: _ =>
                {
                    try
                    {
                        tokenSource.Token.ThrowIfCancellationRequested();
                        var status = _distributedCache.GetString(key);
                        if (!string.IsNullOrEmpty(status) && JsonConvert.DeserializeObject<TaskStatus>(status) ==
                            TaskStatus.Canceled)
                        {
                            tokenSource.Cancel();
                            return;
                        }

                        _distributedCache.SetString(
                            key,
                            _running,
                            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expirationTime });
                    }
                    catch (OperationCanceledException) { }
                },
                state: null,
                dueTime: 0,
                period: (int)heartbeatInterval.TotalMilliseconds);

            await action(tokenSource.Token);
        }
        catch (OperationCanceledException) { }
        finally
        {
            await _distributedCache.RemoveAsync(key);
        }
    }

    /// <summary>
    /// 尝试通过在下一次心跳时将后台任务标记为取消来取消它
    /// </summary>
    /// <param name="key">The task's key</param>
    /// <param name="expirationTime">由于系统关闭或其他原因，任务将被认为停止的时间,即使没有明确取消</param>
    /// <returns>表示请求取消任务的任务。此任务的完成并不一定意味着该任务已被取消，仅表示已请求取消</returns>
    public async Task CancelTaskAsync(string key, TimeSpan expirationTime)
    {
        var status = await _distributedCache.GetStringAsync(key);
        if (!string.IsNullOrEmpty(status) &&
            JsonConvert.DeserializeObject<TaskStatus>(status) != TaskStatus.Canceled)
            await _distributedCache.SetStringAsync(
                key,
                JsonConvert.SerializeObject(TaskStatus.Canceled),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expirationTime });
    }

    /// <summary>
    /// 检查是否有后台任务正在运行
    /// </summary>
    /// <param name="key">The task's key</param>
    /// <returns>如果后台任务正在运行，则解析为true的任务；否则false</returns>
    public async Task<bool> IsTaskRunningAsync(string key)
    {
        return !string.IsNullOrEmpty(await _distributedCache.GetStringAsync(key));
    }

    #endregion
}