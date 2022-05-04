namespace CTFServer.Utils;

/// <summary>
/// 任务结果
/// </summary>
/// <typeparam name="TResult">返回值类型</typeparam>
/// <param name="Status">状态</param>
/// <param name="Result">结果</param>
public record TaskResult<TResult>(TaskStatus Status, TResult? Result = default);

/// <summary>
/// 请求响应
/// </summary>
/// <param name="Title">响应信息</param>
/// <param name="Status">状态码</param>
public record RequestResponse(string Title, int Status = 400);

/// <summary>
/// 请求响应
/// </summary>
/// <param name="Title">响应信息</param>
/// <param name="Data">数据</param>
/// <param name="Status">状态码</param>
public record RequestResponse<T>(string Title, T Data, int Status = 400);