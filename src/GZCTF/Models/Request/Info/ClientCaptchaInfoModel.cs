using GZCTF.Models.Internal;
using MemoryPack;

namespace GZCTF.Models.Request.Info;

/// <summary>
/// Client CAPTCHA information
/// </summary>
[MemoryPackable]
public partial class ClientCaptchaInfoModel
{
    [MemoryPackConstructor]
    public ClientCaptchaInfoModel() { }

    public ClientCaptchaInfoModel(CaptchaConfig? config)
    {
        if (config is null)
            return;

        Type = config.Provider;
        SiteKey = config.SiteKey ?? string.Empty;
    }

    /// <summary>
    /// Captcha Provider Type
    /// </summary>
    public CaptchaProvider Type { get; set; } = CaptchaProvider.None;

    /// <summary>
    /// Site Key
    /// </summary>
    public string SiteKey { get; set; } = string.Empty;
}
