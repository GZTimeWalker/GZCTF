using System;
using System.Text;
using GZCTF.Services.Traffic;
using Xunit;

namespace GZCTF.Test.UnitTests.Services;

/// <summary>
/// Tests for the per-direction flag-byte scanner used by the egress flag
/// tracer. The inspector must (a) find the flag inside a single buffer,
/// (b) find a flag split across two consecutive buffers via its tail
/// straddle scan, and (c) never produce a hit when the flag is absent —
/// including when the tail buffer would yield a substring of the flag
/// but nothing actually matches.
/// </summary>
public class FlagEgressInspectorTest
{
    static byte[] Flag(string s = "flag{1234567890abcdef-deadbeef-cafe}") => Encoding.UTF8.GetBytes(s);

    [Fact]
    public void Inspect_EmptyData_NoHit()
    {
        var inspector = new FlagEgressInspector(Flag());
        Assert.False(inspector.Inspect(ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void Inspect_FlagInSingleBuffer_Hit()
    {
        var flag = Flag();
        var inspector = new FlagEgressInspector(flag);

        var body = Encoding.UTF8.GetBytes($"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\n\r\nyour flag is: {Encoding.UTF8.GetString(flag)}\n");

        Assert.True(inspector.Inspect(body));
    }

    [Fact]
    public void Inspect_FlagAbsent_NoHit()
    {
        var inspector = new FlagEgressInspector(Flag());
        var body = Encoding.UTF8.GetBytes("nothing interesting here, just response bytes");
        Assert.False(inspector.Inspect(body));
    }

    [Fact]
    public void Inspect_FlagSplitAcrossTwoBuffers_Hit()
    {
        var flag = Flag();
        var inspector = new FlagEgressInspector(flag);

        // 10-byte prefix in the first call, remainder in the second call.
        var split = 10;
        var first = flag.AsSpan(0, split).ToArray();
        var second = flag.AsSpan(split).ToArray();

        Assert.False(inspector.Inspect(first));
        Assert.True(inspector.Inspect(second));
    }

    [Fact]
    public void Inspect_FlagSplitWithSurroundingNoise_Hit()
    {
        var flag = Flag();
        var inspector = new FlagEgressInspector(flag);

        var split = flag.Length - 3;
        var prefix = Encoding.UTF8.GetBytes("response chunk 1 trailer: ");
        var first = new byte[prefix.Length + split];
        prefix.CopyTo(first, 0);
        flag.AsSpan(0, split).CopyTo(first.AsSpan(prefix.Length));

        var suffix = Encoding.UTF8.GetBytes("\r\n--end--");
        var second = new byte[(flag.Length - split) + suffix.Length];
        flag.AsSpan(split).CopyTo(second.AsSpan(0));
        suffix.CopyTo(second.AsSpan(flag.Length - split));

        Assert.False(inspector.Inspect(first));
        Assert.True(inspector.Inspect(second));
    }

    [Fact]
    public void Inspect_TailDoesNotLeak_NoFalseHit()
    {
        var flag = Flag();
        var inspector = new FlagEgressInspector(flag);

        // First call ends with the flag prefix in the tail.
        var prefix = flag.AsSpan(0, flag.Length - 5).ToArray();
        Assert.False(inspector.Inspect(prefix));

        // Second call replaces the tail with unrelated bytes.
        var noise = Encoding.UTF8.GetBytes("XXXXXXXXXXXXXXXXXXXX");
        Assert.False(inspector.Inspect(noise));

        // Third call carries the *suffix* of the flag only — without
        // the actual prefix in the tail, the inspector must not fire.
        var suffix = flag.AsSpan(flag.Length - 5).ToArray();
        Assert.False(inspector.Inspect(suffix));
    }

    [Fact]
    public void Inspect_SameBufferTwice_BothHit()
    {
        var flag = Flag();
        var inspector = new FlagEgressInspector(flag);
        var body = Encoding.UTF8.GetBytes($"response 1: {Encoding.UTF8.GetString(flag)}");

        Assert.True(inspector.Inspect(body));
        Assert.True(inspector.Inspect(body));
    }

    [Fact]
    public void Inspect_BufferShorterThanFlag_NoFalseHit()
    {
        var flag = Flag();
        var inspector = new FlagEgressInspector(flag);
        var tinyChunk = flag.AsSpan(0, 6).ToArray();
        Assert.False(inspector.Inspect(tinyChunk));
    }

    [Fact(Skip = "v1 limitation: tail buffer covers a single-boundary straddle only. " +
                  "Byte-at-a-time delivery (e.g. an interactive shell echoing the flag) " +
                  "is not covered. Tracked for v1.1 — would require a sliding ring of " +
                  "flagLen-1 bytes across all prior buffers, not just the last one.")]
    public void Inspect_ManySmallChunksReassemble_Hit()
    {
        var flag = Flag();
        var inspector = new FlagEgressInspector(flag);

        var hit = false;
        for (var i = 0; i < flag.Length; i++)
        {
            hit |= inspector.Inspect(flag.AsSpan(i, 1));
        }
        Assert.True(hit);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(7)]
    [InlineData(15)]
    [InlineData(31)]
    public void Inspect_FlagSplitAtArbitraryOffset_Hit(int splitAt)
    {
        var flag = Flag();
        if (splitAt >= flag.Length) return;

        var inspector = new FlagEgressInspector(flag);
        Assert.False(inspector.Inspect(flag.AsSpan(0, splitAt)));
        Assert.True(inspector.Inspect(flag.AsSpan(splitAt)));
    }

    [Fact]
    public void Inspect_ShortFlag_StillWorks()
    {
        // Tiny flag — exercises edge case where tail buffer has length 0 or 1.
        var flag = Encoding.UTF8.GetBytes("ab");
        var inspector = new FlagEgressInspector(flag);

        Assert.True(inspector.Inspect(Encoding.UTF8.GetBytes("xxabxx")));

        // Re-test the straddle: 'a' at end of one buffer, 'b' at start of next.
        var inspector2 = new FlagEgressInspector(flag);
        Assert.False(inspector2.Inspect(Encoding.UTF8.GetBytes("xxa")));
        Assert.True(inspector2.Inspect(Encoding.UTF8.GetBytes("bxx")));
    }

    // --- Static-container flag shapes ---------------------------------------
    // StaticContainer challenges share a single fixed flag across all teams.
    // The flag tends to be shorter and without a per-team GUID tail, so a few
    // explicit cases here document that the inspector handles those shapes
    // as a first-class scenario (not just dynamic GUID flags).

    [Fact]
    public void Inspect_StaticChallengeFlag_SingleBuffer_Hit()
    {
        var flag = Encoding.UTF8.GetBytes("flag{ctf2024-shared-static-flag}");
        var inspector = new FlagEgressInspector(flag);

        var body = Encoding.UTF8.GetBytes(
            "HTTP/1.1 200 OK\r\nContent-Type: text/html\r\n\r\n" +
            "<html><body><h1>You found it!</h1><pre>flag{ctf2024-shared-static-flag}</pre></body></html>");

        Assert.True(inspector.Inspect(body));
    }

    [Fact]
    public void Inspect_StaticChallengeFlag_RepeatedInSameResponse_Hit()
    {
        // Some static challenges echo the flag in multiple places (Set-Cookie + body).
        // The inspector only needs to detect at least one occurrence; this asserts
        // it doesn't silently miss when the flag appears more than once.
        var flag = Encoding.UTF8.GetBytes("FLAG{static-multiecho}");
        var inspector = new FlagEgressInspector(flag);

        var body = Encoding.UTF8.GetBytes(
            "Set-Cookie: token=FLAG{static-multiecho}; Path=/\r\n" +
            "\r\n" +
            "Welcome! Your flag is FLAG{static-multiecho}.");

        Assert.True(inspector.Inspect(body));
    }

    [Fact]
    public void Inspect_StaticChallengeFlag_SplitAcrossBuffers_Hit()
    {
        // Short static flags can still straddle a TCP boundary; verify the same
        // straddle scan works as for dynamic flags.
        var flag = Encoding.UTF8.GetBytes("flag{short-static}");
        var inspector = new FlagEgressInspector(flag);

        var first = Encoding.UTF8.GetBytes("...prefix junk flag{short-st");
        var second = Encoding.UTF8.GetBytes("atic} suffix junk...");

        Assert.False(inspector.Inspect(first));
        Assert.True(inspector.Inspect(second));
    }
}
