using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GZCTF.Models.Request.Admin;
using GZCTF.Models.Request.Info;

namespace GZCTF.Models.Data;

public class Team
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 队伍名称
    /// </summary>
    [Required]
    [MaxLength(Limits.MaxTeamNameLength)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 队伍 Bio
    /// </summary>
    [MaxLength(Limits.MaxTeamBioLength)]
    public string? Bio { get; set; } = string.Empty;

    /// <summary>
    /// 头像哈希
    /// </summary>
    [MaxLength(Limits.FileHashLength)]
    public string? AvatarHash { get; set; }

    /// <summary>
    /// 队伍是否为锁定状态
    /// </summary>
    public bool Locked { get; set; }

    /// <summary>
    /// 邀请 Token
    /// </summary>
    [MaxLength(Limits.InviteTokenLength)]
    public string InviteToken { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// 邀请 Code
    /// </summary>
    [NotMapped]
    public string InviteCode => $"{Name}:{Id}:{InviteToken}";

    [NotMapped]
    public string? AvatarUrl => AvatarHash is null ? null : $"/assets/{AvatarHash}/avatar";

    public void UpdateInviteToken() => InviteToken = Guid.NewGuid().ToString("N");

    internal void UpdateInfo(TeamUpdateModel model)
    {
        Name = string.IsNullOrEmpty(model.Name) ? Name : model.Name;
        Bio = model.Bio;
    }

    internal void UpdateInfo(AdminTeamModel model)
    {
        Name = string.IsNullOrEmpty(model.Name) ? Name : model.Name;
        Bio = string.IsNullOrEmpty(model.Bio) ? Bio : model.Bio;
        Locked = model.Locked ?? Locked;
    }

    #region Db Relationship

    /// <summary>
    /// 队长用户ID
    /// </summary>
    public Guid CaptainId { get; set; }

    /// <summary>
    /// 队长
    /// </summary>
    public UserInfo? Captain { get; set; }

    /// <summary>
    /// 比赛参与对象
    /// </summary>
    public List<Participation> Participations { get; set; } = [];

    /// <summary>
    /// 比赛对象
    /// </summary>
    public HashSet<Game>? Games { get; set; }

    /// <summary>
    /// 队员
    /// </summary>
    public HashSet<UserInfo> Members { get; set; } = [];

    #endregion Db Relationship
}
