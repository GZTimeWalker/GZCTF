namespace GZCTF.Models.Data;

public class Instance
{
    /// <summary>
    /// 题目是否已经解决
    /// </summary>
    public bool IsSolved { get; set; }

    /// <summary>
    /// 题目是否已经加载
    /// </summary>
    public bool IsLoaded { get; set; }

    /// <summary>
    /// 最后一次容器操作的时间，确保单题目容器操作不会过于频繁
    /// </summary>
    public DateTimeOffset LastContainerOperation { get; set; } = DateTimeOffset.MinValue;

    #region Db Relationship

    public int? FlagId { get; set; }

    /// <summary>
    /// Flag 上下文对象
    /// </summary>
    public FlagContext? FlagContext { get; set; }

    public Guid? ContainerId { get; set; }

    /// <summary>
    /// 容器对象
    /// </summary>
    public Container? Container { get; set; }

    #endregion
}
