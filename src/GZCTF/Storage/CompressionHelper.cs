using System.IO.Compression;
using ZstdSharp;

namespace GZCTF.Storage;

/// <summary>
/// Provides helper methods for creating compression streams and resolving
/// compression-related file extensions.
/// </summary>
public static class CompressionHelper
{
    /// <summary>
    /// Wraps <paramref name="output"/> in a write-mode compression stream for the given format.
    /// The caller writes uncompressed data into the returned stream, which compresses and
    /// forwards it to <paramref name="output"/>.
    /// <para>
    /// The underlying <paramref name="output"/> stream is left open when the compressor is disposed,
    /// allowing the caller to continue using it (e.g. to rewind or flush).
    /// </para>
    /// </summary>
    /// <param name="output">The destination stream that receives compressed bytes.</param>
    /// <param name="format">The compression algorithm to apply.</param>
    /// <returns>
    /// A writable compression stream, or <paramref name="output"/> itself when
    /// <paramref name="format"/> is <see cref="CompressionFormat.None"/>.
    /// </returns>
    public static Stream CreateCompressionStream(Stream output, CompressionFormat format) =>
        format switch
        {
            CompressionFormat.GZip => new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true),
            CompressionFormat.Zstd => new CompressionStream(output, leaveOpen: true),
            CompressionFormat.None => output,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };

    /// <summary>
    /// Appends the conventional file extension for <paramref name="format"/> to the given path
    /// (e.g. <c>.gz</c> for GZip, <c>.zst</c> for ZStandard). Returns the path unchanged
    /// when no compression is applied.
    /// </summary>
    /// <param name="path">The original storage path.</param>
    /// <param name="format">The compression format whose extension should be appended.</param>
    /// <returns>The path with the appropriate extension appended, or the original path.</returns>
    public static string AppendExtension(string path, CompressionFormat format) =>
        format switch
        {
            CompressionFormat.GZip => path + ".gz",
            CompressionFormat.Zstd => path + ".zst",
            _ => path
        };
}
