namespace GZCTF.Models;

public static class Limits
{
    /// <summary>
    /// Flag 最大长度
    /// </summary>
    public const int MaxFlagLength = 127;

    /// <summary>
    /// Flag 模板最大长度, 为替换操作预留空间
    /// </summary>
    public const int MaxFlagTemplateLength = 120;

    /// <summary>
    /// 队伍名称最大长度
    /// </summary>
    public const int MaxTeamNameLength = 20;

    /// <summary>
    /// 队伍签名最大长度（前端展示原因）
    /// </summary>
    public const int MaxTeamBioLength = 72;

    /// <summary>
    /// 个人数据存储最大长度（签名与真实姓名）
    /// </summary>
    public const int MaxUserDataLength = 128;

    /// <summary>
    /// 学工号最大长度
    /// </summary>
    public const int MaxStdNumberLength = 64;

    /// <summary>
    /// 用户名最小长度
    /// </summary>
    public const int MinUserNameLength = 3;

    /// <summary>
    /// 用户名最大长度
    /// </summary>
    public const int MaxUserNameLength = 15;

    /// <summary>
    /// 密码最小长度
    /// </summary>
    public const int MinPasswordLength = 6;

    /// <summary>
    /// 文件哈希长度
    /// </summary>
    public const int FileHashLength = 64;

    /// <summary>
    /// 比赛公私钥长度
    /// </summary>
    public const int GameKeyLength = 63;

    /// <summary>
    /// 邀请 Token 长度
    /// </summary>
    public const int InviteTokenLength = 32;

    /// <summary>
    /// 最大 IP 长度
    /// </summary>
    public const int MaxIPLength = 40;

    /// <summary>
    /// 最大标题长度
    /// </summary>
    public const int MaxPostTitleLength = 50;

    /// <summary>
    /// 最大日志等级长度
    /// </summary>
    public const int MaxLogLevelLength = 15;

    /// <summary>
    /// 最大日志记录源长度
    /// </summary>
    public const int MaxLoggerLength = 250;

    /// <summary>
    /// 最大日志状态长度
    /// </summary>
    public const int MaxLogStatusLength = 10;
}
