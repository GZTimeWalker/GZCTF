namespace GZCTF.Features.SkillTrees.Application;

/// <summary>
/// Seam for invalidating cached skill tree projections when shared category data changes.
/// A distributed cache can be plugged in later without touching the application services.
/// </summary>
public interface ISkillTreeCacheInvalidator
{
    Task InvalidateTreeAsync(Guid skillTreeId, CancellationToken token);
    Task InvalidateByCategoryAsync(Guid categoryId, CancellationToken token);
}

public sealed class NoopSkillTreeCacheInvalidator : ISkillTreeCacheInvalidator
{
    public Task InvalidateTreeAsync(Guid skillTreeId, CancellationToken token) => Task.CompletedTask;

    public Task InvalidateByCategoryAsync(Guid categoryId, CancellationToken token) => Task.CompletedTask;
}
