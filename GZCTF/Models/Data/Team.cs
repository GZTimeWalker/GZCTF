using CTFServer.Models.Request.Info;
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
    public string CaptainId { get; set; } = string.Empty;

    /// <summary>
    /// 队长
    /// </summary>
    public UserInfo? Captain { get; set; }

    /// <summary>
    /// 比赛参与对象
    /// </summary>
    public List<Participation> Participations { get; set; } = new();

    /// <summary>
    /// 比赛对象
    /// </summary>
    public HashSet<Game>? Games { get; set; }

    /// <summary>
    /// 队员
    /// </summary>
    public HashSet<UserInfo> Members { get; set; } = new();

    #endregion Db Relationship

    [NotMapped]
    public string? AvatarUrl => AvatarHash is null ? null : $"/assets/{AvatarHash}/avatar";

    public void UpdateInviteToken() => InviteToken = Guid.NewGuid().ToString("N");

    internal void UpdateInfo(TeamUpdateModel model)
    {
        Name = string.IsNullOrEmpty(model.Name) ? Name : model.Name;
        Bio = model.Bio;
    }
}