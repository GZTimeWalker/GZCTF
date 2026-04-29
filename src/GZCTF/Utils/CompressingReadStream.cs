using System.IO.Pipelines;
using GZCTF.Storage;

namespace GZCTF.Utils;

/// <summary>
/// A read-only stream that compresses data from a source stream in a background task,
/// using a <see cref="Pipe"/> as a producer-consumer bridge.
/// <para>
/// The S3 SDK reads from this stream as a normal forward-only stream while the
/// compressor writes compressed bytes into the pipe on a background task.
/// Backpressure is handled naturally by the pipe.
/// </para>
/// </summary>
internal sealed class CompressingReadStream : Stream
{
    private readonly Stream _reader;
    private readonly PipeWriter _writer;
    private readonly Task _compressionTask;

    /// <summary>
    /// Creates a new <see cref="CompressingReadStream"/>.
    /// </summary>
    /// <param name="source">The uncompressed source stream to read from.</param>
    /// <param name="format">The compression format to apply.</param>
    /// <param name="cancellationToken">Cancellation token observed by the background compression task.</param>
    internal CompressingReadStream(Stream source, CompressionFormat format, CancellationToken cancellationToken)
    {
        var pipe = new Pipe();
        _writer = pipe.Writer;
        _reader = pipe.Reader.AsStream();
        _compressionTask = CompressAsync(source, pipe.Writer, format, cancellationToken);
    }

    private static async Task CompressAsync(Stream source, PipeWriter pipeWriter, CompressionFormat format,
        CancellationToken cancellationToken)
    {
        Exception? error = null;
        try
        {
            var writerStream = pipeWriter.AsStream();
            await using var compressor = CompressionHelper.CreateCompressionStream(writerStream, format);
            await source.CopyToAsync(compressor, cancellationToken);
            await compressor.FlushAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            error = ex;
        }
        finally
        {
            await pipeWriter.CompleteAsync(error);
        }
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count) =>
        _reader.Read(buffer, offset, count);

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
        _reader.ReadAsync(buffer, cancellationToken);

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count,
        CancellationToken cancellationToken) =>
        _reader.ReadAsync(buffer, offset, count, cancellationToken);

    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override async ValueTask DisposeAsync()
    {
        // Signal the compressor to stop if it's still writing.
        await _writer.CompleteAsync();

        // Await the background task so exceptions are observed.
        // Suppress exceptions here — if the upload was cancelled or the pipe was
        // torn down early, the compressor task may fault and that's expected.
        try
        {
            await _compressionTask;
        }
        catch (OperationCanceledException)
        {
            // Expected on cancellation.
        }

        await _reader.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _writer.Complete();

            try
            {
                _compressionTask.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                // Expected on cancellation.
            }

            _reader.Dispose();
        }

        base.Dispose(disposing);
    }
}
