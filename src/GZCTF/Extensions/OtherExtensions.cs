using System.Net;
using GZCTF.Models;
using Microsoft.Extensions.Localization;

namespace GZCTF.Extensions;

public static class ListExtensions
{
    public static int GetSetHashCode<T>(this IList<T> list) =>
        list.Count + list.Distinct().Aggregate(0, (x, y) => x.GetHashCode() ^ y?.GetHashCode() ?? 0xdead);
}

public static class QueryableExtensions
{
    /// <summary>
    /// 如果 count 大于 0 则只获取部分
    /// Warn: 可能造成恶意参数注入获取全部数据
    /// </summary>
    /// <returns></returns>
    public static IQueryable<T> TakeAllIfZero<T>(this IQueryable<T> items, int count = 100, int skip = 0) =>
        count switch
        {
            > 0 => items.Skip(skip).Take(count),
            _ => items
        };
}

public static class ArrayExtensions
{
    public static ArrayResponse<T> ToResponse<T>(this IEnumerable<T> array, int? tot = null) where T : class =>
        array switch
        {
            null => new([]),
            T[] arr => new(arr, tot),
            _ => new(array.ToArray(), tot)
        };
}

public static class IPAddressExtensions
{
    public static IEnumerable<IPAddress> ResolveIP(this string? host) =>
        !string.IsNullOrWhiteSpace(host)
            ? Dns.GetHostAddresses(host)
            : [];
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
