using CTFServer.Models.Request.Teams;
using CTFServer.Utils;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CTFServer.Models;

public class Team
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 队伍名称
    /// </summary>
    [Required]
    [MaxLength(16)]
    [RegularExpression("[0-9A-Za-z]+")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 队伍 Bio
    /// </summary>
    [MaxLength(32)]
    public string? Bio { get; set; } = string.Empty;

    /// <summary>
    /// 头像哈希
    /// </summary>
    public string? AvatarHash { get; set; }

    /// <summary>
    /// 队伍是否为锁定状态
    /// </summary>
    public bool Locked { get; set; } = false;

    /// <summary>
    /// 邀请 Token
    /// </summary>
    public string InviteToken { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// 邀请 Code
    /// </summary>
    [NotMapped]
    public string InviteCode => $"{Name}:{Id}:{InviteToken}";

    #region Db Relationship

    /// <summary>
    /// 队长用户ID
    /// </summary>
    [Required]
    public string CaptainId { get; set; } = string.Empty;

    /// <summary>
    /// 队长
    /// </summary>
    public UserInfo? Captain { get; set; }

    /// <summary>
    /// 比赛
    /// </summary>
    public List<Participation> Games { get; set; } = new();

    /// <summary>
    /// 队员
    /// </summary>
    public HashSet<UserInfo> Members { get; set; } = new();

    #endregion Db Relationship

    [NotMapped]
    public string? AvatarUrl => AvatarHash is null ? null : $"/assets/{AvatarHash}/avatar";

    public void UpdateInviteToken() => InviteToken = Guid.NewGuid().ToString("N");

    public void UpdateInfo(TeamUpdateModel model)
    {
        Name = model.Name ?? Name;
        Bio = model.Bio;
    }
}