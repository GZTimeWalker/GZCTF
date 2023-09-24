namespace GZCTF.Utils;

enum DirType : byte
{
    Logs,
    Uploads,
    Capture
}

static class FilePath
{
    const string Base = "files";

    internal static string Logs => GetDir(DirType.Logs);
    internal static string Uploads => GetDir(DirType.Uploads);
    internal static string Capture => GetDir(DirType.Capture);

    internal static void EnsureDirs()
    {
        foreach (DirType type in Enum.GetValues<DirType>())
        {
            var path = Path.Combine(Base, type.ToString().ToLower());
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
    }

    /// <summary>
    /// 获取文件夹路径
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    static string GetDir(DirType type) => Path.Combine(Base, type.ToString().ToLower());

    /// <summary>
    /// 获取文件夹内容
    /// </summary>
    /// <param name="dir">文件夹</param>
    /// <param name="totSize">总大小</param>
    /// <returns></returns>
    internal static List<FileRecord> GetFileRecords(string dir, out long totSize)
    {
        totSize = 0;
        var records = new List<FileRecord>();

        foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly))
        {
            var info = new FileInfo(file);

            records.Add(FileRecord.FromFileInfo(info));

            totSize += info.Length;
        }

        return records;
    }
}