using GZCTF.Repositories.Interface;
using GZCTF.Services.Cache;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class PostRepository(
    CacheHelper cacheHelper,
    ILogger<PostRepository> logger,
    AppDbContext context) : RepositoryBase(context), IPostRepository
{
    public override Task<int> CountAsync(CancellationToken token = default) => Context.Posts.CountAsync(token);

    public async Task<Post> CreatePost(Post post, CancellationToken token = default)
    {
        post.UpdateKeyWithHash();
        await Context.AddAsync(post, token);
        await SaveAsync(token);

        await cacheHelper.RemoveAsync(CacheKey.Posts, token);

        return post;
    }

    public Task<Post?> GetPostById(string id, CancellationToken token = default) =>
        Context.Posts.FirstOrDefaultAsync(p => p.Id == id, token);

    public async Task<Post?> GetPostByIdFromCache(string id, CancellationToken token = default) =>
        (await GetPosts(token)).Data.FirstOrDefault(p => p.Id == id);

    public Task<DataWithModifiedTime<Post[]>> GetPosts(CancellationToken token = default) =>
        cacheHelper.GetOrCreateAsync(logger, CacheKey.Posts, async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromHours(12);
            var data = await Context.Posts.AsNoTracking().OrderByDescending(n => n.IsPinned)
                .ThenByDescending(n => n.UpdateTimeUtc).ToArrayAsync(token);
            return new DataWithModifiedTime<Post[]>(data, DateTimeOffset.UtcNow);
        }, token: token);

    public async Task RemovePost(Post post, CancellationToken token = default)
    {
        Context.Remove(post);
        await SaveAsync(token);

        await cacheHelper.RemoveAsync(CacheKey.Posts, token);
    }

    public async Task UpdatePost(Post post, CancellationToken token = default)
    {
        Context.Update(post);
        await SaveAsync(token);

        await cacheHelper.RemoveAsync(CacheKey.Posts, token);
    }
}
