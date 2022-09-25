namespace CTFServer.Extensions;

public static class ListHashExtensions
{
    public static int GetSetHashCode<T>(this IList<T> list)
        => list.Count + list.Distinct().Aggregate(0, (x, y) => x.GetHashCode() ^ y?.GetHashCode() ?? 0xdead);
}

public static class IQueryableExtensions
{
    /// <summary>
    /// 如果 count 大于 0 则只获取部分
    /// Warn: 可能造成恶意参数注入获取全部数据
    /// </summary>
    /// <returns></returns>
    public static IQueryable<T> TakeAllIfZero<T>(this IQueryable<T> items, int count = 100, int skip = 0)
        => count switch
        {
            > 0 => items.Skip(skip).Take(count),
            _ => items
        };
}