using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using GZCTF.Features.Dashboard.Domain;
using GZCTF.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Features.Dashboard.Application;

public sealed record DashboardTokenResult(Guid TokenId, string RawToken, DateTimeOffset? ExpiresAtUtc);

public sealed class DashboardTokenService(AppDbContext db)
{
    private const string Purpose = "GZCTF:dashboard:v1:";
    private static readonly ConcurrentDictionary<string, Queue<DateTimeOffset>> Requests = new();

    public async Task<DashboardTokenResult?> CreateAsync(Guid dashboardId, DateTimeOffset? expiresAtUtc, CancellationToken token)
    {
        if (!await db.Dashboards.AnyAsync(item => item.Id == dashboardId && item.IsEnabled, token)) return null;
        var raw = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var entity = new DashboardToken { DashboardId = dashboardId, TokenHash = Hash(raw), ExpiresAtUtc = expiresAtUtc };
        db.DashboardTokens.Add(entity);
        await db.SaveChangesAsync(token);
        return new DashboardTokenResult(entity.Id, raw, entity.ExpiresAtUtc);
    }

    public async Task<DashboardTokenResult?> RotateAsync(Guid dashboardId, Guid tokenId, DateTimeOffset? expiresAtUtc, CancellationToken token)
    {
        var current = await db.DashboardTokens.SingleOrDefaultAsync(item => item.DashboardId == dashboardId && item.Id == tokenId, token);
        if (current is null) return null;
        current.RevokedAtUtc = DateTimeOffset.UtcNow;
        return await CreateAsync(dashboardId, expiresAtUtc, token);
    }

    public async Task<bool> RevokeAsync(Guid dashboardId, Guid tokenId, CancellationToken token)
    {
        var current = await db.DashboardTokens.SingleOrDefaultAsync(item => item.DashboardId == dashboardId && item.Id == tokenId, token);
        if (current is null) return false;
        current.RevokedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(token);
        return true;
    }

    public async Task<bool> IsValidAsync(Guid dashboardId, string? rawToken, string? clientAddress, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(rawToken) || !Allow(rawToken, clientAddress)) return false;
        var hash = Hash(rawToken);
        var entity = await db.DashboardTokens.SingleOrDefaultAsync(item => item.DashboardId == dashboardId && item.TokenHash == hash, token);
        if (entity is null || entity.RevokedAtUtc is not null || entity.ExpiresAtUtc <= DateTimeOffset.UtcNow) return false;
        entity.LastUsedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(token);
        return true;
    }

    public static string Hash(string rawToken) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Purpose + rawToken))).ToLowerInvariant();

    private static bool Allow(string rawToken, string? clientAddress)
    {
        var key = $"{clientAddress}:{Hash(rawToken)}";
        var now = DateTimeOffset.UtcNow;
        var queue = Requests.GetOrAdd(key, _ => new Queue<DateTimeOffset>());
        lock (queue)
        {
            while (queue.Count > 0 && now - queue.Peek() > TimeSpan.FromMinutes(1)) queue.Dequeue();
            if (queue.Count >= 120) return false;
            queue.Enqueue(now);
            return true;
        }
    }
}
