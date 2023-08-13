using GZCTF.Repositories.Interface;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace GZCTF.Repositories;

public class PostRepository : RepositoryBase, IPostRepository
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<PostRepository> _logger;

    public PostRepository(IDistributedCache cache,
        ILogger<PostRepository> logger,
        AppDbContext context) : base(context)
    {
        _cache = cache;
        _logger = logger;
    }

    public override Task<int> CountAsync(CancellationToken token = default) => _context.Posts.CountAsync(token);

    public async Task<Post> CreatePost(Post post, CancellationToken token = default)
    {
        post.UpdateKeyWithHash();
        await _context.AddAsync(post, token);
        await SaveAsync(token);

        _cache.Remove(CacheKey.Posts);

        return post;
    }

    public Task<Post?> GetPostById(string id, CancellationToken token = default)
        => _context.Posts.FirstOrDefaultAsync(p => p.Id == id, token);

    public async Task<Post?> GetPostByIdFromCache(string id, CancellationToken token = default)
        => (await GetPosts(token)).FirstOrDefault(p => p.Id == id);

    public Task<Post[]> GetPosts(CancellationToken token = default)
        => _cache.GetOrCreateAsync(_logger, CacheKey.Posts, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);
            return _context.Posts.AsNoTracking().OrderByDescending(n => n.IsPinned)
                    .ThenByDescending(n => n.UpdateTimeUTC).ToArrayAsync(token);
        }, token);

    public async Task RemovePost(Post post, CancellationToken token = default)
    {
        _context.Remove(post);
        await SaveAsync(token);

        _cache.Remove(CacheKey.Posts);
    }

    public async Task UpdatePost(Post post, CancellationToken token = default)
    {
        _context.Update(post);
        await SaveAsync(token);

        _cache.Remove(CacheKey.Posts);
    }
}
