﻿using System.Globalization;
using System.Net;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;

namespace GZCTF.Middlewares;

/// <summary>
/// The rate limiter middleware
/// </summary>
public static class RateLimiter
{
    public enum LimitPolicy
    {
        /// <summary>
        /// Concurrency operation limit
        /// </summary>
        Concurrency,

        /// <summary>
        /// Register limit
        /// </summary>
        Register,

        /// <summary>
        /// Database query limit
        /// </summary>
        Query,

        /// <summary>
        /// Container operation limit
        /// </summary>
        Container,

        /// <summary>
        /// Flag submit limit
        /// </summary>
        Submit,

        /// <summary>
        /// Pow challenge generation limit
        /// </summary>
        PowChallenge
    }

    public static void ConfigureRateLimiter(RateLimiterOptions options)
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId is not null)
                return RateLimitPartition.GetSlidingWindowLimiter(userId,
                    _ => new()
                    {
                        QueueLimit = 60,
                        PermitLimit = 150,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        SegmentsPerWindow = 6
                    });

            var address = context.Connection.RemoteIpAddress;

            if (address is null || IPAddress.IsLoopback(address))
                return RateLimitPartition.GetNoLimiter(IPAddress.Loopback.ToString());

            return RateLimitPartition.GetSlidingWindowLimiter(address.ToString(),
                _ => new()
                {
                    QueueLimit = 60,
                    PermitLimit = 150,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    SegmentsPerWindow = 6
                });
        });
        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.HttpContext.Response.ContentType = MediaTypeNames.Application.Json;

            var localizer =
                context.HttpContext.RequestServices.GetRequiredService<IStringLocalizer<Program>>();
            var afterSec = (int)TimeSpan.FromMinutes(1).TotalSeconds;

            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                afterSec = (int)retryAfter.TotalSeconds;

            context.HttpContext.Response.Headers.RetryAfter = afterSec.ToString(NumberFormatInfo.InvariantInfo);
            await context.HttpContext.Response.WriteAsJsonAsync(
                new RequestResponse(localizer[nameof(Resources.Program.RateLimit_TooManyRequests), afterSec],
                    StatusCodes.Status429TooManyRequests
                ), cancellationToken);
        };
        options.AddConcurrencyLimiter(nameof(LimitPolicy.Concurrency), o =>
        {
            o.PermitLimit = 1;
            o.QueueLimit = 20;
        });
        options.AddSlidingWindowLimiter(nameof(LimitPolicy.Register), o =>
        {
            o.QueueLimit = 10;
            o.PermitLimit = 20;
            o.Window = TimeSpan.FromSeconds(150);
            o.QueueProcessingOrder = QueueProcessingOrder.NewestFirst;
            o.SegmentsPerWindow = 5;
        });
        options.AddTokenBucketLimiter(nameof(LimitPolicy.Query), o =>
        {
            o.TokenLimit = 100;
            o.TokensPerPeriod = 10;
            o.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
        });
        options.AddTokenBucketLimiter(nameof(LimitPolicy.PowChallenge), o =>
        {
            o.TokenLimit = 40;
            o.TokensPerPeriod = 5;
            o.ReplenishmentPeriod = TimeSpan.FromSeconds(30);
        });
        options.AddTokenBucketLimiter(nameof(LimitPolicy.Container), o =>
        {
            o.TokenLimit = 120;
            o.TokensPerPeriod = 30;
            o.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
        });
        options.AddTokenBucketLimiter(nameof(LimitPolicy.Submit), o =>
        {
            o.TokenLimit = 100;
            o.TokensPerPeriod = 50;
            o.ReplenishmentPeriod = TimeSpan.FromSeconds(5);
        });
    }
}
