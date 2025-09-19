using AutoFixture.Xunit2;
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
    public void GenerateHash_ShouldReturnConsistentHash(string input)
    {
        // Act
        var hash1 = Codec.StrSHA256(input);
        var hash2 = Codec.StrSHA256(input);

        // Assert
        hash1.Should().Be(hash2);
        hash1.Should().NotBeNullOrEmpty();
        hash1.Length.Should().Be(64); // SHA256 produces 64 character hex string
    }

    [Theory]
    [InlineData("password123")]
    [InlineData("verysecurepassword")]
    [InlineData("")]
    public void HashPassword_ShouldReturnDifferentHashesForSameInput(string password)
    {
        // Act
        var hash1 = Codec.HashPassword(password);
        var hash2 = Codec.HashPassword(password);

        // Assert
        hash1.Should().NotBe(hash2); // Due to salt
        hash1.Should().NotBeNullOrEmpty();
        hash2.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("password123")]
    [InlineData("correcthorsebatterystaple")]
    public void VerifyPassword_ShouldReturnTrueForCorrectPassword(string password)
    {
        // Arrange
        var hash = Codec.HashPassword(password);

        // Act
        var isValid = Codec.VerifyPassword(password, hash);

        // Assert
        isValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("password123", "wrongpassword")]
    [InlineData("correct", "incorrect")]
    public void VerifyPassword_ShouldReturnFalseForIncorrectPassword(string correctPassword, string wrongPassword)
    {
        // Arrange
        var hash = Codec.HashPassword(correctPassword);

        // Act
        var isValid = Codec.VerifyPassword(wrongPassword, hash);

        // Assert
        isValid.Should().BeFalse();
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
        var originalBytes = System.Text.Encoding.UTF8.GetBytes(originalData);

        // Act
        var base64 = Codec.Base64.Encode(originalBytes);
        var decodedBytes = Codec.Base64.DecodeToBytes(base64);
        var decodedString = System.Text.Encoding.UTF8.GetString(decodedBytes);

        // Assert
        decodedString.Should().Be(originalData);
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

    [Theory]
    [GzctfAutoData]
    public void RandomBytes_ShouldGenerateDifferentValues(int length)
    {
        // Arrange
        length = Math.Abs(length % 100) + 1; // Ensure positive length

        // Act
        var bytes1 = Random.Shared.NextBytes(length);
        var bytes2 = Random.Shared.NextBytes(length);

        // Assert
        bytes1.Length.Should().Be(length);
        bytes2.Length.Should().Be(length);
        bytes1.Should().NotEqual(bytes2); // Very unlikely to be equal for random data
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
}

/// <summary>
/// Custom AutoData attribute for this test class
/// </summary>
public class GzctfAutoDataAttribute : AutoDataAttribute
{
    public GzctfAutoDataAttribute() : base(() => new AutoFixture.Fixture())
    {
    }
}