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

    public Task<Post?> GetPostById(string id, CancellationToken token = default)
        => context.Posts.FirstOrDefaultAsync(p => p.Id == id, token);

    public async Task<Post?> GetPostByIdFromCache(string id, CancellationToken token = default)
        => (await GetPosts(token)).FirstOrDefault(p => p.Id == id);

    public Task<Post[]> GetPosts(CancellationToken token = default)
        => cache.GetOrCreateAsync(CacheKey.Posts, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);
            return context.Posts.AsNoTracking().OrderByDescending(n => n.IsPinned)
                    .ThenByDescending(n => n.UpdateTimeUTC).ToArrayAsync(token);
        });

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