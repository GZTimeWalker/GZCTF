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
    /// Team name
    /// </summary>
    [Required]
    [MaxLength(Limits.MaxTeamNameLength)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Team bio
    /// </summary>
    [MaxLength(Limits.MaxTeamBioLength)]
    public string? Bio { get; set; } = string.Empty;

    /// <summary>
    /// Avatar hash
    /// </summary>
    [MaxLength(Limits.FileHashLength)]
    public string? AvatarHash { get; set; }

    /// <summary>
    /// Is the team locked
    /// </summary>
    public bool Locked { get; set; }

    /// <summary>
    /// Invite token
    /// </summary>
    [MaxLength(Limits.InviteTokenLength)]
    public string InviteToken { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Invitation code
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
    /// Captain user ID
    /// </summary>
    public Guid CaptainId { get; set; }

    /// <summary>
    /// Captain
    /// </summary>
    public UserInfo? Captain { get; set; }

    /// <summary>
    /// Participation objects
    /// </summary>
    public List<Participation> Participations { get; set; } = [];

    /// <summary>
    /// Games
    /// </summary>
    public HashSet<Game>? Games { get; set; }

    /// <summary>
    /// Members
    /// </summary>
    public HashSet<UserInfo> Members { get; set; } = [];

    #endregion Db Relationship
}
