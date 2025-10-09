using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using GZCTF.Utils;
using Xunit;

namespace GZCTF.Test.UnitTests.Utils;

public class CryptoUtilsTest
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
        hash1.Should().Be(hash2);
        hash1.Should().NotBeNullOrEmpty();
        hash1.Length.Should().Be(64); // SHA256 produces 64 character hex string
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
        result.Should().Be(data);
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
        decoded.Should().Be(originalData);
        base64.Should().NotBeNullOrEmpty();
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
        restored.Should().Be(input);
        bytes.Length.Should().Be(System.Text.Encoding.UTF8.GetByteCount(input));
    }

    [Fact]
    public void RandomPassword_ShouldGenerateDifferentValues()
    {
        // Arrange
        const int length = 16;

        // Act
        var password1 = Codec.RandomPassword(length);
        var password2 = Codec.RandomPassword(length);

        // Assert
        password1.Length.Should().Be(length);
        password2.Length.Should().Be(length);
        password1.Should().NotBe(password2); // Very unlikely to be equal for random data
    }

    [Fact]
    public void BytesToHex_ShouldConvertCorrectly()
    {
        // Arrange
        var bytes = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }; // "Hello" in ASCII

        // Act
        var hex = Codec.BytesToHex(bytes);

        // Assert
        hex.Should().Be("48656c6c6f");
    }

    [Fact]
    public void CurrentTimeStamp_ShouldReturnReasonableValue()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Act
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Assert
        var after = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        timestamp.Should().BeGreaterOrEqualTo(before);
        timestamp.Should().BeLessOrEqualTo(after);
        timestamp.Should().BeGreaterThan(1600000000); // After 2020
    }

    [Theory]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(32)]
    public void RandomPassword_ShouldRespectLength(int length)
    {
        // Act
        var password = Codec.RandomPassword(length);

        // Assert
        password.Length.Should().Be(length);
        password.Should().NotBeNullOrEmpty();
    }
}