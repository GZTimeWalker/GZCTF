using System.ComponentModel.DataAnnotations;

namespace CTFServer.Models.Request.Info;

public class TeamTransferModel
{
    /// <summary>
    /// 新队长 Id
    /// </summary>
    [Required]
    public string NewCaptainId { get; set; } = string.Empty;
}
