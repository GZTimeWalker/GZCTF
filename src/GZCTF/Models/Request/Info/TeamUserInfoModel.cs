using System.Text.Json.Serialization;

namespace GZCTF.Models.Request.Info;

/// <summary>
/// Team member information
/// </summary>
public class TeamUserInfoModel
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Username
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Bio
    /// </summary>
    public string? Bio { get; set; }

    /// <summary>
    /// Avatar URL
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// Is Captain
    /// </summary>
    public bool Captain { get; set; }

    /// <summary>
    /// Real name, used for generating the scoreboard
    /// </summary>
    [JsonIgnore]
    public string? RealName { get; set; }

    /// <summary>
    /// Student number, used for generating the scoreboard
    /// </summary>
    [JsonIgnore]
    public string? StudentNumber { get; set; }
}
