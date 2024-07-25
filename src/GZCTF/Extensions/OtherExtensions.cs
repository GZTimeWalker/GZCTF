using System.Net;

namespace GZCTF.Extensions;

public static class ListExtensions
{
    public static int GetSetHashCode<T>(this IList<T> list) =>
        list.Count + list.Distinct().Aggregate(0, (x, y) => x.GetHashCode() ^ y?.GetHashCode() ?? 0xdead);
}

public static class QueryableExtensions
{
    /// <summary>
    /// Take part of the data if count is greater than 0, otherwise take all data
    /// Warn: Injections may occur if the count is not validated
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
