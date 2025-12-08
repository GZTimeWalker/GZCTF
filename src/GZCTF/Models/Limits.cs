namespace GZCTF.Models;

public static class Limits
{
    /// <summary>
    /// Max length of flag
    /// </summary>
    public const int MaxFlagLength = 127;

    /// <summary>
    /// Max length of flag template, reserved for replacement
    /// </summary>
    public const int MaxFlagTemplateLength = 120;

    /// <summary>
    /// Max length of team name
    /// </summary>
    public const int MaxTeamNameLength = 20;

    /// <summary>
    /// Max length of team bio (for frontend display)
    /// </summary>
    public const int MaxTeamBioLength = 72;

    /// <summary>
    /// Max length of user data (bio and real name)
    /// </summary>
    public const int MaxUserDataLength = 128;

    /// <summary>
    /// Max length of student number
    /// </summary>
    public const int MaxStdNumberLength = 64;

    /// <summary>
    /// Min length of username
    /// </summary>
    public const int MinUserNameLength = 3;

    /// <summary>
    /// Max length of username
    /// </summary>
    public const int MaxUserNameLength = 15;

    /// <summary>
    /// Min length of password
    /// </summary>
    public const int MinPasswordLength = 6;

    /// <summary>
    /// Length of file hash
    /// </summary>
    public const int FileHashLength = 64;

    /// <summary>
    /// Length of game public/private key
    /// </summary>
    public const int GameKeyLength = 63;

    /// <summary>
    /// Length of invite token
    /// </summary>
    public const int InviteTokenLength = 32;

    /// <summary>
    /// Max length of IP address
    /// </summary>
    public const int MaxIPLength = 40;

    /// <summary>
    /// Max length of post title
    /// </summary>
    public const int MaxPostTitleLength = 50;

    /// <summary>
    /// Max length of log level
    /// </summary>
    public const int MaxLogLevelLength = 15;

    /// <summary>
    /// Max length of logger source
    /// </summary>
    public const int MaxLoggerLength = 250;

    /// <summary>
    /// Max length of log status
    /// </summary>
    public const int MaxLogStatusLength = 10;

    /// <summary>
    /// Max length of short identifier (e.g. OAuth provider key, Division name)
    /// </summary>
    public const int MaxShortIdLength = 31;

    /// <summary>
    /// Max length of display name
    /// </summary>
    public const int MaxDisplayNameLength = 80;

    /// <summary>
    /// Max length of user metadata key
    /// </summary>
    public const int MaxUserMetadataKeyLength = 40;

    /// <summary>
    /// Max length of user metadata placeholder
    /// </summary>
    public const int MaxUserMetadataPlaceholderLength = 200;

    /// <summary>
    /// Max length of regex pattern
    /// </summary>
    public const int MaxRegexPatternLength = 400;
}