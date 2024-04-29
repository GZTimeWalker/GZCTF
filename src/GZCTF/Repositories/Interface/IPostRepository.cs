namespace GZCTF.Repositories.Interface;

public interface IPostRepository : IRepository
{
    /// <summary>
    /// 添加文章
    /// </summary>
    /// <param name="post">文章对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Post> CreatePost(Post post, CancellationToken token = default);

    /// <summary>
    /// 更新文章
    /// </summary>
    /// <param name="post">文章对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task UpdatePost(Post post, CancellationToken token = default);

    /// <summary>
    /// 移除文章
    /// </summary>
    /// <param name="post">文章对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task RemovePost(Post post, CancellationToken token = default);

    /// <summary>
    /// 获取指定文章
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Post[]> GetPosts(CancellationToken token = default);

    /// <summary>
    /// 根据 Id 返回文章
    /// </summary>
    /// <param name="id"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Post?> GetPostById(string id, CancellationToken token = default);

    /// <summary>
    /// 根据 Id 从缓存返回文章
    /// </summary>
    /// <param name="id"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Post?> GetPostByIdFromCache(string id, CancellationToken token = default);
}
