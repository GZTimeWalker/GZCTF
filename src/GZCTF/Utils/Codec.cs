using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace GZCTF.Utils;

public static partial class Codec
{
    [GeneratedRegex("^[0-9a-fA-F]{64}$")]
    public static partial Regex FileHashRegex();

    [GeneratedRegex(@"^(?=.*\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[!@#$%^&*()\-_=+]).{8,}$")]
    private static partial Regex PasswordRegex();

    /// <summary>
    /// 生成随机密码
    /// </summary>
    /// <param name="length">密码长度</param>
    /// <returns></returns>
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public static string RandomPassword(int length)
    {
        var random = new Random();
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()-_=+";

        string pwd;
        do
        {
            pwd = new string(Enumerable.Repeat(chars, length < 8 ? 8 : length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        } while (!PasswordRegex().IsMatch(pwd));

        return pwd;
    }

    /// <summary>
    /// 转换为对应进制
    /// </summary>
    /// <param name="source">源数据</param>
    /// <param name="toBase">进制支持2,8,10,16</param>
    /// <returns></returns>
    public static List<string> ToBase(List<int> source, int toBase) =>
        [.. source.ConvertAll(a => Convert.ToString(a, toBase))];

    /// <summary>
    /// 字节数组转换为16进制字符串
    /// </summary>
    /// <param name="bytes">原始字节数组</param>
    /// <param name="useLower">是否使用小写</param>
    /// <returns></returns>
    public static string BytesToHex(byte[] bytes, bool useLower = true)
    {
        var output = Convert.ToHexString(bytes);
        return useLower ? output.ToLowerInvariant() : output.ToUpperInvariant();
    }

    /// <summary>
    /// 根据xor进行byte数异或
    /// </summary>
    /// <param name="data">原始数据</param>
    /// <param name="xor">xor密钥</param>
    /// <returns>异或结果</returns>
    public static byte[] Xor(byte[] data, byte[] xor)
    {
        var res = new byte[data.Length];
        for (var i = 0; i < data.Length; ++i)
            res[i] = (byte)(data[i] ^ xor[i % xor.Length]);
        return res;
    }

    /// <summary>
    /// Base64编解码
    /// </summary>
    public static class Base64
    {
        public static string Decode(string? str, string type = "utf-8")
        {
            if (str is null)
                return string.Empty;

            try
            {
                return Encoding.GetEncoding(type).GetString(Convert.FromBase64String(str));
            }
            catch
            {
                return string.Empty;
            }
        }

        public static string Encode(string? str, string type = "utf-8")
        {
            if (str is null)
                return string.Empty;

            try
            {
                return Convert.ToBase64String(Encoding.GetEncoding(type).GetBytes(str));
            }
            catch
            {
                return string.Empty;
            }
        }

        public static byte[] EncodeToBytes(string? str, string type = "utf-8")
        {
            if (str is null)
                return [];

            byte[] encoded;
            try
            {
                encoded = Encoding.GetEncoding(type).GetBytes(str);
            }
            catch
            {
                return [];
            }

            char[] buffer = new char[encoded.Length * 4 / 3 + 8];
            return Convert.TryToBase64Chars(encoded, buffer, out var charsWritten)
                ? Encoding.GetEncoding(type).GetBytes(buffer[..charsWritten])
                : [];
        }

        public static byte[] DecodeToBytes(string? str)
        {
            if (str is null)
                return [];

            Span<byte> buffer = stackalloc byte[str.Length * 3 / 4 + 8];

            return Convert.TryFromBase64String(str, buffer, out var bytesWritten)
                ? buffer[..bytesWritten].ToArray()
                : [];
        }
    }
}

public static partial class CodecExtensions
{
    [GeneratedRegex("[^a-zA-Z0-9]+")]
    private static partial Regex RFC1123ReplacePattern();

    /// <summary>
    /// 将字符串转换为符合 RFC1123 要求的字符串
    /// </summary>
    /// <param name="str"></param>
    /// <param name="leading">若开头为数字则添加的字符串</param>
    /// <returns></returns>
    public static string ToValidRFC1123String(this string str, string leading = "name")
    {
        var ret = RFC1123ReplacePattern().Replace(str, "-").Trim('-').ToLowerInvariant();
        if (ret.Length > 0 && char.IsDigit(ret[0]))
            return $"{leading}-{ret}";
        return ret;
    }

    /// <summary>
    /// 获取字符串ASCII数组
    /// </summary>
    /// <param name="str">原字符串</param>
    /// <returns></returns>
    public static byte[] Ascii(this string str) => Encoding.ASCII.GetBytes(str);

    /// <summary>
    /// 反转字符串
    /// </summary>
    /// <param name="s">原字符串</param>
    /// <returns></returns>
    public static string Reverse(this string s)
    {
        var charArray = s.ToCharArray();
        Array.Reverse(charArray);
        return new string(charArray);
    }

    /// <summary>
    /// 获取字符串MD5哈希摘要
    /// </summary>
    /// <param name="str">原始字符串</param>
    /// <param name="useBase64">是否使用Base64编码</param>
    /// <returns></returns>
    public static string ToMD5String(this string str, bool useBase64 = false)
    {
        var output = MD5.HashData(str.ToUTF8Bytes());
        return useBase64
            ? Convert.ToBase64String(output)
            : Convert.ToHexStringLower(output);
    }

    /// <summary>
    /// 获取SHA256哈希摘要
    /// </summary>
    /// <param name="str">原始字符串</param>
    /// <param name="useBase64">是否使用Base64编码</param>
    /// <returns></returns>
    public static string ToSHA256String(this string str, bool useBase64 = false)
    {
        var output = SHA256.HashData(str.ToUTF8Bytes());
        return useBase64
            ? Convert.ToBase64String(output)
            : Convert.ToHexStringLower(output);
    }


    /// <summary>
    /// 获取字符串 UTF-8 编码字节
    /// </summary>
    /// <param name="str">原始字符串</param>
    /// <returns></returns>
    public static byte[] ToUTF8Bytes(this string str) => Encoding.UTF8.GetBytes(str);

    /// <summary>
    /// Get leading zeros in bits
    /// </summary>
    public static int LeadingZeros(this byte[] hash)
    {
        var leadingZeros = 0;
        foreach (var t in hash)
        {
            if (t == 0)
            {
                leadingZeros += 8;
            }
            else
            {
                var b = t;
                while ((b & 0x80) == 0)
                {
                    b <<= 1;
                    leadingZeros++;
                }

                break;
            }
        }

        return leadingZeros;
    }
}
