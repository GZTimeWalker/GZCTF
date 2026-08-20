using GZCTF.Models.Data.Cyctf;

namespace GZCTF.Models.Response.Cyctf;

/// <summary>
/// 报名响应
/// </summary>
public class RegistrationResponse
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public int TeamId { get; set; }
    public string? TeamName { get; set; }
    public int DivisionId { get; set; }
    public string? DivisionName { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? FormData { get; set; }
    public string? ReviewNote { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public DateTimeOffset CreateTime { get; set; }
    public DateTimeOffset UpdateTime { get; set; }

    public static RegistrationResponse FromEntity(Registration entity) => new()
    {
        Id = entity.Id,
        GameId = entity.GameId,
        TeamId = entity.TeamId,
        TeamName = entity.Team?.Name,
        DivisionId = entity.DivisionId,
        DivisionName = entity.Division?.Name,
        Status = entity.Status,
        FormData = entity.FormData,
        ReviewNote = entity.ReviewNote,
        ReviewedBy = entity.Reviewer?.UserName,
        ReviewedAt = entity.ReviewedAt,
        CreateTime = entity.CreateTime,
        UpdateTime = entity.UpdateTime
    };
}
