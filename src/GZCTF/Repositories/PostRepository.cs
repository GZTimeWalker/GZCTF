using GZCTF.Repositories.Interface;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace GZCTF.Repositories;

public class PostRepository : RepositoryBase, IPostRepository
{
    private readonly IDistributedCache cache;
    private readonly ILogger<PostRepository> logger;

    public PostRepository(IDistributedCache _cache,
        ILogger<PostRepository> _logger,
        AppDbContext _context) : base(_context)
    {
        cache = _cache;
        logger = _logger;
    }

    public override Task<int> CountAsync(CancellationToken token = default) => context.Posts.CountAsync(token);

    public async Task<Post> CreatePost(Post post, CancellationToken token = default)
    {
        post.UpdateKeyWithHash();
        await context.AddAsync(post, token);
        await SaveAsync(token);

        cache.Remove(CacheKey.Posts);

        return post;
    }

    public Task<Post?> GetPostById(string id, CancellationToken token = default)
        => context.Posts.FirstOrDefaultAsync(p => p.Id == id, token);

    public async Task<Post?> GetPostByIdFromCache(string id, CancellationToken token = default)
        => (await GetPosts(token)).FirstOrDefault(p => p.Id == id);

    public Task<Post[]> GetPosts(CancellationToken token = default)
        => cache.GetOrCreateAsync(logger, CacheKey.Posts, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);
            return context.Posts.AsNoTracking().OrderByDescending(n => n.IsPinned)
                    .ThenByDescending(n => n.UpdateTimeUTC).ToArrayAsync(token);
        }, token);

    public async Task RemovePost(Post post, CancellationToken token = default)
    {
        context.Remove(post);
        await SaveAsync(token);

        cache.Remove(CacheKey.Posts);
    }

    public async Task UpdatePost(Post post, CancellationToken token = default)
    {
        context.Update(post);
        await SaveAsync(token);

        cache.Remove(CacheKey.Posts);
    }
}
