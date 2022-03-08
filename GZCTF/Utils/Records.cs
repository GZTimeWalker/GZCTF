namespace CTFServer.Utils;

/// <summary>
/// 验证结果
/// </summary>
public record VerifyResult(AnswerResult Result = AnswerResult.WrongAnswer, int Score = 0, int UpgradeAccessLevel = 0);

#region 请求响应

public record RequestResponse(string Title, int Status = 400);
public record ChallengesResponse(int Id, int Status = 200);

#endregion 请求响应
