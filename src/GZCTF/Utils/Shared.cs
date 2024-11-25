using System.ComponentModel.DataAnnotations;
using System.Threading.Channels;

namespace GZCTF.Utils;

public static class ChannelService
{
    internal static void AddChannel<T>(this IServiceCollection services)
    {
        var channel = Channel.CreateUnbounded<T>();
        services.AddSingleton(channel);
        services.AddSingleton(channel.Reader);
        services.AddSingleton(channel.Writer);
    }
}

/// <summary>
/// Task result
/// </summary>
/// <typeparam name="TResult">Return type</typeparam>
/// <param name="Status">Status</param>
/// <param name="Result">Result</param>
public record TaskResult<TResult>(TaskStatus Status, TResult? Result = default);

/// <summary>
/// Request response
/// </summary>
/// <param name="Title">Response message</param>
/// <param name="Status">Status code</param>
public record RequestResponse(string Title, int Status = StatusCodes.Status400BadRequest);

/// <summary>
/// Request response
/// </summary>
/// <param name="Title">Response message</param>
/// <param name="Data">Data</param>
/// <param name="Status">Status code</param>
public record RequestResponse<T>(string Title, T Data, int Status = StatusCodes.Status400BadRequest);

/// <summary>
/// Answer verification result
/// </summary>
/// <param name="SubType">Submission type</param>
/// <param name="AnsRes">Flag submission result</param>
public record VerifyResult(SubmissionType SubType, AnswerResult AnsRes);

/// <summary>
/// Team information
/// </summary>
/// <param name="Id">Team ID</param>
/// <param name="Name">Team name</param>
/// <param name="Avatar">Team avatar</param>
public record TeamModel(int Id, string Name, string? Avatar)
{
    internal static TeamModel FromTeam(Team team) => new(team.Id, team.Name, team.AvatarUrl);
}

/// <summary>
/// Challenge information
/// </summary>
/// <param name="Id">Challenge ID</param>
/// <param name="Title">Challenge title</param>
/// <param name="Category">Challenge category</param>
public record ChallengeModel(int Id, string Title, ChallengeCategory Category)
{
    internal static ChallengeModel FromChallenge(GameChallenge chal) => new(chal.Id, chal.Title, chal.Category);
}

/// <summary>
/// Team participation information
/// </summary>
/// <param name="Id">Participation ID</param>
/// <param name="Team">Team information</param>
/// <param name="Status">Team participation status</param>
/// <param name="Division">Team division</param>
public record ParticipationModel(int Id, TeamModel Team, ParticipationStatus Status, string? Division)
{
    internal static ParticipationModel FromParticipation(Participation part) =>
        new(part.Id, TeamModel.FromTeam(part.Team), part.Status, part.Division);
}

/// <summary>
/// List response
/// </summary>
/// <typeparam name="T"></typeparam>
public class ArrayResponse<T>(T[] array, int? tot = null)
    where T : class
{
    /// <summary>
    /// Data
    /// </summary>
    [Required]
    public T[] Data { get; set; } = array;

    /// <summary>
    /// Data length
    /// </summary>
    [Required]
    public int Length => Data.Length;

    /// <summary>
    /// Total length
    /// </summary>
    public int Total { get; set; } = tot ?? array.Length;
}

/// <summary>
/// File record
/// </summary>
public class FileRecord
{
    /// <summary>
    /// File name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// File size
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// File modification date
    /// </summary>
    public DateTimeOffset UpdateTime { get; set; } = DateTimeOffset.Now;

    internal static FileRecord FromFileInfo(FileInfo info) => new()
    {
        FileName = info.Name,
        UpdateTime = info.LastWriteTimeUtc,
        Size = info.Length
    };
}

/// <summary>
/// Blood bonus
/// </summary>
public readonly struct BloodBonus(long init = BloodBonus.DefaultValue)
{
    public const long DefaultValue = (50 << 20) + (30 << 10) + 10;
    const int Mask = 0x3ff;
    const int Base = 1000;

    public long Val { get; } = init;

    public static BloodBonus FromValue(long value)
    {
        if ((value & Mask) > Base || ((value >> 10) & Mask) > Base || ((value >> 20) & Mask) > Base)
            return new();
        return new(value);
    }

    public long FirstBlood => (Val >> 20) & 0x3ff;

    public float FirstBloodFactor => FirstBlood / 1000f + 1.0f;

    public long SecondBlood => (Val >> 10) & 0x3ff;

    public float SecondBloodFactor => SecondBlood / 1000f + 1.0f;

    public long ThirdBlood => Val & 0x3ff;

    public float ThirdBloodFactor => ThirdBlood / 1000f + 1.0f;

    public bool NoBonus => Val == 0;
}
