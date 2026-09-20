using GZCTF.Models.Data;

namespace GZCTF.Features.Dashboard.Domain;

public sealed class Cohort
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public List<UserInfo> Users { get; set; } = [];
}

public sealed class Dashboard
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Name { get; set; } = string.Empty;
    public int TopCount { get; set; } = 10;
    public bool IsEnabled { get; set; } = true;
    public string? DisplaySettingsJson { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public List<DashboardToken> Tokens { get; set; } = [];
}

public sealed class DashboardToken
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid DashboardId { get; set; }
    public Dashboard Dashboard { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public DateTimeOffset? LastUsedAtUtc { get; set; }
}

public sealed class LearnerDailySolveStat
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public UserInfo User { get; set; } = null!;
    public DateOnly Date { get; set; }
    public int SolveCount { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
