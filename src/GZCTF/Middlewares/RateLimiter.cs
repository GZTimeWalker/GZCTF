using System.Globalization;
using System.Net;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace GZCTF.Middlewares;

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

    public static RateLimiterOptions GetRateLimiterOptions() =>
        new RateLimiterOptions
        {
            RejectionStatusCode = StatusCodes.Status429TooManyRequests,
            GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var userId = context?.User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (userId is not null)
                    return RateLimitPartition.GetSlidingWindowLimiter(userId,
                        key => new()
                        {
                            PermitLimit = 150,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 60,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            SegmentsPerWindow = 6
                        });

                IPAddress? address = context?.Connection?.RemoteIpAddress;

                if (address is null || IPAddress.IsLoopback(address))
                    return RateLimitPartition.GetNoLimiter(IPAddress.Loopback.ToString());

                return RateLimitPartition.GetSlidingWindowLimiter(address.ToString(),
                    key => new()
                    {
                        PermitLimit = 150,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 60,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        SegmentsPerWindow = 6
                    });
            }),
            OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = MediaTypeNames.Application.Json;

                var afterSec = (int)TimeSpan.FromMinutes(1).TotalSeconds;

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
                    afterSec = (int)retryAfter.TotalSeconds;

                context.HttpContext.Response.Headers.RetryAfter = afterSec.ToString(NumberFormatInfo.InvariantInfo);
                await context.HttpContext.Response.WriteAsJsonAsync(
                    new RequestResponse($"Too many requests, retry after {afterSec} seconds.",
                        StatusCodes.Status429TooManyRequests
                    ), cancellationToken);
            }
        }
            .AddConcurrencyLimiter(nameof(LimitPolicy.Concurrency), options =>
            {
                options.PermitLimit = 1;
                options.QueueLimit = 20;
            })
            .AddFixedWindowLimiter(nameof(LimitPolicy.Register), options =>
            {
                options.PermitLimit = 20;
                options.Window = TimeSpan.FromSeconds(150);
            })
            .AddTokenBucketLimiter(nameof(LimitPolicy.Container), options =>
            {
                options.TokenLimit = 120;
                options.TokensPerPeriod = 30;
                options.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
            })
            .AddTokenBucketLimiter(nameof(LimitPolicy.Submit), options =>
            {
                options.TokenLimit = 60;
                options.TokensPerPeriod = 30;
                options.ReplenishmentPeriod = TimeSpan.FromSeconds(5);
            });
}

public static class RateLimiterExtensions
{
    public static IApplicationBuilder UseConfiguredRateLimiter(this IApplicationBuilder builder) =>
        builder.UseRateLimiter(RateLimiter.GetRateLimiterOptions());
}