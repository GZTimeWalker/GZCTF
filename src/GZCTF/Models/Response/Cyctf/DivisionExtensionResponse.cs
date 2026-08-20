using GZCTF.Models.Data.Cyctf;

namespace GZCTF.Models.Response.Cyctf;

/// <summary>
/// 组别扩展信息响应
/// </summary>
public class DivisionExtensionResponse
{
    public int DivisionId { get; set; }
    public int? MinTeamSize { get; set; }
    public int? MaxTeamSize { get; set; }
    public string? RegistrationFields { get; set; }
    public DateTimeOffset CreateTime { get; set; }
    public DateTimeOffset UpdateTime { get; set; }

    public static DivisionExtensionResponse FromEntity(DivisionExtension entity) => new()
    {
        DivisionId = entity.DivisionId,
        MinTeamSize = entity.MinTeamSize,
        MaxTeamSize = entity.MaxTeamSize,
        RegistrationFields = entity.RegistrationFields,
        CreateTime = entity.CreateTime,
        UpdateTime = entity.UpdateTime
    };
}
