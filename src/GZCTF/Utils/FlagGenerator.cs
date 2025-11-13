using System.Diagnostics.CodeAnalysis;
using System.Text;
using GZCTF.Models;

namespace GZCTF.Utils;

/// <summary>
/// LRU Cache for parsed flag templates
/// </summary>
internal class FlagTemplateCache(int capacity = 64)
{
    private readonly Dictionary<string, FlagTemplate> _cache = new(capacity);
    private readonly LinkedList<string> _accessOrder = [];

    public bool TryGet(string template, [MaybeNullWhen(false)] out FlagTemplate result)
    {
        if (_cache.TryGetValue(template, out var cached))
        {
            _accessOrder.Remove(template);
            _accessOrder.AddLast(template);
            result = cached;
            return true;
        }

        result = null;
        return false;
    }

    public void Set(string template, FlagTemplate parsed)
    {
        if (_cache.ContainsKey(template))
        {
            _cache[template] = parsed;
            _accessOrder.Remove(template);
        }
        else
        {
            if (_cache.Count >= capacity && _accessOrder.Count > 0)
            {
                var lru = _accessOrder.First!.Value;
                _accessOrder.RemoveFirst();
                _cache.Remove(lru);
            }

            _cache[template] = parsed;
        }

        _accessOrder.AddLast(template);
    }
}

/// <summary>
/// Leet conversion mode
/// </summary>
public enum LeetMode
{
    /// <summary>
    /// No Leet conversion (when [GUID] or [TEAM_HASH] present)
    /// </summary>
    None = 0,

    /// <summary>
    /// Default Leet conversion (no explicit marker, no GUID/TEAM_HASH)
    /// </summary>
    Default = 1,

    /// <summary>
    /// [LEET] marker - basic Leet conversion
    /// </summary>
    Leet = 2,

    /// <summary>
    /// [CLEET] marker - Leet conversion with special characters
    /// </summary>
    CLeet = 3
}

/// <summary>
/// Flag segment type
/// </summary>
public enum FlagSegmentType
{
    /// <summary>
    /// Plain text (no Leet conversion)
    /// </summary>
    PlainText,

    /// <summary>
    /// Text that needs Leet conversion
    /// </summary>
    LeetText,

    /// <summary>
    /// [TEAM_HASH] placeholder
    /// </summary>
    TeamHash,

    /// <summary>
    /// [GUID] placeholder
    /// </summary>
    Guid
}

/// <summary>
/// Flag segment
/// </summary>
public record FlagSegment
{
    public required FlagSegmentType Type { get; init; }

    /// <summary>
    /// Range in the original template string (for PlainText/LeetText)
    /// Null for placeholder segments (Guid/TeamHash)
    /// </summary>
    public Range? ContentRange { get; init; }
}

/// <summary>
/// Parsed flag template structure with ordered segments
/// </summary>
public record FlagTemplate
{
    /// <summary>
    /// Original template string (for Span access)
    /// </summary>
    public required string Template { get; init; }

    /// <summary>
    /// Leet conversion mode
    /// </summary>
    public LeetMode LeetMode { get; init; }

    /// <summary>
    /// Ordered list of flag segments
    /// </summary>
    public required List<FlagSegment> Segments { get; init; }

    /// <summary>
    /// Estimated final flag length (for StringBuilder capacity)
    /// </summary>
    public int EstimatedLength { get; init; }

    /// <summary>
    /// Whether template has sufficient randomness (GUID or TEAM_HASH)
    /// Templates with GUID/TEAM_HASH are considered valid without checking Leet entropy
    /// </summary>
    public bool HasSufficientRandomness { get; init; }
}

/// <summary>
/// Dynamic flag generator with optimized template parsing
/// </summary>
public class DynamicFlagGenerator(string? template)
{
    private static readonly FlagTemplateCache Cache = new();
    private static readonly Lock CacheLock = new();
    private static readonly Random Random = new();

    private static readonly Dictionary<char, string> CharMap = new()
    {
        ['A'] = "Aa4",
        ['B'] = "Bb68",
        ['C'] = "Cc",
        ['D'] = "Dd",
        ['E'] = "Ee3",
        ['F'] = "Ff1",
        ['G'] = "Gg69",
        ['H'] = "Hh",
        ['I'] = "Ii1l",
        ['J'] = "Jj",
        ['K'] = "Kk",
        ['L'] = "Ll1I",
        ['M'] = "Mm",
        ['N'] = "Nn",
        ['O'] = "Oo0",
        ['P'] = "Pp",
        ['Q'] = "Qq9",
        ['R'] = "Rr",
        ['S'] = "Ss5",
        ['T'] = "Tt7",
        ['U'] = "Uu",
        ['V'] = "Vv",
        ['W'] = "Ww",
        ['X'] = "Xx",
        ['Y'] = "Yy",
        ['Z'] = "Zz2",
        ['0'] = "0oO",
        ['1'] = "1lI",
        ['2'] = "2zZ",
        ['3'] = "3eE",
        ['4'] = "4aA",
        ['5'] = "5Ss",
        ['6'] = "6Gb",
        ['7'] = "7T",
        ['8'] = "8bB",
        ['9'] = "9g"
    };

    private static readonly Dictionary<char, string> ComplexCharMap = new()
    {
        ['A'] = "Aa4@",
        ['B'] = "Bb6&",
        ['C'] = "Cc(",
        ['D'] = "Dd",
        ['E'] = "Ee3",
        ['F'] = "Ff1",
        ['G'] = "Gg69",
        ['H'] = "Hh",
        ['I'] = "Ii1l!",
        ['J'] = "Jj",
        ['K'] = "Kk",
        ['L'] = "Ll1I!",
        ['M'] = "Mm",
        ['N'] = "Nn",
        ['O'] = "Oo0#",
        ['P'] = "Pp",
        ['Q'] = "Qq9",
        ['R'] = "Rr",
        ['S'] = "Ss5$",
        ['T'] = "Tt7",
        ['U'] = "Uu",
        ['V'] = "Vv",
        ['W'] = "Ww",
        ['X'] = "Xx",
        ['Y'] = "Yy",
        ['Z'] = "Zz2?",
        ['0'] = "0oO#",
        ['1'] = "1lI|",
        ['2'] = "2zZ?",
        ['3'] = "3eE",
        ['4'] = "4aA",
        ['5'] = "5Ss",
        ['6'] = "6Gb",
        ['7'] = "7T",
        ['8'] = "8B&",
        ['9'] = "9g"
    };

    private readonly FlagTemplate _template = ParseTemplate(template);

    public FlagTemplate FlagTemplate => _template;

    /// <summary>
    /// Parse template with caching
    /// </summary>
    private static FlagTemplate ParseTemplate(string? template)
    {
        if (string.IsNullOrEmpty(template))
        {
            return new FlagTemplate
            {
                Template = string.Empty,
                LeetMode = LeetMode.None,
                Segments = [],
                EstimatedLength = 0,
                HasSufficientRandomness = false
            };
        }

        lock (CacheLock)
        {
            if (Cache.TryGet(template, out var cached))
                return cached;
        }

        // Detect leet markers
        var leetMode = LeetMode.Default;

        string processedTemplate;
        if (template.StartsWith("[LEET]"))
        {
            leetMode = LeetMode.Leet;
            processedTemplate = template[6..];
        }
        else if (template.StartsWith("[CLEET]"))
        {
            leetMode = LeetMode.CLeet;
            processedTemplate = template[7..];
        }
        else
        {
            processedTemplate = template;
        }

        var segments = ParseSegments(
            processedTemplate,
            out var hasSufficientRandomness,
            out var estimatedLength);

        if (leetMode is LeetMode.Default && hasSufficientRandomness)
            leetMode = LeetMode.None;

        var result = new FlagTemplate
        {
            Template = processedTemplate,
            LeetMode = leetMode,
            Segments = segments,
            EstimatedLength = estimatedLength,
            HasSufficientRandomness = hasSufficientRandomness
        };

        lock (CacheLock)
        {
            Cache.Set(template, result);
        }

        return result;
    }

    /// <summary>
    /// Parse template into ordered segments (independent of LeetMode)
    /// Braced content is marked as LeetText, others as PlainText
    /// </summary>
    private static List<FlagSegment> ParseSegments(
        ReadOnlySpan<char> span,
        out bool hasSufficientRandomness,
        out int estimatedLength)
    {
        var segments = new List<FlagSegment>();
        var braceDepth = 0;
        var segmentStart = 0;
        var index = 0;
        var length = 0;
        var hasGuidOrTeamHash = false;

        while (index < span.Length)
        {
            // Check for placeholders using pattern matching
            var remaining = span[index..];
            if (remaining is ['[', ..])
            {
                if (remaining.StartsWith("[GUID]"))
                {
                    FlushText(index, braceDepth > 0 ? FlagSegmentType.LeetText : FlagSegmentType.PlainText);
                    segments.Add(new FlagSegment { Type = FlagSegmentType.Guid });
                    hasGuidOrTeamHash = true;
                    length += 36; // GUID format
                    index += 6;
                    segmentStart = index;
                    continue;
                }

                if (remaining.StartsWith("[TEAM_HASH]"))
                {
                    FlushText(index, braceDepth > 0 ? FlagSegmentType.LeetText : FlagSegmentType.PlainText);
                    segments.Add(new FlagSegment { Type = FlagSegmentType.TeamHash });
                    hasGuidOrTeamHash = true;
                    length += 12; // 12 hex chars
                    index += 11;
                    segmentStart = index;
                    continue;
                }
            }

            // Track brace state for Leet scope using pattern matching
            switch (span[index])
            {
                case '{' when braceDepth++ == 0:
                    FlushText(index + 1, FlagSegmentType.PlainText); // Include '{'
                    break;
                case '}' when --braceDepth == 0:
                    FlushText(index + 1, FlagSegmentType.LeetText); // Include '}'
                    break;
            }

            index++;
        }

        FlushText(span.Length, braceDepth > 0 ? FlagSegmentType.LeetText : FlagSegmentType.PlainText);
        hasSufficientRandomness = hasGuidOrTeamHash;
        estimatedLength = length;
        return segments;

        void FlushText(int end, FlagSegmentType type)
        {
            if (end > segmentStart)
            {
                segments.Add(new FlagSegment { Type = type, ContentRange = segmentStart..end });
                length += end - segmentStart;
            }

            segmentStart = end;
        }
    }

    /// <summary>
    /// Generate flag with team hash replacement
    /// </summary>
    /// <param name="teamHashProvider">Lazy team hash provider function</param>
    public string GenerateWithTeamHash(Func<string> teamHashProvider)
    {
        if (string.IsNullOrEmpty(template))
            return $"flag{Guid.NewGuid():B}";

        string? cachedTeamHash = null;
        return GenerateFlag(() => cachedTeamHash ??= teamHashProvider());
    }

    /// <summary>
    /// Generate test flag with TestTeamHash
    /// </summary>
    public string GenerateTestFlag() => string.IsNullOrEmpty(template)
        ? "flag{GZCTF_dynamic_flag_test}"
        : GenerateFlag(() => "TestTeamHash");

    /// <summary>
    /// Core flag generation logic
    /// </summary>
    /// <param name="teamHashResolver">Function to resolve TeamHash segments</param>
    private string GenerateFlag(Func<string> teamHashResolver)
    {
        var builder = new StringBuilder(_template.EstimatedLength);
        var applyLeet = _template.LeetMode is not LeetMode.None;
        var map = _template.LeetMode is LeetMode.CLeet ? ComplexCharMap : CharMap;
        var templateSpan = _template.Template.AsSpan();

        foreach (var segment in _template.Segments)
        {
            switch (segment.Type)
            {
                case FlagSegmentType.PlainText when segment.ContentRange.HasValue:
                    builder.Append(templateSpan[segment.ContentRange.Value]);
                    break;

                case FlagSegmentType.LeetText when segment.ContentRange.HasValue:
                    var content = templateSpan[segment.ContentRange.Value];
                    if (applyLeet)
                        LeetSpan(content, builder, map);
                    else
                        builder.Append(content);
                    break;

                case FlagSegmentType.TeamHash:
                    builder.Append(teamHashResolver());
                    break;

                case FlagSegmentType.Guid:
                    builder.Append(Guid.NewGuid().ToString("D"));
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Apply Leet conversion to a span segment
    /// </summary>
    private static void LeetSpan(ReadOnlySpan<char> segment, StringBuilder builder,
        Dictionary<char, string> map)
    {
        foreach (var c in segment)
        {
            if (map.TryGetValue(char.ToUpperInvariant(c), out var table))
            {
                var nc = table[Random.Next(table.Length)];
                builder.Append(nc);
            }
            else
            {
                builder.Append(c is ' ' ? '_' : c);
            }
        }
    }

    /// <summary>
    /// Calculate Leet entropy for the flag template
    /// </summary>
    public double CalculateEntropy()
    {
        if (_template.LeetMode is LeetMode.None)
            return 0;

        var map = _template.LeetMode is LeetMode.CLeet ? ComplexCharMap : CharMap;
        var entropy = 0.0;
        var templateSpan = _template.Template.AsSpan();

        foreach (var segment in _template.Segments)
        {
            if (segment.Type is not FlagSegmentType.LeetText || !segment.ContentRange.HasValue)
                continue;

            foreach (var c in templateSpan[segment.ContentRange.Value])
            {
                if (map.TryGetValue(char.ToUpperInvariant(c), out var table))
                    entropy += Math.Log(table.Length, 2);
            }
        }

        return entropy;
    }

    /// <summary>
    /// Validate if the flag template is valid
    /// </summary>
    /// <param name="minEntropy">Minimum required entropy (default 32.0)</param>
    /// <returns>True if template is valid</returns>
    public bool IsValid(double minEntropy = 32.0)
    {
        if (string.IsNullOrWhiteSpace(template))
            return false;

        // Templates with GUID or TEAM_HASH have sufficient randomness
        if (_template.HasSufficientRandomness)
            return true;

        // If the estimated length is too long (than the database limit)
        if (_template.EstimatedLength > Limits.MaxFlagLength)
            return false;

        // Otherwise, check if Leet entropy meets the minimum requirement
        return _template.LeetMode is not LeetMode.None && CalculateEntropy() >= minEntropy;
    }
}
