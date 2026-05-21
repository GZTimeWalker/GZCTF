using GZCTF.Models.Data;

namespace GZCTF.Models.Response.Admin;

/// <summary>
/// One row returned by <c>GET /api/admin/AntiCheatBlocks</c> — a login
/// that was rejected by the per-team-user policy.
/// </summary>
public sealed class AntiCheatBlockModel
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public Guid? ConflictUserId { get; set; }
    public string? ConflictUserName { get; set; }
    public AntiCheatBlockKind Kind { get; set; }
    public string? ConflictingValue { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
}
