namespace GZCTF.Utils;

enum DirType : byte
{
    Logs,
    Uploads,
    Capture
}

static class FilePath
{
    const string _base = "files";

    internal static readonly string Logs = GetDir(DirType.Logs);
    internal static readonly string Uploads = GetDir(DirType.Uploads);
    internal static readonly string Capture = GetDir(DirType.Capture);

    internal static bool AllowBaseCreate(IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
            return true;

        var know = Environment.GetEnvironmentVariable("YES_I_KNOW_FILES_ARE_NOT_PERSISTED_GO_AHEAD_PLEASE");
        if (know is not null)
            return true;

        return false;
    }

    internal static async Task EnsureDirsAsync(IHostEnvironment environment)
    {
        if (!Directory.Exists(_base))
        {
            if (AllowBaseCreate(environment))
                Directory.CreateDirectory(_base);
            else
                Program.ExitWithFatalMessage(
                    Program.StaticLocalizer[nameof(Resources.Program.Init_NoFilesDir), Path.GetFullPath(_base)]);
        }

        await using (FileStream versionFile = File.Open(Path.Combine(_base, "version.txt"), FileMode.Create))
        await using (var writer = new StreamWriter(versionFile))
        {
            await writer.WriteLineAsync(typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown");
        }

        foreach (DirType type in Enum.GetValues<DirType>())
        {
            var path = Path.Combine(_base, type.ToString().ToLower());
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
    }

    /// <summary>
    /// 获取文件夹路径
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    static string GetDir(DirType type) => Path.Combine(_base, type.ToString().ToLower());

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
