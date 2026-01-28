namespace Nop.Core;

/// <summary>
/// 表示实体的基类
/// </summary>
public abstract partial class BaseEntity
{
    /// <summary>
    /// 获取或设置实体标识符
    /// </summary>
    public int Id { get; set; }
}