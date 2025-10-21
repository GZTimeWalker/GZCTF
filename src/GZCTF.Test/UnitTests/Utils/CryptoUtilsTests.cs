using GZCTF.Utils;
using Xunit;

namespace GZCTF.Test.UnitTests.Utils;

/// <summary>
/// Tests for cryptographic utilities
/// </summary>
public class CryptoUtilsTests
{
    [Theory]
    [InlineData("Hello World")]
    [InlineData("")]
    [InlineData("Special chars: !@#$%^&*()")]
    [InlineData("Unicode: 你好世界")]
    public void ToSHA256String_ShouldReturnConsistentHash(string input)
    {
        // Act
        var hash1 = input.ToSHA256String();
        var hash2 = input.ToSHA256String();

        // Assert
        Assert.Equal(hash1, hash2);
        Assert.NotEmpty(hash1);
        Assert.Equal(64, hash1.Length); // SHA256 produces 64 character hex string
    }

    [Theory]
    [InlineData("Hello World", "key123")]
    [InlineData("", "")]
    [InlineData("Data", "LongerKeyThanData")]
    public void Xor_ShouldBeReversible(string data, string key)
    {
        // Arrange
        var dataBytes = System.Text.Encoding.UTF8.GetBytes(data);
        var keyBytes = System.Text.Encoding.UTF8.GetBytes(key);

        // Act
        var encoded = Codec.Xor(dataBytes, keyBytes);
        var decoded = Codec.Xor(encoded, keyBytes);
        var result = System.Text.Encoding.UTF8.GetString(decoded);

        // Assert
        Assert.Equal(data, result);
    }

    [Fact]
    public void Base64_ShouldBeReversible()
    {
        // Arrange
        var originalData = "Hello, GZCTF Testing Framework!";

        // Act
        var base64 = Codec.Base64.Encode(originalData);
        var decoded = Codec.Base64.Decode(base64);

        // Assert
        Assert.Equal(originalData, decoded);
        Assert.NotEmpty(base64);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("Hello World")]
    [InlineData("Long string with many characters for testing purposes")]
    public void ToUTF8Bytes_ShouldConvertCorrectly(string input)
    {
        // Act
        var bytes = input.ToUTF8Bytes();
        var restored = System.Text.Encoding.UTF8.GetString(bytes);

        // Assert
        Assert.Equal(input, restored);
        Assert.Equal(System.Text.Encoding.UTF8.GetByteCount(input), bytes.Length);
    }
}
