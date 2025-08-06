using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace GZCTF.Utils;

/// <summary>
/// Signature algorithms
/// </summary>
public enum SignAlgorithm
{
    Ed25519,
    Ed25519Ctx,
    Ed448,
    SHA512WithRSA
}

public static class CryptoUtils
{
    /// <summary>
    /// Generate a signature for the given data using the specified private key and algorithm
    /// </summary>
    /// <param name="data">The data to sign</param>
    /// <param name="privateKey">The private key to use for signing</param>
    /// <param name="signAlgorithm">The signature algorithm to use</param>
    /// <param name="useUrlSafeBase64">If true, returns the signature as a URL-safe Base64 string</param>
    /// <returns>The generated signature as a Base64-encoded string</returns>
    public static string GenerateSignature(
        string data,
        AsymmetricKeyParameter privateKey,
        SignAlgorithm signAlgorithm,
        bool useUrlSafeBase64 = false
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(data);
        ArgumentNullException.ThrowIfNull(privateKey);

        var byteData = Encoding.UTF8.GetBytes(data);
        ISigner signer = SignerUtilities.GetSigner(signAlgorithm.ToString());
        signer.Init(true, privateKey);
        signer.BlockUpdate(byteData, 0, data.Length);
        var result = signer.GenerateSignature();
        return useUrlSafeBase64 ? Base64UrlEncoder.Encode(result) : Convert.ToBase64String(result);
    }

    /// <summary>
    /// Verify a signature for the given data using the specified public key and algorithm
    /// </summary>
    /// <param name="data">The data to verify</param>
    /// <param name="sign">The signature to verify, as a Base64-encoded string</param>
    /// <param name="publicKey">The public key to use for verification</param>
    /// <param name="signAlgorithm">The signature algorithm to use</param>
    /// <param name="useUrlSafeBase64">If true, expects the signature to be in URL-safe Base64 format</param>
    /// <returns>If the signature is valid, returns true; otherwise, returns false</returns>
    public static bool VerifySignature(
        string data,
        string sign,
        AsymmetricKeyParameter publicKey,
        SignAlgorithm signAlgorithm,
        bool useUrlSafeBase64 = false
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(data);
        ArgumentException.ThrowIfNullOrEmpty(sign);
        ArgumentNullException.ThrowIfNull(publicKey);

        var signBytes = useUrlSafeBase64 ? Base64UrlEncoder.DecodeBytes(sign) : Convert.FromBase64String(sign);
        var plainBytes = Encoding.UTF8.GetBytes(data);
        ISigner? verifier = SignerUtilities.GetSigner(signAlgorithm.ToString());
        verifier.Init(false, publicKey);
        verifier.BlockUpdate(plainBytes, 0, plainBytes.Length);

        return verifier.VerifySignature(signBytes);
    }

    /// <summary>
    /// Generate a new X25519 key pair
    /// </summary>
    public static AsymmetricCipherKeyPair GenerateX25519KeyPair()
    {
        SecureRandom sr = new();
        var keyGen = new X25519KeyPairGenerator();
        keyGen.Init(new X25519KeyGenerationParameters(sr));
        return keyGen.GenerateKeyPair();
    }

    /// <summary>
    /// Generate a new Ed25519 key pair
    /// </summary>
    public static AsymmetricCipherKeyPair GenerateEd25519KeyPair()
    {
        SecureRandom sr = new();
        var keyGen = new Ed25519KeyPairGenerator();
        keyGen.Init(new Ed25519KeyGenerationParameters(sr));
        return keyGen.GenerateKeyPair();
    }

    /// <summary>
    /// Encrypt data using the public key
    /// </summary>
    /// <param name="data">The data to encrypt</param>
    /// <param name="publicKey">The public key to use for encryption</param>
    /// <returns>Encrypted data as a byte array</returns>
    public static byte[] EncryptData(byte[] data, X25519PublicKeyParameters publicKey)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(publicKey);

        var sr = new SecureRandom();
        var keyGen = new X25519KeyPairGenerator();
        keyGen.Init(new X25519KeyGenerationParameters(sr));
        AsymmetricCipherKeyPair? ephemeralKeyPair = keyGen.GenerateKeyPair();

        var agreement = new X25519Agreement();
        agreement.Init(ephemeralKeyPair.Private);

        Span<byte> sharedSecret = stackalloc byte[32];
        agreement.CalculateAgreement(publicKey, sharedSecret);

        Span<byte> aesKey = stackalloc byte[32];
        SHA256.HashData(sharedSecret, aesKey);

        var cipher = new GcmBlockCipher(new AesEngine());
        Span<byte> nonce = stackalloc byte[12];
        sr.NextBytes(nonce);

        var parameters = new AeadParameters(new KeyParameter(aesKey.ToArray()), 128, nonce.ToArray());
        cipher.Init(true, parameters);

        var cipherText = new byte[cipher.GetOutputSize(data.Length)];
        var len = cipher.ProcessBytes(data, 0, data.Length, cipherText, 0);
        cipher.DoFinal(cipherText, len);

        var ephemeralPublicKeyBytes = ((X25519PublicKeyParameters)ephemeralKeyPair.Public).GetEncoded();
        var result = new byte[32 + 12 + cipherText.Length];

        Span<byte> resultSpan = result.AsSpan();
        ephemeralPublicKeyBytes.CopyTo(resultSpan);
        nonce.CopyTo(resultSpan[32..]);
        cipherText.CopyTo(resultSpan[44..]);

        return result;
    }

    /// <summary>
    /// Decrypt data using the private key
    /// </summary>
    /// <param name="data">The encrypted data to decrypt</param>
    /// <param name="privateKey">The private key to use for decryption</param>
    /// <returns>Decrypted data as a byte array</returns>
    public static byte[] DecryptData(byte[] data, X25519PrivateKeyParameters privateKey)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(privateKey);

        if (data.Length < 44)
            throw new ArgumentException("Invalid encrypted data length");

        Span<byte> dataSpan = data.AsSpan();
        Span<byte> ephemeralPublicKeyBytes = dataSpan[..32];
        Span<byte> nonce = dataSpan[32..44];
        Span<byte> cipherText = dataSpan[44..];

        var ephemeralPublicKey = new X25519PublicKeyParameters(ephemeralPublicKeyBytes);

        var agreement = new X25519Agreement();
        agreement.Init(privateKey);

        Span<byte> sharedSecret = stackalloc byte[32];
        agreement.CalculateAgreement(ephemeralPublicKey, sharedSecret);

        Span<byte> aesKey = stackalloc byte[32];
        SHA256.HashData(sharedSecret, aesKey);

        var cipher = new GcmBlockCipher(new AesEngine());
        var parameters = new AeadParameters(new KeyParameter(aesKey.ToArray()), 128, nonce.ToArray());
        cipher.Init(false, parameters);

        var plainText = new byte[cipher.GetOutputSize(cipherText.Length)];
        var len = cipher.ProcessBytes(cipherText.ToArray(), 0, cipherText.Length, plainText, 0);
        len += cipher.DoFinal(plainText, len);

        return len == plainText.Length ? plainText : plainText[..len];
    }
}
