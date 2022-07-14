namespace CTFServer.Models.Request.Admin;

public class ParticipationUpdateModel
{
    /// <summary>
    /// 参与对象 Id
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 参与状态
    /// </summary>
    public ParticipationStatus Status { get; set; }
}