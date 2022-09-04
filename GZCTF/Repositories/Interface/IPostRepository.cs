namespace CTFServer.Repositories.Interface;

public interface IPostRepository : IRepository
{
    /// <summary>
    /// 添加文章
    /// </summary>
    /// <param name="notice">文章对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Post> CreatePost(Post notice, CancellationToken token = default);

    /// <summary>
    /// 更新文章
    /// </summary>
    /// <param name="notice">文章对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task UpdatePost(Post notice, CancellationToken token = default);

    /// <summary>
    /// 移除文章
    /// </summary>
    /// <param name="notice">文章对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task RemovePost(Post notice, CancellationToken token = default);

    /// <summary>
    /// 获取指定文章
    /// </summary>
    /// <param name="count"></param>
    /// <param name="skip"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Post[]> GetPosts(int count = 30, int skip = 0, CancellationToken token = default);

    /// <summary>
    /// 获取最新文章（20条）
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Post[]> GetLatestPosts(CancellationToken token = default);

    /// <summary>
    /// 根据 Id 返回文章
    /// </summary>
    /// <param name="id"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Post?> GetPostById(string id, CancellationToken token = default);
}