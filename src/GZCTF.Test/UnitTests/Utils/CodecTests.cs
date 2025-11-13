using GZCTF.Utils;
using Xunit;

namespace GZCTF.Test.UnitTests.Utils;

/// <summary>
/// Tests for Codec utility functions
/// </summary>
public class CodecTests
{
    [Fact]
    public void RandomPassword_GeneratesValidPassword()
    {
        // Act
        var password = Codec.RandomPassword(12);

        // Assert
        Assert.NotNull(password);
        Assert.True(password.Length >= 8); // Minimum length is 8
        Assert.Matches(@"(?=.*\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[!@#$%^&*()\-_=+])", password);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(32)]
    public void RandomPassword_RespectsMinimumLength(int length)
    {
        // Act
        var password = Codec.RandomPassword(length);

        // Assert
        Assert.True(password.Length >= 8);
    }

    [Theory]
    [InlineData(new byte[] { 0x12, 0x34, 0xAB, 0xCD }, true, "1234abcd")]
    [InlineData(new byte[] { 0x12, 0x34, 0xAB, 0xCD }, false, "1234ABCD")]
    [InlineData(new byte[] { 0xFF, 0x00, 0x12 }, true, "ff0012")]
    public void BytesToHex_ConvertsCorrectly(byte[] bytes, bool useLower, string expected)
    {
        // Act
        var result = Codec.BytesToHex(bytes, useLower);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FileHashRegex_MatchesValidHash()
    {
        // Arrange
        var validHash = "a".PadRight(64, 'b');

        // Act
        var matches = Codec.FileHashRegex().IsMatch(validHash);

        // Assert
        Assert.True(matches);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("not-a-hash")]
    [InlineData("")]
    public void FileHashRegex_RejectsInvalidHash(string invalidHash)
    {
        // Act
        var matches = Codec.FileHashRegex().IsMatch(invalidHash);

        // Assert
        Assert.False(matches);
    }

    [Theory]
    [InlineData("flag{test}", false, true)] // Should have entropy
    [InlineData("flag{ABC123}", false, true)]
    [InlineData("noflag", false, false)] // No entropy without braces
    public void LeetEntropy_CalculatesCorrectly(string flag, bool expectedZero, bool expectedPositive)
    {
        // Act
        var entropy = new GZCTF.Utils.DynamicFlagGenerator(flag).CalculateEntropy();

        // Assert
        if (expectedZero)
            Assert.Equal(0, entropy);
        else if (expectedPositive)
            Assert.True(entropy > 0);
    }

    [Theory]
    [InlineData(new[] { 5, 10, 15 }, 2, new[] { "101", "1010", "1111" })]
    [InlineData(new[] { 8, 16 }, 16, new[] { "8", "10" })]
    public void ToBase_ConvertsCorrectly(int[] source, int toBase, string[] expected)
    {
        // Act
        var result = Codec.ToBase([.. source], toBase);

        // Assert
        Assert.Equal(expected, result);
    }
}
