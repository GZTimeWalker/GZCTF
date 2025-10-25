using System.Text.Json.Serialization;
using Microsoft.Extensions.Localization;

namespace GZCTF.Utils;

/// <summary>
/// User role enumeration
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<Role>))]
public enum Role : byte
{
    /// <summary>
    /// Banned user role
    /// </summary>
    Banned = 0,

    /// <summary>
    /// Regular user role
    /// </summary>
    User = 1,

    /// <summary>
    /// Monitor role, can query submission logs
    /// </summary>
    Monitor = 2,

    /// <summary>
    /// Admin role, can view system logs
    /// </summary>
    Admin = 3
}

/// <summary>
/// Login response status
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<RegisterStatus>))]
public enum RegisterStatus : byte
{
    /// <summary>
    /// Registered successfully and logged in
    /// </summary>
    LoggedIn = 0,

    /// <summary>
    /// Waiting for admin confirmation
    /// </summary>
    AdminConfirmationRequired = 1,

    /// <summary>
    /// Waiting for email confirmation
    /// </summary>
    EmailConfirmationRequired = 2
}

/// <summary>
/// Task execution status
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<TaskStatus>))]
public enum TaskStatus : sbyte
{
    /// <summary>
    /// System is unhealthy
    /// </summary>
    Unhealthy = -3,

    /// <summary>
    /// System is in a degraded state
    /// </summary>
    Degraded = -2,

    /// <summary>
    /// Task is in progress
    /// </summary>
    Pending = -1,

    /// <summary>
    /// Task completed successfully
    /// </summary>
    Success = 0,

    /// <summary>
    /// Task execution failed
    /// </summary>
    Failed = 1,

    /// <summary>
    /// Task encountered a duplicate error
    /// </summary>
    Duplicate = 2,

    /// <summary>
    /// Task processing was denied
    /// </summary>
    Denied = 3,

    /// <summary>
    /// Task request not found
    /// </summary>
    NotFound = 4,

    /// <summary>
    /// Task thread is about to exit
    /// </summary>
    Exit = 5
}

[JsonConverter(typeof(JsonStringEnumConverter<FileType>))]
public enum FileType : byte
{
    /// <summary>
    /// No attachment
    /// Normally, Attachment will not be this value, if there is no attachment, Attachment will be null
    /// </summary>
    None = 0,

    /// <summary>
    /// Local file
    /// </summary>
    Local = 1,

    /// <summary>
    /// Remote file
    /// </summary>
    Remote = 2
}

/// <summary>
/// Container status
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ContainerStatus>))]
public enum ContainerStatus : byte
{
    /// <summary>
    /// Starting
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Running
    /// </summary>
    Running = 1,

    /// <summary>
    /// Destroyed
    /// </summary>
    Destroyed = 2
}

/// <summary>
/// Game announcement type
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<NoticeType>))]
public enum NoticeType : byte
{
    /// <summary>
    /// Regular announcement
    /// </summary>
    Normal = 0,

    /// <summary>
    /// First blood announcement
    /// </summary>
    FirstBlood = 1,

    /// <summary>
    /// Second blood announcement
    /// </summary>
    SecondBlood = 2,

    /// <summary>
    /// Third blood announcement
    /// </summary>
    ThirdBlood = 3,

    /// <summary>
    /// New hint released
    /// </summary>
    NewHint = 4,

    /// <summary>
    /// New challenge released
    /// </summary>
    NewChallenge = 5
}

public static class SubmissionTypeExtensions
{
    public static string ToBloodString(this SubmissionType type, IStringLocalizer<Program> localizer) =>
        type switch
        {
            SubmissionType.FirstBlood => localizer[nameof(Resources.Program.Submission_FirstBlood)],
            SubmissionType.SecondBlood => localizer[nameof(Resources.Program.Submission_SecondBlood)],
            SubmissionType.ThirdBlood => localizer[nameof(Resources.Program.Submission_ThirdBlood)],
            _ => throw new ArgumentException(type.ToString(), nameof(type))
        };
}

/// <summary>
/// Game event type
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<EventType>))]
public enum EventType : byte
{
    /// <summary>
    /// Regular information
    /// </summary>
    Normal = 0,

    /// <summary>
    /// Container start information
    /// </summary>
    ContainerStart = 1,

    /// <summary>
    /// Container destroy information
    /// </summary>
    ContainerDestroy = 2,

    /// <summary>
    /// Flag submission information
    /// </summary>
    FlagSubmit = 3,

    /// <summary>
    /// Cheating information
    /// </summary>
    CheatDetected = 4
}

/// <summary>
/// Submission type
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<SubmissionType>))]
public enum SubmissionType : byte
{
    /// <summary>
    /// Not solved
    /// </summary>
    Unaccepted = 0,

    /// <summary>
    /// First blood
    /// </summary>
    FirstBlood = 1,

    /// <summary>
    /// Second blood
    /// </summary>
    SecondBlood = 2,

    /// <summary>
    /// Third blood
    /// </summary>
    ThirdBlood = 3,

    /// <summary>
    /// Solved
    /// </summary>
    Normal = 4
}

[JsonConverter(typeof(JsonStringEnumConverter<ParticipationStatus>))]
public enum ParticipationStatus : byte
{
    /// <summary>
    /// Registered
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Accepted
    /// </summary>
    Accepted = 1,

    /// <summary>
    /// Rejected
    /// </summary>
    Rejected = 2,

    /// <summary>
    /// Suspended
    /// </summary>
    Suspended = 3,

    /// <summary>
    /// Not submitted
    /// </summary>
    Unsubmitted = 4
}

[JsonConverter(typeof(JsonStringEnumConverter<ChallengeType>))]
public enum ChallengeType : byte
{
    /// <summary>
    /// Static challenge
    /// All teams use the same attachment and flag
    /// </summary>
    StaticAttachment = 0b00,

    /// <summary>
    /// Static container challenge
    /// All teams use the same docker and flag
    /// </summary>
    StaticContainer = 0b01,

    /// <summary>
    /// Dynamic attachment challenge
    /// Randomly distribute attachments, implement flag specificity with attachments
    /// </summary>
    DynamicAttachment = 0b10,

    /// <summary>
    /// Dynamic container challenge
    /// Randomly distribute containers, dynamic flag passed in via environment variables
    /// </summary>
    DynamicContainer = 0b11
}

public static class ChallengeTypeExtensions
{
    /// <summary>
    /// Is it a static challenge
    /// </summary>
    public static bool IsStatic(this ChallengeType type) => ((byte)type & 0b10) == 0;

    /// <summary>
    /// Is it a dynamic challenge
    /// </summary>
    public static bool IsDynamic(this ChallengeType type) => ((byte)type & 0b10) != 0;

    /// <summary>
    /// Is it an attachment challenge
    /// </summary>
    public static bool IsAttachment(this ChallengeType type) => ((byte)type & 0b01) == 0;

    /// <summary>
    /// Is it a container challenge
    /// </summary>
    public static bool IsContainer(this ChallengeType type) => ((byte)type & 0b01) != 0;
}

/// <summary>
/// Challenge category
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ChallengeCategory>))]
public enum ChallengeCategory : byte
{
    Misc = 0,
    Crypto = 1,
    Pwn = 2,
    Web = 3,
    Reverse = 4,
    Blockchain = 5,
    Forensics = 6,
    Hardware = 7,
    Mobile = 8,
    PPC = 9,

    // ReSharper disable once InconsistentNaming
    AI = 10,
    Pentest = 11,

    // ReSharper disable once InconsistentNaming
    OSINT = 12
}

/// <summary>
/// Challenge difficulty
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<Difficulty>))]
public enum Difficulty : byte
{
    Baby = 0,
    Trivial = 1,
    Easy = 2,
    Normal = 3,
    Medium = 4,
    Hard = 5,
    Expert = 6,
    Insane = 7
}

/// <summary>
/// Game participant permission
/// </summary>
[Flags]
[JsonConverter(typeof(JsonNumberEnumConverter<GamePermission>))]
public enum GamePermission
{
    /// <summary>
    /// Join the game
    /// </summary>
    /// <remarks>
    /// Division-level permission only. Controls whether users can join the game through this division.
    /// Without this permission, the division cannot accept new participants.
    /// </remarks>
    JoinGame = 1 << 0,

    /// <summary>
    /// Can be ranked on the overall scoreboard
    /// </summary>
    /// <remarks>
    /// Division-level permission only. Determines if teams in this division appear on the overall scoreboard rankings.
    /// Teams without this permission will only have division-specific rankings.
    /// Use case: Unofficial participants or guest teams that should not compete for overall prizes.
    /// </remarks>
    RankOverall = 1 << 1,

    /// <summary>
    /// Require review before acceptance
    /// </summary>
    /// <remarks>
    /// Division-level permission only. When enabled, participations require manual admin approval (Pending status).
    /// When disabled, participations are automatically accepted without review (Accepted status).
    /// Overrides game-level AcceptWithoutReview setting for this specific division.
    /// Use case: Require review for external registrations while auto-accepting internal teams.
    /// Note: Permission name is positive (require review) so that All permission defaults to the secure behavior.
    /// </remarks>
    RequireReview = 1 << 2,

    /// <summary>
    /// Can view challenge
    /// </summary>
    /// <remarks>
    /// Challenge-specific permission. Controls access to challenge details, descriptions, and attachments.
    /// Without this permission, challenges will be hidden from the team.
    /// </remarks>
    ViewChallenge = 1 << 8,

    /// <summary>
    /// Can submit flags
    /// </summary>
    /// <remarks>
    /// Challenge-specific permission. Allows teams to submit flag answers for challenges.
    /// Can be combined with ViewChallenge but without GetScore for practice/demo scenarios.
    /// </remarks>
    SubmitFlags = 1 << 9,

    /// <summary>
    /// Can be awarded points
    /// </summary>
    /// <remarks>
    /// Challenge-specific permission. Determines if successful flag submissions award points to the team.
    /// Teams without this permission can submit flags but won't receive any score.
    /// Use case: Observer teams or late joiners who participate without competing.
    /// </remarks>
    GetScore = 1 << 10,

    /// <summary>
    /// Can earn blood bonuses
    /// </summary>
    /// <remarks>
    /// Challenge-specific permission. Allows teams to receive first/second/third blood bonus points.
    /// Requires GetScore permission to be effective. Blood bonuses are typically 30%, 20%, and 10% extra points.
    /// Use case: Restrict blood bonuses to specific divisions while allowing others to score normally.
    /// </remarks>
    GetBlood = 1 << 11,

    /// <summary>
    /// Affects dynamic scoring calculation
    /// </summary>
    /// <remarks>
    /// Challenge-specific permission. Controls whether this team's submissions count toward challenge accept count,
    /// which influences dynamic score calculation for all teams.
    /// Independent of GetScore - teams can score without affecting others' challenge values.
    /// Use case: Allow external participants to score without affecting internal competition's dynamic scoring.
    /// </remarks>
    AffectDynamicScore = 1 << 12,

    /// <summary>
    /// All permissions, including future permissions
    /// </summary>
    /// <remarks>
    /// Special value representing all current and future permissions.
    /// Use int.MaxValue to ensure compatibility with newly added permission flags.
    /// </remarks>
    All = int.MaxValue
}

/// <summary>
/// Judgement result
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<AnswerResult>))]
public enum AnswerResult : sbyte
{
    /// <summary>
    /// Flag submitted successfully
    /// </summary>
    FlagSubmitted = 0,

    /// <summary>
    /// Answer is correct
    /// </summary>
    Accepted = 1,

    /// <summary>
    /// Answer is wrong
    /// </summary>
    WrongAnswer = 2,

    /// <summary>
    /// Cheating detected
    /// </summary>
    CheatDetected = 3,

    /// <summary>
    /// Submitted challenge instance not found
    /// </summary>
    NotFound = -1
}

public static class AnswerResultExtensions
{
    public static string ToShortString(this AnswerResult result, IStringLocalizer<Program> localizer) =>
        result switch
        {
            AnswerResult.FlagSubmitted => localizer[nameof(Resources.Program.Submission_FlagSubmitted)],
            AnswerResult.Accepted => localizer[nameof(Resources.Program.Submission_Accepted)],
            AnswerResult.WrongAnswer => localizer[nameof(Resources.Program.Submission_WrongAnswer)],
            AnswerResult.CheatDetected => localizer[nameof(Resources.Program.Submission_CheatDetected)],
            AnswerResult.NotFound => localizer[nameof(Resources.Program.Submission_UnknownInstance)],
            _ => "??"
        };
}

/// <summary>
/// System error codes, starting from 10000
/// </summary>
public static class ErrorCodes
{
    /// <summary>
    /// Game not started
    /// </summary>
    public const int GameNotStarted = 10001;

    /// <summary>
    /// Game ended
    /// </summary>
    public const int GameEnded = 10002;
}
