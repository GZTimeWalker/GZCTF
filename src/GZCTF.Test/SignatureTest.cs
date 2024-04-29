using System.Text;
using GZCTF.Utils;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.Encoders;
using Xunit;
using Xunit.Abstractions;

namespace GZCTF.Test;

public class SignatureTest(ITestOutputHelper output)
{
    [Fact]
    public void Ed25519Test()
    {
        var sAlgorithm = SignAlgorithm.Ed25519;
        var s = "Hello " + sAlgorithm;
        output.WriteLine(s);
        SecureRandom sr = new();
        Ed25519KeyPairGenerator kpg = new();
        kpg.Init(new Ed25519KeyGenerationParameters(sr));

        AsymmetricCipherKeyPair kp = kpg.GenerateKeyPair();
        var privateKey = (Ed25519PrivateKeyParameters)kp.Private;
        var publicKey = (Ed25519PublicKeyParameters)kp.Public;

        output.WriteLine("私钥：");
        output.WriteLine(Base64.ToBase64String(privateKey.GetEncoded()));
        output.WriteLine("公钥：");
        output.WriteLine(Base64.ToBase64String(publicKey.GetEncoded()));

        var sign = DigitalSignature.GenerateSignature(s, privateKey, sAlgorithm);
        output.WriteLine($"签名：\n{sign}");

        var verified = DigitalSignature.VerifySignature(s, sign, publicKey, sAlgorithm);

        output.WriteLine("验证结果：");
        output.WriteLine(verified ? "Signature verified" : "Signature not verified");
        Assert.True(verified);
    }

    [Fact]
    public void Ed25519WithXorTest()
    {
        var sAlgorithm = SignAlgorithm.Ed25519;
        var s = "Hello " + sAlgorithm;
        output.WriteLine(s);

        Ed25519PrivateKeyParameters privateKey =
            new(Codec.Base64.DecodeToBytes("Qu4G33WZ7DYTUEdlf3P5amVg7f8yXcOmFcG0EJvfQEY="), 0);
        Ed25519PublicKeyParameters publicKey =
            new(Codec.Base64.DecodeToBytes("t4zduq4LGA1hEYhkCVK19xRACXuDxm/W72v4PBN1EXY="), 0);

        output.WriteLine("私钥：");
        output.WriteLine(Base64.ToBase64String(privateKey.GetEncoded()));

        var xorkey = Encoding.UTF8.GetBytes("helloworld");
        var encodedkey = Base64.ToBase64String(Codec.Xor(privateKey.GetEncoded(), xorkey));

        output.WriteLine("编码私钥：");
        output.WriteLine(encodedkey);

        var bytekey = Codec.Xor(Codec.Base64.DecodeToBytes(encodedkey), xorkey);
        privateKey = new(bytekey, 0);

        output.WriteLine("解码私钥：");
        output.WriteLine(Base64.ToBase64String(privateKey.GetEncoded()));
        output.WriteLine("公钥：");
        output.WriteLine(Base64.ToBase64String(publicKey.GetEncoded()));

        var sign = DigitalSignature.GenerateSignature(s, privateKey, sAlgorithm);
        output.WriteLine($"签名：\n{sign}");

        var verified = DigitalSignature.VerifySignature(s, sign, publicKey, sAlgorithm);

        output.WriteLine("验证结果：");
        output.WriteLine(verified ? "Signature verified" : "Signature not verified");
        Assert.True(verified);
    }

    [Fact]
    public void Ed25519CtxTest()
    {
        const SignAlgorithm sAlgorithm = SignAlgorithm.Ed25519Ctx;
        var s = "Hello " + sAlgorithm;
        output.WriteLine(s);
        SecureRandom sr = new();
        Ed25519KeyPairGenerator kpg = new();
        kpg.Init(new Ed25519KeyGenerationParameters(sr));

        AsymmetricCipherKeyPair kp = kpg.GenerateKeyPair();
        var privateKey = (Ed25519PrivateKeyParameters)kp.Private;
        var publicKey = (Ed25519PublicKeyParameters)kp.Public;

        output.WriteLine("私钥：");
        output.WriteLine(Base64.ToBase64String(privateKey.GetEncoded()));
        output.WriteLine("公钥：");
        output.WriteLine(Base64.ToBase64String(publicKey.GetEncoded()));

        var sign = DigitalSignature.GenerateSignature(s, privateKey, sAlgorithm);
        output.WriteLine($"签名：\n{sign}");

        var verified = DigitalSignature.VerifySignature(s, sign, publicKey, sAlgorithm);

        output.WriteLine("验证结果：");
        output.WriteLine(verified ? "Signature verified" : "Signature not verified");
        Assert.True(verified);
    }

    [Fact]
    public void Ed448Test()
    {
        var sAlgorithm = SignAlgorithm.Ed448;
        var s = "Hello " + sAlgorithm;
        output.WriteLine(s);
        SecureRandom sr = new();
        Ed448KeyPairGenerator kpg = new();
        kpg.Init(new Ed448KeyGenerationParameters(sr));

        AsymmetricCipherKeyPair kp = kpg.GenerateKeyPair();
        var privateKey = (Ed448PrivateKeyParameters)kp.Private;
        var publicKey = (Ed448PublicKeyParameters)kp.Public;

        output.WriteLine("私钥：");
        output.WriteLine(Base64.ToBase64String(privateKey.GetEncoded()));
        output.WriteLine("公钥：");
        output.WriteLine(Base64.ToBase64String(publicKey.GetEncoded()));

        var sign = DigitalSignature.GenerateSignature(s, privateKey, sAlgorithm);
        output.WriteLine($"签名：\n{sign}");

        var verified = DigitalSignature.VerifySignature(s, sign, publicKey, sAlgorithm);

        output.WriteLine("验证结果：");
        output.WriteLine(verified ? "Signature verified" : "Signature not verified");
        Assert.True(verified);
    }

    [Fact]
    public void SHA512WithRSATest()
    {
        const SignAlgorithm sAlgorithm = SignAlgorithm.SHA512WithRSA;
        var s = "Hello " + sAlgorithm;
        output.WriteLine(s);
        SecureRandom sr = new();
        RsaKeyPairGenerator kpg = new();
        kpg.Init(new KeyGenerationParameters(sr, 2048));

        AsymmetricCipherKeyPair kp = kpg.GenerateKeyPair();
        var privateKey = (RsaKeyParameters)kp.Private;
        var publicKey = (RsaKeyParameters)kp.Public;

        output.WriteLine("私钥：");
        output.WriteLine(privateKey.Exponent.ToString());
        output.WriteLine("公钥：");
        output.WriteLine(publicKey.Exponent.ToString());

        var sign = DigitalSignature.GenerateSignature(s, privateKey, sAlgorithm);
        output.WriteLine($"签名：\n{sign}");

        var verified = DigitalSignature.VerifySignature(s, sign, publicKey, sAlgorithm);

        output.WriteLine("验证结果：");
        output.WriteLine(verified ? "Signature verified" : "Signature not verified");
        Assert.True(verified);
    }
}
