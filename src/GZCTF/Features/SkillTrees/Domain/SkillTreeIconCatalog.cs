namespace GZCTF.Features.SkillTrees.Domain;

public static class SkillTreeIconCatalog
{
    public const string Default = "flag";

    public static IReadOnlyDictionary<string, string> Values { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["flag"] = "🚩",
            ["web"] = "🕸️",
            ["crypto"] = "🔐",
            ["pwn"] = "💣",
            ["brain"] = "🧠",
            ["ai"] = "🤖"
        };

    public static bool IsSupported(string? key) =>
        key is not null && Values.ContainsKey(key);
}
