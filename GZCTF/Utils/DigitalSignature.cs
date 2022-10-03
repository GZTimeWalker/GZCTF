using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.Encoders;
using System.Text;

namespace CTFServer.Utils;

/// <summary>
/// 签名算法
/// </summary>
public enum SignAlgorithm
{
    Ed25519,
    Ed25519ph,
    Ed25519ctx,
    Ed448,
    Ed448ph,
    MD5withRSA,
    SHA256withRSA,
    SHA384withRSA,
    SHA512withRSA,
    SHA256withDSA,
    SHA384withDSA,
    SHA512withDSA,
    SHA256withECDSA,
    SHA384withECDSA,
    SHA512withECDSA,
}

public static class DigitalSignature
{
    public static string GenerateSignature(string data, AsymmetricKeyParameter privateKey, SignAlgorithm signAlgorithm)
    {
        if (string.IsNullOrEmpty(data))
            throw new ArgumentNullException(nameof(data));

        if (privateKey is null)
            throw new ArgumentNullException(nameof(privateKey));

        var byteData = Encoding.UTF8.GetBytes(data);
        var normalSig = SignerUtilities.GetSigner(signAlgorithm.ToString());
        normalSig.Init(true, privateKey);
        normalSig.BlockUpdate(byteData, 0, data.Length);
        var normalResult = normalSig.GenerateSignature();
        return Base64.ToBase64String(normalResult);
    }

    public static bool VerifySignature(string data, string sign, AsymmetricKeyParameter publicKey, SignAlgorithm signAlgorithm)
    {
        if (string.IsNullOrEmpty(data))
            throw new ArgumentNullException(nameof(data));

        if (string.IsNullOrEmpty(sign))
            throw new ArgumentNullException(nameof(sign));

        if (publicKey == null)
            throw new ArgumentNullException(nameof(publicKey));

        var signBytes = Base64.Decode(sign);
        var plainBytes = Encoding.UTF8.GetBytes(data);
        var verifier = SignerUtilities.GetSigner(signAlgorithm.ToString());
        verifier.Init(false, publicKey);
        verifier.BlockUpdate(plainBytes, 0, plainBytes.Length);

        return verifier.VerifySignature(signBytes);
    }
}