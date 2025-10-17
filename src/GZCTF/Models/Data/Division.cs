using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using GZCTF.Models.Request.Edit;
using MemoryPack;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

[MemoryPackable]
[Index(nameof(GameId))]
public partial class Division
{
    [Key]
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// The game ID this division belongs to.
    /// </summary>
    [Required]
    [JsonIgnore]
    public int GameId { get; set; }

    /// <summary>
    /// The name of the division.
    /// </summary>
    [Required]
    [MaxLength(Limits.MaxDivisionNameLength)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Invitation code for joining the division.
    /// </summary>
    [MaxLength(Limits.InviteTokenLength)]
    public string? InviteCode { get; set; }

    /// <summary>
    /// Permissions associated with the division.
    /// </summary>
    public GamePermission DefaultPermissions { get; set; } = GamePermission.All;

    #region Db Relationship

    /// <summary>
    /// Challenge configs for this division.
    /// </summary>
    public HashSet<DivisionChallengeConfig> ChallengeConfigs { get; set; } = [];

    /// <summary>
    /// The game this division belongs to.
    /// </summary>
    [JsonIgnore]
    [MemoryPackIgnore]
    public Game? Game { get; set; }

    #endregion

    internal void UpdateChallengeConfigs(HashSet<DivisionChallengeConfigModel>? configs)
    {
        if (configs is null)
            return;

        var newConfigs = configs.ToDictionary(c => c.ChallengeId);
        ChallengeConfigs.RemoveWhere(c => !newConfigs.ContainsKey(c.ChallengeId));

        foreach (var config in newConfigs.Values)
        {
            var existingConfig = ChallengeConfigs.FirstOrDefault(c => c.ChallengeId == config.ChallengeId);
            if (existingConfig is not null)
            {
                existingConfig.Permissions = config.Permissions;
            }
            else
            {
                ChallengeConfigs.Add(new DivisionChallengeConfig
                {
                    ChallengeId = config.ChallengeId,
                    Permissions = config.Permissions,
                    DivisionId = Id
                });
            }
        }
    }

    internal void Update(DivisionEditModel model)
    {
        Name = model.Name ?? Name;
        InviteCode = model.InviteCode ?? InviteCode;
        DefaultPermissions = model.DefaultPermissions ?? DefaultPermissions;

        UpdateChallengeConfigs(model.ChallengeConfigs);
    }
}

[MemoryPackable]
[PrimaryKey(nameof(ChallengeId), nameof(DivisionId))]
public partial class DivisionChallengeConfig
{
    [Required]
    [JsonIgnore]
    public int DivisionId { get; set; }

    [Required]
    public int ChallengeId { get; set; }

    /// <summary>
    /// Challenge Specific Permissions
    /// </summary>
    public GamePermission Permissions { get; set; } = GamePermission.All;

    #region Db Relationship

    [JsonIgnore]
    [MemoryPackIgnore]
    public Division? Division { get; set; }

    [JsonIgnore]
    [MemoryPackIgnore]
    public GameChallenge? Challenge { get; set; }

    #endregion
}
