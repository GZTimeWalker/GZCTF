using GZCTF.Models.Data.Cyctf;

namespace GZCTF.Models.Response.Cyctf;

/// <summary>
/// 赞助商响应
/// </summary>
public class SponsorResponse
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public string ShortName { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? TypeLabel { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset CreateTime { get; set; }
    public DateTimeOffset UpdateTime { get; set; }

    public static SponsorResponse FromEntity(Sponsor entity) => new()
    {
        Id = entity.Id,
        GameId = entity.GameId,
        ShortName = entity.ShortName,
        FullName = entity.FullName,
        Website = entity.Website,
        LogoUrl = entity.LogoUrl,
        Type = entity.Type,
        TypeLabel = entity.TypeLabel,
        SortOrder = entity.SortOrder,
        CreateTime = entity.CreateTime,
        UpdateTime = entity.UpdateTime
    };
}
