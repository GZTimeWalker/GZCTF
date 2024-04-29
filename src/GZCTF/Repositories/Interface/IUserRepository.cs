namespace GZCTF.Repositories.Interface;

public interface IUserRepository
{
    /// <summary>
    /// 从 ID 查找用户
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<UserInfo?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
