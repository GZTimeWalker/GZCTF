using GZCTF.Extensions;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Cache;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace GZCTF.Repositories;

public class PostRepository(
    IDistributedCache cache,
    ILogger<PostRepository> logger,
    AppDbContext context) : RepositoryBase(context), IPostRepository
{
    public override Task<int> CountAsync(CancellationToken token = default) => Context.Posts.CountAsync(token);

    public async Task<Post> CreatePost(Post post, CancellationToken token = default)
    {
        post.UpdateKeyWithHash();
        await Context.AddAsync(post, token);
        await SaveAsync(token);

        await cache.RemoveAsync(CacheKey.Posts, token);

        return post;
    }

    public Task<Post?> GetPostById(string id, CancellationToken token = default) =>
        Context.Posts.FirstOrDefaultAsync(p => p.Id == id, token);

    public async Task<Post?> GetPostByIdFromCache(string id, CancellationToken token = default) =>
        (await GetPosts(token)).FirstOrDefault(p => p.Id == id);

    public Task<Post[]> GetPosts(CancellationToken token = default) =>
        cache.GetOrCreateAsync(logger, CacheKey.Posts, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);
            return Context.Posts.AsNoTracking().OrderByDescending(n => n.IsPinned)
                .ThenByDescending(n => n.UpdateTimeUtc).ToArrayAsync(token);
        }, token);

    public async Task RemovePost(Post post, CancellationToken token = default)
    {
        Context.Remove(post);
        await SaveAsync(token);

        await cache.RemoveAsync(CacheKey.Posts, token);
    }

    public async Task UpdatePost(Post post, CancellationToken token = default)
    {
        Context.Update(post);
        await SaveAsync(token);

        await cache.RemoveAsync(CacheKey.Posts, token);
    }
}
