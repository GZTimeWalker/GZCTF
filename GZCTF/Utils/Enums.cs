namespace CTFServer;

/// <summary>
/// 用户权限枚举
/// </summary>
public enum Privilege
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
public enum TaskStatus
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
}

/// <summary>
/// 判定结果
/// </summary>
public enum AnswerResult
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
}

/// <summary>
/// 缓存标识
/// </summary>
public static class CacheKey
{
    /// <summary>
    /// 最高访问等级
    /// </summary>
    public const string MaxAccessLevel = "_MaxAccessLevel";

    /// <summary>
    /// 积分榜缓存
    /// </summary>
    public const string ScoreBoard = "_ScoreBoard";

    /// <summary>
    /// 公告
    /// </summary>
    public const string Announcements = "_Announcements";

    /// <summary>
    /// 可访问的谜题列表
    /// </summary>
    /// <param name="accessLevel">访问等级</param>
    /// <returns></returns>
    public static string AccessiblePuzzles(int accessLevel) => $"_AccessiblePuzzle_{accessLevel}";
}
