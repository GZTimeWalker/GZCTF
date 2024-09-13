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
/// 任务结果
/// </summary>
/// <typeparam name="TResult">返回值类型</typeparam>
/// <param name="Status">状态</param>
/// <param name="Result">结果</param>
public record TaskResult<TResult>(TaskStatus Status, TResult? Result = default);

/// <summary>
/// 请求响应
/// </summary>
/// <param name="Title">响应信息</param>
/// <param name="Status">状态码</param>
public record RequestResponse(string Title, int Status = StatusCodes.Status400BadRequest);

/// <summary>
/// 请求响应
/// </summary>
/// <param name="Title">响应信息</param>
/// <param name="Data">数据</param>
/// <param name="Status">状态码</param>
public record RequestResponse<T>(string Title, T Data, int Status = StatusCodes.Status400BadRequest);

/// <summary>
/// 答案校验结果
/// </summary>
/// <param name="SubType">提交类型</param>
/// <param name="AnsRes">提交 flag 判定结果</param>
public record VerifyResult(SubmissionType SubType, AnswerResult AnsRes);

/// <summary>
/// 队伍信息
/// </summary>
/// <param name="Id">队伍 ID</param>
/// <param name="Name">队名</param>
/// <param name="Avatar">队伍头像</param>
public record TeamModel(int Id, string Name, string? Avatar)
{
    internal static TeamModel FromTeam(Team team) => new(team.Id, team.Name, team.AvatarUrl);
}

/// <summary>
/// 题目信息
/// </summary>
/// <param name="Id">题目 ID</param>
/// <param name="Title">题目名称</param>
/// <param name="Category">题目类别</param>
public record ChallengeModel(int Id, string Title, ChallengeCategory Category)
{
    internal static ChallengeModel FromChallenge(GameChallenge chal) => new(chal.Id, chal.Title, chal.Category);
}

/// <summary>
/// 队伍参与信息
/// </summary>
/// <param name="Id">参与 Id</param>
/// <param name="Team">队伍信息</param>
/// <param name="Status">队伍参与状态</param>
/// <param name="Organization">队伍所属组织</param>
public record ParticipationModel(int Id, TeamModel Team, ParticipationStatus Status, string? Organization)
{
    internal static ParticipationModel FromParticipation(Participation part) =>
        new(part.Id, TeamModel.FromTeam(part.Team), part.Status, part.Organization);
}

/// <summary>
/// 列表响应
/// </summary>
/// <typeparam name="T"></typeparam>
public class ArrayResponse<T>(T[] array, int? tot = null)
    where T : class
{
    /// <summary>
    /// 数据
    /// </summary>
    [Required]
    public T[] Data { get; set; } = array;

    /// <summary>
    /// 数据长度
    /// </summary>
    [Required]
    public int Length => Data.Length;

    /// <summary>
    /// 总长度
    /// </summary>
    public int Total { get; set; } = tot ?? array.Length;
}

/// <summary>
/// 文件记录
/// </summary>
public class FileRecord
{
    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// 文件修改日期
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
/// 三血加分
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
