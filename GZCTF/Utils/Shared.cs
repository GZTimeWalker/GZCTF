using System.ComponentModel.DataAnnotations;

namespace CTFServer.Utils;

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
public class ArrayResponse<T> where T: class
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