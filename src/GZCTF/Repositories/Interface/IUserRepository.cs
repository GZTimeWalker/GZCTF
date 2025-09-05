namespace GZCTF.Repositories.Interface;

public interface IUserRepository
{
    /// <summary>
    /// Find user by ID
    /// </summary>
    /// <param name="id"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<UserInfo?> FindByIdAsync(Guid id, CancellationToken token = default);
}
