using CTFServer.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using YamlDotNet.Core.Tokens;

namespace CTFServer.Repositories;

public class PostRepository : RepositoryBase, IPostRepository
{
    private readonly IMemoryCache cache;

    public PostRepository(IMemoryCache memoryCache, AppDbContext _context) : base(_context)
    {
        cache = memoryCache;
    }

    public async Task<Post> CreatePost(Post post, CancellationToken token = default)
    {
        post.UpdateKeyWithHash();
        await context.AddAsync(post, token);
        await SaveAsync(token);

        cache.Remove(CacheKey.Posts);

        return post;
    }

    public Task<Post[]> GetLatestPosts(CancellationToken token = default)
        => cache.GetOrCreateAsync(CacheKey.Posts, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);
            return context.Posts.OrderByDescending(n => n.IsPinned)
                    .ThenByDescending(n => n.UpdateTimeUTC).Take(20).ToArrayAsync(token);
        });

    public Task<Post?> GetPostById(string id, CancellationToken token = default)
        => cache.GetOrCreateAsync(CacheKey.Post(id), entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);
            return context.Posts.FirstOrDefaultAsync(post => post.Id == id, token);
        });

    public Task<Post[]> GetPosts(int count = 30, int skip = 0, CancellationToken token = default)
        => context.Posts.OrderByDescending(e => e.UpdateTimeUTC)
            .OrderByDescending(e => e.IsPinned).Skip(skip).Take(count).ToArrayAsync(token);

    public async Task RemovePost(Post post, CancellationToken token = default)
    {
        context.Remove(post);
        await SaveAsync(token);

        cache.Remove(CacheKey.Post(post.Id));
        cache.Remove(CacheKey.Posts);
    }

    public async Task UpdatePost(Post post, CancellationToken token = default)
    {
        await SaveAsync(token);

        cache.Remove(CacheKey.Post(post.Id));
        cache.Remove(CacheKey.Posts);
    }
}