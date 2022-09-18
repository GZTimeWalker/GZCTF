using System;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using System.Net;
using System.Globalization;
using CTFServer.Utils;

namespace CTFServer.Middlewares;

/// <summary>
/// 请求频率限制
/// </summary>
public class RateLimiter
{
    public enum LimitPolicy
    {
        /// <summary>
        /// 并发操作限制
        /// </summary>
        Concurrency,

        /// <summary>
        /// 注册请求限制
        /// </summary>
        Register,

        /// <summary>
        /// 容器操作限制
        /// </summary>
        Container,

        /// <summary>
        /// 提交请求限制
        /// </summary>
        Submit
    }

    public static RateLimiterOptions GetRateLimiterOptions()
        => new RateLimiterOptions()
        {
            RejectionStatusCode = StatusCodes.Status429TooManyRequests,
            GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, IPAddress>(context =>
            {
                IPAddress? remoteIPaddress = context?.Connection?.RemoteIpAddress;

                if (remoteIPaddress is not null && !IPAddress.IsLoopback(remoteIPaddress))
                {
                    return RateLimitPartition.GetSlidingWindowLimiter<IPAddress>(remoteIPaddress, key => new()
                    {
                        PermitLimit = 150,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 60,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        SegmentsPerWindow = 6,
                    });
                }
                else
                {
                    return RateLimitPartition.GetNoLimiter<IPAddress>(IPAddress.Loopback);
                }
            }),
            OnRejected = (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                    ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
                }

                context?.HttpContext?.RequestServices?
                    .GetService<ILoggerFactory>()?
                    .CreateLogger<RateLimiter>()
                    .Log($"请求过于频繁：{context.HttpContext.Request.Path}",
                        context.HttpContext, TaskStatus.Denied, LogLevel.Debug);

                return new ValueTask();
            }
        }
        .AddConcurrencyLimiter(nameof(LimitPolicy.Concurrency), options =>
        {
            options.PermitLimit = 1;
            options.QueueLimit = 5;
        })
        .AddFixedWindowLimiter(nameof(LimitPolicy.Register), options =>
        {
            options.PermitLimit = 10;
            options.Window = TimeSpan.FromSeconds(150);
        })
        .AddTokenBucketLimiter(nameof(LimitPolicy.Container), options =>
        {
            options.TokenLimit = 4;
            options.TokensPerPeriod = 2;
            options.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
        })
        .AddTokenBucketLimiter(nameof(LimitPolicy.Submit), options =>
        {
            options.TokenLimit = 3;
            options.TokensPerPeriod = 1;
            options.ReplenishmentPeriod = TimeSpan.FromSeconds(20);
        });
}

public static class RateLimiterExtensions
{
    public static IApplicationBuilder UseConfiguredRateLimiter(this IApplicationBuilder builder)
        => builder.UseRateLimiter(RateLimiter.GetRateLimiterOptions());
}

