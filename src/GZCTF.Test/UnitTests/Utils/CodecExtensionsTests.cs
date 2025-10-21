using GZCTF.Utils;
using Xunit;

namespace GZCTF.Test.UnitTests.Utils;

/// <summary>
/// Tests for Codec extension methods
/// </summary>
public class CodecExtensionsTests
{
    [Theory]
    [InlineData("hello-world", "hello-world")]
    [InlineData("Hello World!", "hello-world")]
    [InlineData("Test_123", "test-123")]
    [InlineData("Multiple   Spaces", "multiple-spaces")]
    [InlineData("123-start", "name-123-start")] // Starts with digit
    [InlineData("---trimmed---", "trimmed")] // Leading/trailing dashes
    public void ToValidRFC1123String_ConvertsCorrectly(string input, string expected)
    {
        // Act
        var result = input.ToValidRFC1123String();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("hello", "olleh")]
    [InlineData("12345", "54321")]
    [InlineData("a", "a")]
    [InlineData("", "")]
    public void Reverse_ReversesString(string input, string expected)
    {
        // Act
        var result = input.Reverse();

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Ascii_ConvertsToBytes()
    {
        // Arrange
        var input = "Hello";

        // Act
        var bytes = input.Ascii();

        // Assert
        Assert.NotNull(bytes);
        Assert.Equal(5, bytes.Length);
        Assert.Equal(72, bytes[0]); // 'H'
        Assert.Equal(101, bytes[1]); // 'e'
    }

    [Theory]
    [InlineData("hello", "5d41402abc4b2a76b9719d911017c592")]
    [InlineData("", "d41d8cd98f00b204e9800998ecf8427e")]
    public void ToMD5String_CalculatesCorrectHash(string input, string expectedHash)
    {
        // Act
        var hash = input.ToMD5String(false);

        // Assert
        Assert.Equal(expectedHash, hash);
    }

    [Fact]
    public void ToMD5String_WithBase64_ReturnsBase64()
    {
        // Arrange
        var input = "hello";

        // Act
        var hash = input.ToMD5String(true);

        // Assert
        Assert.NotNull(hash);
        Assert.Matches(@"^[A-Za-z0-9+/]+=*$", hash); // Base64 pattern
    }

    [Theory]
    [InlineData(new byte[] { 0x00 }, 8)]
    [InlineData(new byte[] { 0x00, 0x00 }, 16)]
    [InlineData(new byte[] { 0x80 }, 0)]
    [InlineData(new byte[] { 0x40 }, 1)]
    [InlineData(new byte[] { 0x20 }, 2)]
    [InlineData(new byte[] { 0x01 }, 7)]
    public void LeadingZeros_CountsCorrectly(byte[] hash, int expected)
    {
        // Act
        var zeros = hash.LeadingZeros();

        // Assert
        Assert.Equal(expected, zeros);
    }

    [Fact]
    public void ToUTF8Bytes_ConvertsCorrectly()
    {
        // Arrange
        var input = "Hello";

        // Act
        var bytes = input.ToUTF8Bytes();

        // Assert
        Assert.NotNull(bytes);
        Assert.Equal(5, bytes.Length);
    }

    [Fact]
    public void ToUTF8Bytes_HandlesUnicode()
    {
        // Arrange
        var input = "你好"; // Chinese characters

        // Act
        var bytes = input.ToUTF8Bytes();

        // Assert
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > input.Length); // UTF-8 multibyte
    }
}
