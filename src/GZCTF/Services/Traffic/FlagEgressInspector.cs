namespace GZCTF.Services.Traffic;

/// <summary>
/// Per-recorder, per-direction scanner that detects a specific flag byte
/// sequence inside a stream of buffer chunks. Maintains a tail buffer of size
/// (flagLen - 1) so flags that straddle a buffer boundary are still found.
///
/// Thread-safety: not thread-safe by design. One inspector per (recorder, direction);
/// CaptureNetworkStream's read and write paths are inherently single-threaded
/// per direction.
/// </summary>
internal sealed class FlagEgressInspector
{
    readonly byte[] _flag;
    readonly byte[] _tail;
    int _tailLen;

    public FlagEgressInspector(byte[] flagBytes)
    {
        _flag = flagBytes;
        _tail = new byte[Math.Max(0, flagBytes.Length - 1)];
    }

    /// <summary>
    /// Returns true if the flag is found either entirely inside <paramref name="data"/>
    /// or straddling the boundary from the previous call. Updates the tail buffer.
    /// </summary>
    public bool Inspect(ReadOnlySpan<byte> data)
    {
        if (data.Length == 0 || _flag.Length == 0)
            return false;

        var hit = false;

        if (_tailLen > 0)
        {
            var pre = Math.Min(data.Length, _flag.Length - 1);
            Span<byte> straddle = stackalloc byte[_tailLen + pre];
            _tail.AsSpan(0, _tailLen).CopyTo(straddle);
            data[..pre].CopyTo(straddle[_tailLen..]);

            if (straddle.IndexOf(_flag) >= 0)
                hit = true;
        }

        if (!hit && data.IndexOf(_flag) >= 0)
            hit = true;

        var keep = Math.Min(data.Length, _flag.Length - 1);
        if (keep > 0)
        {
            data[^keep..].CopyTo(_tail);
        }
        _tailLen = keep;

        return hit;
    }
}
