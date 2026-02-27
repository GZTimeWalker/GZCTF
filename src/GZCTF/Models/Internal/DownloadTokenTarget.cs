namespace GZCTF.Models.Internal;

public static class DownloadTokenTarget
{
    public static string ForLocalHash(string hash) => hash;

    public static string ForRemoteAttachment(int attachmentId) => $"remote:{attachmentId}";
}
