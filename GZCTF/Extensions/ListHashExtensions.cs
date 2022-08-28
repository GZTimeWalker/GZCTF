using System;
namespace CTFServer.Extensions;

public static class ListHashExtensions
{
    public static int GetSetHashCode<T>(this IList<T> list)
        => list.Count + list.Distinct().Aggregate(0, (x, y) => x.GetHashCode() ^ y?.GetHashCode() ?? 0xdead);
}

