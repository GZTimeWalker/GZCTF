namespace CTFServer;

/// <summary>
/// 用户权限枚举
/// </summary>
public enum Role: byte
{
    /// <summary>
    /// 小黑屋用户权限
    /// </summary>
    BannedUser = 0,

    /// <summary>
    /// 常规用户权限
    /// </summary>
    User = 1,

    /// <summary>
    /// 监控者权限，可查询提交日志
    /// </summary>
    Monitor = 2,

    /// <summary>
    /// 管理员权限，可查看系统日志
    /// </summary>
    Admin = 3,
}

/// <summary>
/// 任务执行状态
/// </summary>
public enum TaskStatus: sbyte
{
    /// <summary>
    /// 任务正在进行
    /// </summary>
    Pending = -1,

    /// <summary>
    /// 任务成功完成
    /// </summary>
    Success = 0,

    /// <summary>
    /// 任务执行失败
    /// </summary>
    Fail = 1,

    /// <summary>
    /// 任务遇到重复错误
    /// </summary>
    Duplicate = 2,

    /// <summary>
    /// 任务处理被拒绝
    /// </summary>
    Denied = 3,

    /// <summary>
    /// 任务请求未找到
    /// </summary>
    NotFound = 4,
}

public enum FileType: byte
{
    /// <summary>
    /// 无附件
    /// </summary>
    None = 0,
    /// <summary>
    /// 本地文件
    /// </summary>
    Local = 1,
    /// <summary>
    /// 远程文件
    /// </summary>
    Remote = 2,
}

public enum ContainerStatus : byte
{
    /// <summary>
    /// 正在启动
    /// </summary>
    Pending = 0,
    /// <summary>
    /// 正在运行
    /// </summary>
    Running = 1,
    /// <summary>
    /// 已销毁
    /// </summary>
    Destoryed = 2
}

/// <summary>
/// 比赛公告类型
/// </summary>
public enum NoticeType: byte
{
    /// <summary>
    /// 常规公告
    /// </summary>
    Normal = 0,

    /// <summary>
    /// 一血
    /// </summary>
    FirstBlood = 1,

    /// <summary>
    /// 二血
    /// </summary>
    SecondBlood = 2,

    /// <summary>
    /// 三血
    /// </summary>
    ThirdBlood = 3,

    /// <summary>
    /// 发布新的提示
    /// </summary>
    NewHint = 4,

    /// <summary>
    /// 修复错误
    /// </summary>
    ErrorFix = 5
}

public enum ChallengeType: byte
{
    /// <summary>
    /// 静态题目
    /// 所有队伍使用统一附件、统一 flag
    /// </summary>
    StaticAttachment  = 0b00,
    /// <summary>
    /// 容器静态题目
    /// 所有队伍使用统一 docker，统一 flag
    /// </summary>
    StaticContainer   = 0b01,
    /// <summary>
    /// 动态附件题目
    /// 随机分发附件，随附件实现 flag 特异性
    /// </summary>
    DynamicAttachment = 0b10,
    /// <summary>
    /// 容器动态题目
    /// 随机分发容器，动态 flag 随环境变量传入
    /// </summary>
    DynamicContainer  = 0b11
}

public static class ChallengeTypeExtensions
{
    /// <summary>
    /// 是否为静态题目
    /// </summary>
    public static bool IsStatic(this ChallengeType type) => ((byte)type & 0b01) == 0;

    /// <summary>
    /// 是否为动态题目
    /// </summary>
    public static bool IsDynamic(this ChallengeType type) => ((byte)type & 0b01) != 0;

    /// <summary>
    /// 是否为附件题目
    /// </summary>
    public static bool IsAttachment(this ChallengeType type) => ((byte)type & 0b10) == 0;

    /// <summary>
    /// 是否为容器题目
    /// </summary>
    public static bool IsContainer(this ChallengeType type) => ((byte)type & 0b10) != 0;
}

/// <summary>
/// 判定结果
/// </summary>
public enum AnswerResult: byte
{
    /// <summary>
    /// 答案正确
    /// </summary>
    Accepted = 1,

    /// <summary>
    /// 答案错误
    /// </summary>
    WrongAnswer = 2,

    /// <summary>
    /// 提交未授权
    /// </summary>
    Unauthorized = 3,

    /// <summary>
    /// 检测到抄袭
    /// </summary>
    CheatDetected = 4
}

/// <summary>
/// 缓存标识
/// </summary>
public static class CacheKey
{
    /// <summary>
    /// 积分榜缓存
    /// </summary>
    public const string ScoreBoard = "_ScoreBoard";

    /// <summary>
    /// 公告
    /// </summary>
    public const string Notices = "_Notices";
}
