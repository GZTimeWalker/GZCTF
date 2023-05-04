using System.ComponentModel.DataAnnotations;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CTFServer.Utils;

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