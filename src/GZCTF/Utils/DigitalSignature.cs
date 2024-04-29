using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.Encoders;

namespace GZCTF.Utils;

/// <summary>
/// 签名算法
/// </summary>
public enum SignAlgorithm
{
    Ed25519,
    Ed25519Ctx,
    Ed448,
    SHA512WithRSA
}

public static class DigitalSignature
{
    public static string GenerateSignature(string data, AsymmetricKeyParameter privateKey, SignAlgorithm signAlgorithm)
    {
        ArgumentException.ThrowIfNullOrEmpty(data);
        ArgumentNullException.ThrowIfNull(privateKey);

        var byteData = Encoding.UTF8.GetBytes(data);
        ISigner? normalSig = SignerUtilities.GetSigner(signAlgorithm.ToString());
        normalSig.Init(true, privateKey);
        normalSig.BlockUpdate(byteData, 0, data.Length);
        var normalResult = normalSig.GenerateSignature();
        return Base64.ToBase64String(normalResult);
    }

    public static bool VerifySignature(string data, string sign, AsymmetricKeyParameter publicKey,
        SignAlgorithm signAlgorithm)
    {
        ArgumentException.ThrowIfNullOrEmpty(data);
        ArgumentException.ThrowIfNullOrEmpty(sign);
        ArgumentNullException.ThrowIfNull(publicKey);

        var signBytes = Base64.Decode(sign);
        var plainBytes = Encoding.UTF8.GetBytes(data);
        ISigner? verifier = SignerUtilities.GetSigner(signAlgorithm.ToString());
        verifier.Init(false, publicKey);
        verifier.BlockUpdate(plainBytes, 0, plainBytes.Length);

        return verifier.VerifySignature(signBytes);
    }
}
