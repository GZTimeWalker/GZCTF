using System.ComponentModel.DataAnnotations;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GZCTF.Utils;

public static class ChannelService
{
    internal static IServiceCollection AddChannel<T>(this IServiceCollection services)
    {
        var channel = Channel.CreateUnbounded<T>();
        services.AddSingleton(channel);
        services.AddSingleton(channel.Reader);
        services.AddSingleton(channel.Writer);
        return services;
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
public record RequestResponse(string Title, int Status = 400);

/// <summary>
/// 请求响应
/// </summary>
/// <param name="Title">响应信息</param>
/// <param name="Data">数据</param>
/// <param name="Status">状态码</param>
public record RequestResponse<T>(string Title, T Data, int Status = 400);

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
    internal static TeamModel FromTeam(Team team)
        => new(team.Id, team.Name, team.AvatarUrl);
}

/// <summary>
/// 题目信息
/// </summary>
/// <param name="Id">题目 ID</param>
/// <param name="Title">题目名称</param>
/// <param name="Tag">题目标签</param>
public record ChallengeModel(int Id, string Title, ChallengeTag Tag)
{
    internal static ChallengeModel FromChallenge(Challenge chal)
        => new(chal.Id, chal.Title, chal.Tag);
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
    internal static ParticipationModel FromParticipation(Participation part)
        => new(part.Id, TeamModel.FromTeam(part.Team), part.Status, part.Organization);
}

/// <summary>
/// 列表响应
/// </summary>
/// <typeparam name="T"></typeparam>
public class ArrayResponse<T> where T : class
{
    public ArrayResponse(T[] array, int? tot = null)
    {
        Data = array;
        Total = tot ?? array.Length;
    }

    /// <summary>
    /// 数据
    /// </summary>
    [Required]
    public T[] Data { get; set; }

    /// <summary>
    /// 数据长度
    /// </summary>
    [Required]
    public int Length => Data.Length;

    /// <summary>
    /// 总长度
    /// </summary>
    public int Total { get; set; }
}

/// <summary>
/// 三血加分
/// </summary>
public struct BloodBonus
{
    public const long DefaultValue = (50 << 20) + (30 << 10) + 10;
    public const int Mask = 0x3ff;
    public const int Base = 1000;

    public BloodBonus(long init = DefaultValue) => Val = init;

    public static BloodBonus Default => new();

    public long Val { get; private set; } = DefaultValue;

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

    public static ValueConverter<BloodBonus, long> Converter => new(v => v.Val, v => new(v));

    public static ValueComparer<BloodBonus> Comparer => new((a, b) => a.Val == b.Val, c => c.Val.GetHashCode());
}
