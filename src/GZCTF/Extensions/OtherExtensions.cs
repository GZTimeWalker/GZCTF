using System.Net;
using System.Text.Json;

namespace GZCTF.Extensions;

public static class ListExtensions
{
    extension<T>(IList<T> list)
    {
        public int GetSetHashCode() =>
            list.Count + list.Distinct().Aggregate(0, (x, y) => x.GetHashCode() ^ y?.GetHashCode() ?? 0xdead);
    }
}

public static class QueryableExtensions
{
    extension<T>(IQueryable<T> items)
    {
        /// <summary>
        /// Take part of the data if count is greater than 0, otherwise take all data
        /// Warn: Injections may occur if the count is not validated
        /// </summary>
        /// <returns></returns>
        public IQueryable<T> TakeAllIfZero(int count = 100, int skip = 0) =>
            count switch
            {
                > 0 => items.Skip(skip).Take(count),
                _ => items
            };
    }
}

public static class ArrayExtensions
{
    extension<T>(IEnumerable<T> array) where T : class
    {
        public ArrayResponse<T> ToResponse(int? tot = null) =>
            array switch
            {
                null => new([]),
                T[] arr => new(arr, tot),
                _ => new(array.ToArray(), tot)
            };
    }
}

public static class IPAddressExtensions
{
    extension(string? host)
    {
        public IEnumerable<IPAddress> ResolveIP() =>
            !string.IsNullOrWhiteSpace(host)
                ? Dns.GetHostAddresses(host)
                : [];
    }
}

internal static class JsonSerializerOptionsExtensions
{
    extension(JsonSerializerOptions options)
    {
        public void ConfigCustomSerializerOptions()
        {
            options.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
            options.Converters.Add(new DateTimeOffsetJsonConverter());
            options.Converters.Add(new IPAddressJsonConverter());
        }
    }
}
