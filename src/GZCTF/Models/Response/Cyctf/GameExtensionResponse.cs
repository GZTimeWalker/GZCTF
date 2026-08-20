using GZCTF.Models.Data.Cyctf;

namespace GZCTF.Models.Response.Cyctf;

/// <summary>
/// 比赛扩展信息响应
/// </summary>
public class GameExtensionResponse
{
    public int GameId { get; set; }
    public DateTimeOffset RegistrationStartTime { get; set; }
    public DateTimeOffset RegistrationEndTime { get; set; }
    public int? MaxTeams { get; set; }
    public bool ShowRegistrationCount { get; set; }
    public bool ShowEventTime { get; set; }
    public int CurrentTeams { get; set; }
    public string? EmailWhitelist { get; set; }
    public string? Status { get; set; }
    public DateTimeOffset CreateTime { get; set; }
    public DateTimeOffset UpdateTime { get; set; }

    public static GameExtensionResponse FromEntity(GameExtension entity) => new()
    {
        GameId = entity.GameId,
        RegistrationStartTime = entity.RegistrationStartTime,
        RegistrationEndTime = entity.RegistrationEndTime,
        MaxTeams = entity.MaxTeams,
        ShowRegistrationCount = entity.ShowRegistrationCount,
        ShowEventTime = entity.ShowEventTime,
        CurrentTeams = entity.CurrentTeams,
        EmailWhitelist = entity.EmailWhitelist,
        Status = entity.Status,
        CreateTime = entity.CreateTime,
        UpdateTime = entity.UpdateTime
    };
}
