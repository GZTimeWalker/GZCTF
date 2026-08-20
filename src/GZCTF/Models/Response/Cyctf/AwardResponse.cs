using GZCTF.Models.Data.Cyctf;

namespace GZCTF.Models.Response.Cyctf;

/// <summary>
/// 奖项响应
/// </summary>
public class AwardResponse
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset CreateTime { get; set; }
    public DateTimeOffset UpdateTime { get; set; }

    public static AwardResponse FromEntity(Award entity) => new()
    {
        Id = entity.Id,
        GameId = entity.GameId,
        Name = entity.Name,
        Description = entity.Description,
        PrimaryColor = entity.PrimaryColor,
        SecondaryColor = entity.SecondaryColor,
        SortOrder = entity.SortOrder,
        CreateTime = entity.CreateTime,
        UpdateTime = entity.UpdateTime
    };
}
