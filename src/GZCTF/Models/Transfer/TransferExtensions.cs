namespace GZCTF.Models.Transfer;

/// <summary>
/// Extension methods for converting between domain models and transfer models
/// </summary>
public static class TransferExtensions
{
    /// <summary>
    /// Convert Game to TransferGame
    /// </summary>
    public static TransferGame ToTransfer(this Game game)
    {
        var transfer = new TransferGame
        {
            Title = game.Title,
            Summary = game.Summary,
            Content = game.Content,
            Hidden = game.Hidden,
            PracticeMode = game.PracticeMode,
            AcceptWithoutReview = game.AcceptWithoutReview,
            InviteCode = game.InviteCode,
            TeamMemberCountLimit = game.TeamMemberCountLimit,
            ContainerCountLimit = game.ContainerCountLimit,
            StartTime = game.StartTimeUtc,
            EndTime = game.EndTimeUtc
        };

        // Writeup configuration
        if (game.WriteupRequired || game.WriteupDeadline != DateTimeOffset.FromUnixTimeSeconds(0))
        {
            transfer.Writeup = new WriteupSection
            {
                Required = game.WriteupRequired,
                Deadline = game.WriteupDeadline,
                Note = string.IsNullOrWhiteSpace(game.WriteupNote) ? null : game.WriteupNote
            };
        }

        // Blood bonus configuration
        var bloodBonus = game.BloodBonus;
        if (!bloodBonus.NoBonus)
        {
            transfer.BloodBonus = new BloodBonusSection
            {
                First = (int)bloodBonus.FirstBlood,
                Second = (int)bloodBonus.SecondBlood,
                Third = (int)bloodBonus.ThirdBlood
            };
        }

        // Poster hash
        if (!string.IsNullOrWhiteSpace(game.PosterHash))
            transfer.PosterHash = game.PosterHash;

        return transfer;
    }

    /// <summary>
    /// Convert GameChallenge to TransferChallenge
    /// </summary>
    public static TransferChallenge ToTransfer(this GameChallenge challenge)
    {
        var transfer = new TransferChallenge
        {
            Id = challenge.Id,
            Title = challenge.Title,
            Content = challenge.Content,
            Category = challenge.Category.ToString(),
            Type = challenge.Type.ToString(),
            Enabled = challenge.IsEnabled,
            Scoring = new ScoringSection
            {
                Original = challenge.OriginalScore,
                MinRate = challenge.MinScoreRate,
                Difficulty = challenge.Difficulty
            },
            Limits = new LimitsSection
            {
                Submission = challenge.SubmissionLimit,
                Deadline = challenge.DeadlineUtc
            },
            Flags = new FlagsSection
            {
                Template = challenge.FlagTemplate,
                DisableBloodBonus = challenge.DisableBloodBonus,
                EnableTrafficCapture = challenge.EnableTrafficCapture,
                Static = challenge.Flags
                    .Select(f => new StaticFlagSection
                    {
                        Value = f.Flag,
                        Attachment = f.Attachment?.ToTransfer()
                    })
                    .ToList()
            }
        };

        // Hints
        if (challenge.Hints?.Count > 0)
        {
            transfer.Hints = challenge.Hints;
        }

        // Attachment
        if (challenge.Attachment != null)
        {
            transfer.Attachment = challenge.Attachment.ToTransfer();
        }

        // Container configuration (only for container challenges)
        if (challenge.Type.IsContainer())
        {
            transfer.Container = new ContainerSection
            {
                Image = challenge.ContainerImage ?? string.Empty,
                MemoryLimit = challenge.MemoryLimit ?? 64,
                CpuCount = challenge.CPUCount ?? 1,
                StorageLimit = challenge.StorageLimit ?? 256,
                ExposePort = challenge.ContainerExposePort ?? 80,
                FileName = challenge.FileName
            };
        }

        return transfer;
    }

    /// <summary>
    /// Convert Attachment to TransferChallenge.AttachmentSection
    /// </summary>
    public static AttachmentSection? ToTransfer(this Attachment attachment)
    {
        if (attachment.Type == FileType.None)
            return null;

        var transfer = new AttachmentSection
        {
            Type = attachment.Type.ToString()
        };

        if (attachment.Type == FileType.Local && attachment.LocalFile != null)
        {
            transfer.Hash = attachment.LocalFile.Hash;
            transfer.FileName = attachment.LocalFile.Name;
            transfer.FileSize = attachment.LocalFile.FileSize;
        }
        else if (attachment.Type == FileType.Remote)
        {
            transfer.RemoteUrl = attachment.RemoteUrl;
        }

        return transfer;
    }

    /// <summary>
    /// Convert Division to TransferDivision
    /// </summary>
    public static TransferDivision ToTransfer(this Division division) =>
        new()
        {
            Name = division.Name,
            InviteCode = division.InviteCode,
            DefaultPermissions = PermissionsToStrings(division.DefaultPermissions),
            ChallengeConfig = division.ChallengeConfigs
                .Select(cc => new ChallengeConfigSection
                {
                    ChallengeId = cc.ChallengeId,
                    Permissions = PermissionsToStrings(cc.Permissions)
                })
                .ToList()
        };

    /// <summary>
    /// Convert TransferGame to Game
    /// </summary>
    public static Game ToGame(this TransferGame transfer, byte[]? xorKey = null)
    {
        var game = new Game
        {
            Title = transfer.Title,
            Summary = transfer.Summary,
            Content = transfer.Content,
            Hidden = transfer.Hidden,
            PracticeMode = transfer.PracticeMode,
            AcceptWithoutReview = transfer.AcceptWithoutReview,
            InviteCode = transfer.InviteCode,
            TeamMemberCountLimit = transfer.TeamMemberCountLimit,
            ContainerCountLimit = transfer.ContainerCountLimit,
            StartTimeUtc = transfer.StartTime,
            EndTimeUtc = transfer.EndTime,
            PosterHash = transfer.PosterHash
        };

        // Generate new key pair (always generate new keys on import)
        game.GenerateKeyPair(xorKey);

        // Writeup configuration
        if (transfer.Writeup != null)
        {
            game.WriteupRequired = transfer.Writeup.Required;
            game.WriteupDeadline = transfer.Writeup.Deadline;
            game.WriteupNote = transfer.Writeup.Note ?? string.Empty;
        }
        else
        {
            game.WriteupRequired = false;
            game.WriteupDeadline = DateTimeOffset.FromUnixTimeSeconds(0);
            game.WriteupNote = string.Empty;
        }

        // Blood bonus configuration
        if (transfer.BloodBonus != null)
        {
            var bloodBonusValue = ((long)transfer.BloodBonus.First << 20)
                                  + ((long)transfer.BloodBonus.Second << 10)
                                  + transfer.BloodBonus.Third;
            game.BloodBonusValue = bloodBonusValue;
        }
        else
        {
            game.BloodBonusValue = 0; // No bonus
        }

        return game;
    }

    /// <summary>
    /// Convert TransferChallenge to GameChallenge
    /// </summary>
    /// <remarks>
    /// Note: This does NOT set the GameId or create the attachment/flag entities.
    /// Those should be handled separately during the import process.
    /// </remarks>
    public static GameChallenge ToChallenge(this TransferChallenge transfer)
    {
        var challenge = new GameChallenge
        {
            Title = transfer.Title,
            Content = transfer.Content,
            Category = Enum.Parse<ChallengeCategory>(transfer.Category),
            Type = Enum.Parse<ChallengeType>(transfer.Type),
            IsEnabled = transfer.Enabled,
            OriginalScore = transfer.Scoring.Original,
            MinScoreRate = transfer.Scoring.MinRate,
            Difficulty = transfer.Scoring.Difficulty,
            SubmissionLimit = transfer.Limits.Submission,
            DeadlineUtc = transfer.Limits.Deadline,
            FlagTemplate = transfer.Flags.Template,
            DisableBloodBonus = transfer.Flags.DisableBloodBonus,
            EnableTrafficCapture = transfer.Flags.EnableTrafficCapture,
            Hints = transfer.Hints
        };

        // Container configuration
        if (transfer.Container == null)
            return challenge;

        challenge.ContainerImage = transfer.Container.Image;
        challenge.MemoryLimit = transfer.Container.MemoryLimit;
        challenge.CPUCount = transfer.Container.CpuCount;
        challenge.StorageLimit = transfer.Container.StorageLimit;
        challenge.ContainerExposePort = transfer.Container.ExposePort;
        challenge.FileName = transfer.Container.FileName;

        return challenge;
    }

    /// <summary>
    /// Create FlagContext from static flag transfer
    /// </summary>
    public static FlagContext ToFlagContext(this StaticFlagSection transfer) =>
        new()
        {
            Flag = transfer.Value,
            IsOccupied = false
        };

    /// <summary>
    /// Convert TransferDivision to Division
    /// </summary>
    public static Division ToDivision(this TransferDivision transfer) =>
        new()
        {
            Name = transfer.Name,
            InviteCode = transfer.InviteCode,
            DefaultPermissions = StringsToPermissions(transfer.DefaultPermissions)
        };

    /// <summary>
    /// Create DivisionChallengeConfig from transfer
    /// </summary>
    public static DivisionChallengeConfig ToConfig(
        this ChallengeConfigSection transfer,
        int divisionId,
        Dictionary<int, int> challengeIdMap)
    {
        // Map original challenge ID to new challenge ID
        if (!challengeIdMap.TryGetValue(transfer.ChallengeId, out var newChallengeId))
        {
            throw new InvalidOperationException(
                $"Challenge ID {transfer.ChallengeId} not found in mapping");
        }

        return new DivisionChallengeConfig
        {
            DivisionId = divisionId,
            ChallengeId = newChallengeId,
            Permissions = StringsToPermissions(transfer.Permissions)
        };
    }

    /// <summary>
    /// Convert GamePermission enum to string array
    /// </summary>
    public static List<string> PermissionsToStrings(GamePermission permissions)
    {
        // Special cases
        if (permissions == GamePermission.All)
            return ["All"];

        if (permissions == 0)
            return ["None"];

        // Extract individual flags
        var flags = new List<string>();
        foreach (GamePermission value in Enum.GetValues<GamePermission>())
        {
            // Skip special values
            if (value == 0 || value == GamePermission.All)
                continue;

            if (permissions.HasFlag(value))
            {
                flags.Add(value.ToString());
            }
        }

        return flags;
    }

    /// <summary>
    /// Convert string array to GamePermission enum
    /// </summary>
    public static GamePermission StringsToPermissions(List<string>? permissionStrings)
    {
        if (permissionStrings == null || permissionStrings.Count == 0)
            return 0;

        // Special cases
        if (permissionStrings.Contains("All"))
            return GamePermission.All;

        if (permissionStrings.Contains("None"))
            return 0;

        // Combine individual flags
        GamePermission result = 0;
        foreach (var str in permissionStrings)
        {
            if (Enum.TryParse<GamePermission>(str, out var value))
            {
                result |= value;
            }
        }

        return result;
    }
}
