using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Options;
using ZstdSharp;

namespace GZCTF.Utils;

public class ZStandardCompressionProvider : ICompressionProvider
{
    public ZStandardCompressionProvider(IOptions<ZStandardCompressionProviderOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        Options = options.Value;
    }

    private ZStandardCompressionProviderOptions Options { get; }

    public string EncodingName => "zstd";

    public bool SupportsFlush => true;

    public Stream CreateStream(Stream outputStream) =>
        new CompressionStream(outputStream, Options.Level, leaveOpen: true);
}

public class ZStandardCompressionProviderOptions
{
    public int Level { get; set; } = Compressor.DefaultCompressionLevel;
}
