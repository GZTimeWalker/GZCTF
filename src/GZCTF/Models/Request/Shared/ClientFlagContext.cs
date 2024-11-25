namespace GZCTF.Models.Request.Shared;

public class ClientFlagContext
{
    /// <summary>
    /// Close time of the challenge instance
    /// </summary>
    public DateTimeOffset? CloseTime { get; set; }

    /// <summary>
    /// Connection method of the challenge instance
    /// </summary>
    public string? InstanceEntry { get; set; }

    /// <summary>
    /// Attachment URL
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Attachment file size
    /// </summary>
    public long? FileSize { get; set; }
}
