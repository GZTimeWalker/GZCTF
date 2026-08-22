using System.Text.RegularExpressions;

namespace GZCTF.Utils;

internal static class RegexHelper
{
    private const int CacheCapacity = 128;
    private static readonly Dictionary<string, LinkedListNode<CachedRegex>> Cache = new();
    private static readonly LinkedList<CachedRegex> LruList = [];

    /// <summary>
    /// The cached regex record
    /// </summary>
    /// <param name="Pattern">The regex pattern</param>
    /// <param name="Regex">The compiled regex instance, or null if compilation failed</param>
    internal record CachedRegex(string Pattern, Regex? Regex);

    internal static Regex? GetCompiledRegex(string pattern)
    {
        if (Cache.TryGetValue(pattern, out var node))
        {
            LruList.Remove(node);
            LruList.AddLast(node);
            return node.Value.Regex;
        }

        Regex? regex;
        try
        {
            regex = new Regex(pattern, RegexOptions.Compiled);
        }
        catch
        {
            regex = null;
        }

        var newNode = new LinkedListNode<CachedRegex>(new(pattern, regex));
        Cache[pattern] = newNode;
        LruList.AddLast(newNode);

        if (Cache.Count <= CacheCapacity || LruList.First is not { } oldestNode)
            return regex;

        LruList.RemoveFirst();
        Cache.Remove(oldestNode.Value.Pattern);

        return regex;
    }
}
