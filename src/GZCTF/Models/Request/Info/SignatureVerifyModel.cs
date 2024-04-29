using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Info;

/// <summary>
/// 签名校验
/// </summary>
public class SignatureVerifyModel
{
    /// <summary>
    /// 队伍 Token
    /// </summary>
    [Required]
    public string TeamToken { get; set; } = string.Empty;

    /// <summary>
    /// 比赛公钥，Base64 编码
    /// </summary>
    [Required]
    public string PublicKey { get; set; } = string.Empty;
}
