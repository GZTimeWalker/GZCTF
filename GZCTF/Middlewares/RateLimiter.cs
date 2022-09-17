using System;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using System.Net;

namespace CTFServer.Middlewares;

/// <summary>
/// 请求频率限制
/// </summary>
public static class RateLimiter
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

    public static IApplicationBuilder UseConfiguredRateLimiter(this IApplicationBuilder builder)
    {
        var rateLimiterOption = new RateLimiterOptions()
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
                options.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
            })
            .AddTokenBucketLimiter(nameof(LimitPolicy.Submit), options =>
            {
                options.TokenLimit = 3;
                options.ReplenishmentPeriod = TimeSpan.FromSeconds(60);
            });

        rateLimiterOption.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, IPAddress>(context =>
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
        });

        return builder.UseRateLimiter(rateLimiterOption);
    }
}

