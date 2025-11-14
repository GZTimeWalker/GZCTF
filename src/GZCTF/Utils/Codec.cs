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
    /// Generate a random strong password
    /// </summary>
    /// <param name="length">The length of the password</param>
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
    /// Convert integers to target base strings
    /// </summary>
    /// <param name="source">Source integers</param>
    /// <param name="toBase">Target base</param>
    /// <returns></returns>
    public static List<string> ToBase(List<int> source, int toBase) =>
        [.. source.ConvertAll(a => Convert.ToString(a, toBase))];

    /// <summary>
    /// Convert bytes to hex string
    /// </summary>
    /// <param name="bytes">The byte array</param>
    /// <param name="useLower">Whether to use lower case</param>
    /// <returns></returns>
    public static string BytesToHex(byte[] bytes, bool useLower = true)
    {
        var output = Convert.ToHexString(bytes);
        return useLower ? output.ToLowerInvariant() : output.ToUpperInvariant();
    }

    /// <summary>
    /// Xor bytes
    /// </summary>
    /// <param name="data">Original data</param>
    /// <param name="xor">Xor data</param>
    /// <returns></returns>
    public static byte[] Xor(byte[] data, byte[] xor)
    {
        // if no xor data, return original data
        if (xor.Length == 0)
            return data;

        var res = new byte[data.Length];
        for (var i = 0; i < data.Length; ++i)
            res[i] = (byte)(data[i] ^ xor[i % xor.Length]);
        return res;
    }

    /// <summary>
    /// Base64 encode/decode
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

    /// <param name="str"></param>
    extension(string str)
    {
        /// <summary>
        /// Convert to a valid RFC 1123 string
        /// </summary>
        /// <param name="leading">Leading string if the result starts with a digit</param>
        /// <returns></returns>
        public string ToValidRFC1123String(string leading = "name")
        {
            var ret = RFC1123ReplacePattern().Replace(str, "-").Trim('-').ToLowerInvariant();
            if (ret.Length > 0 && char.IsDigit(ret[0]))
                return $"{leading}-{ret}";
            return ret;
        }

        /// <summary>
        /// Get ASCII bytes
        /// </summary>
        /// <returns></returns>
        public byte[] Ascii() => Encoding.ASCII.GetBytes(str);

        /// <summary>
        /// Reverse string
        /// </summary>
        /// <returns></returns>
        public string Reverse()
        {
            var charArray = str.ToCharArray();
            Array.Reverse((Array)charArray);
            return new string(charArray);
        }

        /// <summary>
        /// Get MD5 hash string
        /// </summary>
        /// <param name="useBase64">Whether to use Base64 encoding</param>
        /// <returns></returns>
        public string ToMD5String(bool useBase64 = false)
        {
            var output = MD5.HashData(str.ToUTF8Bytes());
            return useBase64
                ? Convert.ToBase64String(output)
                : Convert.ToHexStringLower(output);
        }

        /// <summary>
        /// Get SHA256 hash string
        /// </summary>
        /// <param name="useBase64">Whether to use Base64 encoding</param>
        /// <returns></returns>
        public string ToSHA256String(bool useBase64 = false)
        {
            var output = SHA256.HashData(str.ToUTF8Bytes());
            return useBase64
                ? Convert.ToBase64String(output)
                : Convert.ToHexStringLower(output);
        }

        /// <summary>
        /// Get UTF8 bytes
        /// </summary>
        /// <returns></returns>
        public byte[] ToUTF8Bytes() => Encoding.UTF8.GetBytes(str);
    }


    extension(byte[] hash)
    {
        /// <summary>
        /// Get leading zeros in bits
        /// </summary>
        public int LeadingZeros()
        {
            var leadingZeros = 0;
            foreach (var t in hash)
            {
                if (t == 0)
                    leadingZeros += 8;
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
}
