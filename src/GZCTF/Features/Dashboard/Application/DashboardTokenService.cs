using System.Security.Cryptography;
using System.Text;
using GZCTF.Features.Dashboard.Domain;
using GZCTF.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Features.Dashboard.Application;

public sealed record DashboardTokenResult(Guid TokenId, string RawToken, DateTimeOffset? ExpiresAtUtc);

public sealed class DashboardTokenService(AppDbContext db, DashboardRequestLimiter requestLimiter)
{
    private const string Purpose = "GZCTF:dashboard:v1:";

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
        if (!await db.Dashboards.AnyAsync(item => item.Id == dashboardId && item.IsEnabled, token))
            return null;

        var current = await db.DashboardTokens.SingleOrDefaultAsync(item =>
            item.DashboardId == dashboardId && item.Id == tokenId && item.RevokedAtUtc == null, token);
        if (current is null) return null;

        await using var transaction = await db.Database.BeginTransactionAsync(token);
        current.RevokedAtUtc = DateTimeOffset.UtcNow;
        var raw = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var replacement = new DashboardToken
        {
            DashboardId = dashboardId,
            TokenHash = Hash(raw),
            ExpiresAtUtc = expiresAtUtc
        };
        db.DashboardTokens.Add(replacement);
        await db.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        return new DashboardTokenResult(replacement.Id, raw, replacement.ExpiresAtUtc);
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
        if (string.IsNullOrWhiteSpace(rawToken)) return false;
        var hash = Hash(rawToken);
        if (!requestLimiter.Allow($"{clientAddress}:{hash}", DateTimeOffset.UtcNow)) return false;
        var entity = await db.DashboardTokens.SingleOrDefaultAsync(item => item.DashboardId == dashboardId && item.TokenHash == hash, token);
        if (entity is null || entity.RevokedAtUtc is not null || entity.ExpiresAtUtc <= DateTimeOffset.UtcNow) return false;
        entity.LastUsedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(token);
        return true;
    }

    public static string Hash(string rawToken) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Purpose + rawToken))).ToLowerInvariant();
}
