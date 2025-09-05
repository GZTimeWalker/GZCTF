namespace GZCTF.Repositories.Interface;

public interface IPostRepository : IRepository
{
    /// <summary>
    /// Add a new post
    /// </summary>
    /// <param name="post"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Post> CreatePost(Post post, CancellationToken token = default);

    /// <summary>
    /// Update an existing post
    /// </summary>
    /// <param name="post"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task UpdatePost(Post post, CancellationToken token = default);

    /// <summary>
    /// Remove post
    /// </summary>
    /// <param name="post"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task RemovePost(Post post, CancellationToken token = default);

    /// <summary>
    /// Get all posts
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<DataWithModifiedTime<Post[]>> GetPosts(CancellationToken token = default);

    /// <summary>
    /// Get post by ID
    /// </summary>
    /// <param name="id"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Post?> GetPostById(string id, CancellationToken token = default);

    /// <summary>
    /// Get post by ID from cache
    /// </summary>
    /// <param name="id"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Post?> GetPostByIdFromCache(string id, CancellationToken token = default);
}
