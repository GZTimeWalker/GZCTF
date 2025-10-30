using CsToml;

namespace GZCTF.Models.Transfer;

/// <summary>
/// File checksums for integrity verification
/// </summary>
[TomlSerializedObject(TomlNamingConvention.SnakeCase)]
public partial class TransferChecksums
{
    /// <summary>
    /// File path to checksum mapping
    /// </summary>
    [TomlValueOnSerialized]
    public Dictionary<string, string> Files { get; set; } = new();

    /// <summary>
    /// Checksum algorithm
    /// </summary>
    [TomlValueOnSerialized]
    public string Algorithm { get; set; } = "SHA256";
}
