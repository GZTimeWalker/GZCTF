﻿using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

[Index(nameof(FlagId))]
[PrimaryKey(nameof(ParticipationId), nameof(ChallengeId))]
public class GameInstance : Instance
{
    /// <summary>
    /// Get instance attachment
    /// </summary>
    internal Attachment? Attachment => Challenge.Type == ChallengeType.DynamicAttachment
        ? FlagContext?.Attachment
        : Challenge.Attachment;

    /// <summary>
    /// Get instance attachment URL
    /// </summary>
    internal string? AttachmentUrl => Challenge.Type == ChallengeType.DynamicAttachment
        ? FlagContext?.Attachment?.UrlWithName(Challenge.FileName)
        : Challenge.Attachment?.UrlWithName();

    #region Db Relationship

    [Required]
    public int ChallengeId { get; set; }

    /// <summary>
    /// Challenge object
    /// </summary>
    public GameChallenge Challenge { get; set; } = default!;

    [Required]
    public int ParticipationId { get; set; }

    /// <summary>
    /// Participation team object
    /// </summary>
    public Participation Participation { get; set; } = default!;

    #endregion Db Relationship
}
