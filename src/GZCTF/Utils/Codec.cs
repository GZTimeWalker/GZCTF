using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace GZCTF.Utils;

public partial class Codec
{
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
                return Array.Empty<byte>();

            byte[] encoded;
            try
            {
                encoded = Encoding.GetEncoding(type).GetBytes(str);
            }
            catch
            {
                return Array.Empty<byte>();
            }

            Span<char> buffer = new char[encoded.Length * 4 / 3 + 8];
            if (Convert.TryToBase64Chars(encoded, buffer, out var charsWritten))
                return Encoding.GetEncoding(type).GetBytes(buffer.Slice(0, charsWritten).ToArray());
            else
                return Array.Empty<byte>();
        }

        public static byte[] DecodeToBytes(string? str)
        {
            if (str is null)
                return Array.Empty<byte>();

            Span<byte> buffer = new byte[str.Length * 3 / 4 + 8];

            if (Convert.TryFromBase64String(str, buffer, out int bytesWritten))
                return buffer.Slice(0, bytesWritten).ToArray();

            return Array.Empty<byte>();
        }
    }

    /// <summary>
    /// Leet
    /// </summary>
    public static class Leet
    {
        private readonly static Dictionary<char, string> CharMap = new()
        {
            ['A'] = "Aa4",
            ['B'] = "Bb68",
            ['C'] = "Cc",
            ['D'] = "Dd",
            ['E'] = "Ee3",
            ['F'] = "Ff1",
            ['G'] = "Gg69",
            ['H'] = "Hh",
            ['I'] = "Ii1l",
            ['J'] = "Jj",
            ['K'] = "Kk",
            ['L'] = "Ll1I",
            ['M'] = "Mm",
            ['N'] = "Nn",
            ['O'] = "Oo0",
            ['P'] = "Pp",
            ['Q'] = "Qq9",
            ['R'] = "Rr",
            ['S'] = "Ss5",
            ['T'] = "Tt7",
            ['U'] = "Uu",
            ['V'] = "Vv",
            ['W'] = "Ww",
            ['X'] = "Xx",
            ['Y'] = "Yy",
            ['Z'] = "Zz2",
            ['0'] = "0oO",
            ['1'] = "1lI",
            ['2'] = "2zZ",
            ['3'] = "3eE",
            ['4'] = "4aA",
            ['5'] = "5Ss",
            ['6'] = "6Gb",
            ['7'] = "7T",
            ['8'] = "8bB",
            ['9'] = "9g"
        };

        public static double LeetEntropy(string flag)
        {
            double entropy = 0;
            var doLeet = false;
            foreach (var c in flag)
            {
                if (c == '{' || c == ']')
                    doLeet = true;
                else if (doLeet && (c == '}' || c == '['))
                    doLeet = false;
                else if (doLeet && CharMap.TryGetValue(char.ToUpperInvariant(c), out var table) && table is not null)
                    entropy += Math.Log(table.Length, 2);
            }
            return entropy;
        }

        public static string LeetFlag(string original)
        {
            StringBuilder sb = new(original.Length);
            Random random = new();

            var doLeet = false;
            // note: only leet 'X' in flag{XXXX_XXX_[TEAM_HASH]_XXX}
            foreach (var c in original)
            {
                if (c == '{' || c == ']')
                    doLeet = true;
                else if (doLeet && (c == '}' || c == '['))
                    doLeet = false;
                else if (doLeet && CharMap.TryGetValue(char.ToUpperInvariant(c), out var table) && table is not null)
                {
                    var nc = table[random.Next(table.Length)];
                    sb.Append(nc);
                    continue;
                }

                sb.Append(c == ' ' ? '_' : c); // replace blank to underline
            }

            return sb.ToString();
        }
    }

    [GeneratedRegex("^(?=.*\\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[!@#$%^&*()\\-_=+]).{8,}$")]
    private static partial Regex PasswordRegex();

    /// <summary>
    /// 生成随机密码
    /// </summary>
    /// <param name="length">密码长度</param>
    /// <returns></returns>
    public static string RandomPassword(int length)
    {
        var random = new Random();
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()-_=+";

        string pwd;
        do
        {
            pwd = new string(Enumerable.Repeat(chars, length < 8 ? 8 : length)
                    .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        while (!PasswordRegex().IsMatch(pwd));

        return pwd;
    }

    /// <summary>
    /// 转换为对应进制
    /// </summary>
    /// <param name="source">源数据</param>
    /// <param name="tobase">进制支持2,8,10,16</param>
    /// <returns></returns>
    public static List<string> ToBase(List<int> source, int tobase)
        => new(source.ConvertAll((a) => Convert.ToString(a, tobase)));

    /// <summary>
    /// 字节数组转换为16进制字符串
    /// </summary>
    /// <param name="bytes">原始字节数组</param>
    /// <param name="useLower">是否使用小写</param>
    /// <returns></returns>
    public static string BytesToHex(byte[] bytes, bool useLower = true)
    {
        var output = BitConverter.ToString(bytes).Replace("-", "");
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
        {
            res[i] = (byte)(data[i] ^ xor[i % xor.Length]);
        }
        return res;
    }

    /// <summary>
    /// 将文件打包为 zip 文件
    /// </summary>
    /// <param name="files">文件列表</param>
    /// <param name="basepath">根目录</param>
    /// <param name="zipName">压缩包根目录</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public async static Task<Stream> ZipFilesAsync(IEnumerable<LocalFile> files, string basepath, string zipName, CancellationToken token = default)
    {
        var size = files.Select(f => f.FileSize).Sum();

        Stream tmp = size <= 64 * 1024 * 1024 ? new MemoryStream() :
            File.Create(Path.GetTempFileName(), 4096, FileOptions.DeleteOnClose);

        using var zip = new ZipArchive(tmp, ZipArchiveMode.Create, true);

        foreach (var file in files)
        {
            var entry = zip.CreateEntry(Path.Combine(zipName, file.Name), CompressionLevel.Optimal);
            await using var entryStream = entry.Open();
            await using var fileStream = File.OpenRead(Path.Combine(basepath, file.Location, file.Hash));
            await fileStream.CopyToAsync(entryStream, token);
        }

        await tmp.FlushAsync(token);
        return tmp;
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
    public static List<int> ASCII(this string str)
    {
        var buff = Encoding.ASCII.GetBytes(str);
        List<int> res = new();
        foreach (var item in buff)
            res.Add(item);
        return res;
    }

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
    public static string StrMD5(this string str, bool useBase64 = false)
    {
        var output = MD5.HashData(Encoding.Default.GetBytes(str));
        if (useBase64)
            return Convert.ToBase64String(output);
        return BitConverter.ToString(output).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// 获取SHA256哈希摘要
    /// </summary>
    /// <param name="str">原始字符串</param>
    /// <param name="useBase64">是否使用Base64编码</param>
    /// <returns></returns>
    public static string StrSHA256(this string str, bool useBase64 = false)
    {
        var output = SHA256.HashData(Encoding.Default.GetBytes(str));
        if (useBase64)
            return Convert.ToBase64String(output);
        return BitConverter.ToString(output).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// 获取MD5哈希字节摘要
    /// </summary>
    /// <param name="str">原始字符串</param>
    /// <returns></returns>
    public static byte[] BytesMD5(this string str)
        => MD5.HashData(Encoding.Default.GetBytes(str));

    /// <summary>
    /// 获取SHA256哈希字节摘要
    /// </summary>
    /// <param name="str">原始字符串</param>
    /// <returns></returns>
    public static byte[] BytesSHA256(this string str)
        => SHA256.HashData(Encoding.Default.GetBytes(str));

}
