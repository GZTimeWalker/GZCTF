namespace GZCTF.Models.Request.Shared;

public class ClientFlagContext
{
    /// <summary>
    /// 题目实例的关闭时间
    /// </summary>
    public DateTimeOffset? CloseTime { get; set; }

    /// <summary>
    /// 题目实例的连接方式
    /// </summary>
    public string? InstanceEntry { get; set; }

    /// <summary>
    /// 附件 Url
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// 附件文件大小
    /// </summary>
    public long? FileSize { get; set; }
}
