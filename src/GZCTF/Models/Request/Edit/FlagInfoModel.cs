namespace GZCTF.Models.Request.Edit;

/// <summary>
/// Flag 信息（Edit）
/// </summary>
public class FlagInfoModel
{
    /// <summary>
    /// Flag Id
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Flag文本
    /// </summary>
    public string Flag { get; set; } = string.Empty;

    /// <summary>
    /// Flag 对应附件
    /// </summary>
    public Attachment? Attachment { get; set; }

    internal static FlagInfoModel FromFlagContext(FlagContext context) =>
        new() { Id = context.Id, Flag = context.Flag, Attachment = context.Attachment };
}
