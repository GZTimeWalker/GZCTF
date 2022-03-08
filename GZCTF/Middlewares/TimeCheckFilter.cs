using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using CTFServer.Utils;

namespace CTFServer.Middlewares;

/// <summary>
/// 检查比赛开始时间
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class TimeCheckAttribute : Attribute, IAsyncAuthorizationFilter
{

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {

        DateTimeOffset now = DateTimeOffset.UtcNow;

        string? start= context.HttpContext.RequestServices.GetRequiredService<IConfiguration>()["StartTime"];

        if(DateTimeOffset.TryParse(start, out DateTimeOffset startTime))
        {
            if (now < startTime)
            {
                context.Result = new JsonResult(new RequestResponse("解密尚未开始", 403))
                {
                    StatusCode = 403
                };
            }
        }

        return Task.CompletedTask;
    }
}