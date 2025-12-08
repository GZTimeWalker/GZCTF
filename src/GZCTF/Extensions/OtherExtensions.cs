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
        public IPAddress[] ResolveIP() =>
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

internal static class UserMetadataFieldTypeExtensions
{
    extension(UserMetadataFieldType type)
    {
        internal UserMetadataFieldValueType GetFieldValueType() =>
            type switch
            {
                UserMetadataFieldType.Number => UserMetadataFieldValueType.Number,
                UserMetadataFieldType.Boolean => UserMetadataFieldValueType.Boolean,
                UserMetadataFieldType.Text => UserMetadataFieldValueType.String,
                UserMetadataFieldType.TextArea => UserMetadataFieldValueType.String,
                UserMetadataFieldType.Email => UserMetadataFieldValueType.String,
                UserMetadataFieldType.Url => UserMetadataFieldValueType.String,
                UserMetadataFieldType.Phone => UserMetadataFieldValueType.String,
                UserMetadataFieldType.Select => UserMetadataFieldValueType.String,
                UserMetadataFieldType.Date => UserMetadataFieldValueType.DateTime,
                UserMetadataFieldType.MultiSelect => UserMetadataFieldValueType.String,
                UserMetadataFieldType.DateTime => UserMetadataFieldValueType.DateTime,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
    }
}