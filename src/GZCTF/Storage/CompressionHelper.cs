using System.IO.Compression;
using ZstdSharp;

namespace GZCTF.Storage;

public static class CompressionHelper
{
    public static Stream CreateCompressionStream(Stream output, CompressionFormat format) =>
        format switch
        {
            CompressionFormat.GZip => new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true),
            CompressionFormat.Zstd => new CompressionStream(output, leaveOpen: true),
            CompressionFormat.None => output,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };

    public static string AppendExtension(string path, CompressionFormat format) =>
        format switch
        {
            CompressionFormat.GZip => path + ".gz",
            CompressionFormat.Zstd => path + ".zst",
            _ => path
        };
}
