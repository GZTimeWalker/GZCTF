namespace GZCTF.Utils;

internal enum DirType : byte
{
    Logs,
    Uploads,
    Capture
}

internal static class FilePath
{
    internal const string Base = "files";

    internal static void EnsureDirs()
    {
        foreach (DirType type in Enum.GetValues<DirType>())
        {
            string path = Path.Combine(Base, type.ToString().ToLower());
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }

    /// <summary>
    /// 获取文件夹路径
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    internal static string GetDir(DirType type)
        => Path.Combine(Base, type.ToString().ToLower());

    internal static string Logs => GetDir(DirType.Logs);
    internal static string Uploads => GetDir(DirType.Uploads);
    internal static string Capture => GetDir(DirType.Capture);
}
