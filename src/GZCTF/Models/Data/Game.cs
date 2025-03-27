using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using GZCTF.Models.Request.Edit;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.Encoders;

namespace GZCTF.Models.Data;

public class Game
{
    [Key]
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// Game title
    /// </summary>
    [Required]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Token signature public key
    /// </summary>
    [Required]
    [MaxLength(Limits.GameKeyLength)]
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// Token signature private key
    /// </summary>
    [Required]
    [MaxLength(Limits.GameKeyLength)]
    public string PrivateKey { get; set; } = string.Empty;

    /// <summary>
    /// Whether to hide
    /// </summary>
    [Required]
    public bool Hidden { get; set; }

    /// <summary>
    /// Poster hash
    /// </summary>
    [MaxLength(Limits.FileHashLength)]
    public string? PosterHash { get; set; }

    /// <summary>
    /// Game description
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Detailed introduction of the game
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Teams can join without review
    /// </summary>
    public bool AcceptWithoutReview { get; set; }

    /// <summary>
    /// Whether writeup is required
    /// </summary>
    public bool WriteupRequired { get; set; }

    /// <summary>
    /// Game invitation code
    /// </summary>
    [MaxLength(Limits.InviteTokenLength)]
    public string? InviteCode { get; set; }

    /// <summary>
    /// List of divisions for the game
    /// </summary>
    public HashSet<string>? Divisions { get; set; }

    /// <summary>
    /// Limit on the number of team members, 0 means no limit
    /// </summary>
    public int TeamMemberCountLimit { get; set; }

    /// <summary>
    /// Limit on the number of containers a team can have simultaneously
    /// </summary>
    public int ContainerCountLimit { get; set; } = 3;

    /// <summary>
    /// Start time
    /// </summary>
    [Required]
    [JsonPropertyName("start")]
    public DateTimeOffset StartTimeUtc { get; set; } = DateTimeOffset.FromUnixTimeSeconds(0);

    /// <summary>
    /// End time
    /// </summary>
    [Required]
    [JsonPropertyName("end")]
    public DateTimeOffset EndTimeUtc { get; set; } = DateTimeOffset.FromUnixTimeSeconds(0);

    /// <summary>
    /// Writeup submission deadline
    /// </summary>
    [Required]
    public DateTimeOffset WriteupDeadline { get; set; } = DateTimeOffset.FromUnixTimeSeconds(0);

    /// <summary>
    /// Additional notes for writeup
    /// </summary>
    [Required]
    public string WriteupNote { get; set; } = string.Empty;

    [JsonIgnore]
    [Column(nameof(BloodBonus))]
    public long BloodBonusValue { get; set; } = BloodBonus.DefaultValue;

    /// <summary>
    /// Blood bonus
    /// </summary>
    [NotMapped]
    [Required]
    public BloodBonus BloodBonus
    {
        get => BloodBonus.FromValue(BloodBonusValue);
        set => BloodBonusValue = value.Val;
    }

    /// <summary>
    /// Whether the game is active
    /// </summary>
    [NotMapped]
    [JsonIgnore]
    public bool IsActive => StartTimeUtc <= DateTimeOffset.Now && DateTimeOffset.Now <= EndTimeUtc;

    /// <summary>
    /// Poster URL
    /// </summary>
    [NotMapped]
    public string? PosterUrl => GetPosterUrl(PosterHash);

    /// <summary>
    /// Team hash salt
    /// </summary>
    [NotMapped]
    public string TeamHashSalt => $"GZCTF@{PrivateKey}@PK".ToSHA256String();

    internal static string? GetPosterUrl(string? hash) => hash is null ? null : $"/assets/{hash}/poster";

    internal void GenerateKeyPair(byte[]? xorKey)
    {
        SecureRandom sr = new();
        Ed25519KeyPairGenerator kpg = new();
        kpg.Init(new Ed25519KeyGenerationParameters(sr));
        var kp = kpg.GenerateKeyPair();
        var privateKey = (Ed25519PrivateKeyParameters)kp.Private;
        var publicKey = (Ed25519PublicKeyParameters)kp.Public;

        PrivateKey =
            Base64.ToBase64String(xorKey is null
                ? privateKey.GetEncoded()
                : Codec.Xor(privateKey.GetEncoded(), xorKey));

        PublicKey = Base64.ToBase64String(publicKey.GetEncoded());
    }

    internal string Sign(string str, byte[]? xorKey)
    {
        Ed25519PrivateKeyParameters privateKey;
        if (xorKey is null)
            privateKey = new(Codec.Base64.DecodeToBytes(PrivateKey), 0);
        else
            privateKey = new(Codec.Xor(Codec.Base64.DecodeToBytes(PrivateKey), xorKey), 0);

        return DigitalSignature.GenerateSignature(str, privateKey, SignAlgorithm.Ed25519);
    }

    internal bool IsValidDivision(string? division)
    {
        if (Divisions is not { Count: > 0 })
            return division is null;

        return !string.IsNullOrWhiteSpace(division) && Divisions.Contains(division);
    }

    internal Game Update(GameInfoModel model)
    {
        Title = model.Title;
        Content = model.Content;
        Summary = model.Summary;
        Hidden = model.Hidden;
        PracticeMode = model.PracticeMode;
        AcceptWithoutReview = model.AcceptWithoutReview;
        InviteCode = model.InviteCode;
        Divisions = model.Divisions?.ToHashSet() ?? Divisions;
        EndTimeUtc = model.EndTimeUtc;
        StartTimeUtc = model.StartTimeUtc;
        TeamMemberCountLimit = model.TeamMemberCountLimit;
        ContainerCountLimit = model.ContainerCountLimit;
        WriteupNote = model.WriteupNote;
        WriteupRequired = model.WriteupRequired;
        WriteupDeadline = model.WriteupDeadline;
        BloodBonus = BloodBonus.FromValue(model.BloodBonusValue);

        return this;
    }

    #region Db Relationship

    /// <summary>
    /// Game events
    /// </summary>
    [JsonIgnore]
    public List<GameEvent> GameEvents { get; set; } = [];

    /// <summary>
    /// Game notices
    /// </summary>
    [JsonIgnore]
    public List<GameNotice> GameNotices { get; set; } = [];

    /// <summary>
    /// Game challenges
    /// </summary>
    [JsonIgnore]
    public List<GameChallenge> Challenges { get; set; } = [];

    /// <summary>
    /// Game submissions
    /// </summary>
    [JsonIgnore]
    public List<Submission> Submissions { get; set; } = [];

    /// <summary>
    /// Game participations
    /// </summary>
    [JsonIgnore]
    public HashSet<Participation> Participations { get; set; } = [];

    /// <summary>
    /// Game teams
    /// </summary>
    [JsonIgnore]
    public ICollection<Team>? Teams { get; set; }

    /// <summary>
    /// Whether the game is in practice mode (most operations can still be performed after the game ends)
    /// </summary>
    public bool PracticeMode { get; set; } = true;

    #endregion Db Relationship
}
