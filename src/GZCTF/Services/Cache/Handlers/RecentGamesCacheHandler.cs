using GZCTF.Models.Request.Game;
using GZCTF.Repositories.Interface;
using MemoryPack;

namespace GZCTF.Services.Cache.Handlers;

public class RecentGamesCacheHandler : ICacheRequestHandler
{
    public string CacheKey(CacheRequest request) => Cache.CacheKey.RecentGames;

    public async Task<byte[]> Handler(AsyncServiceScope scope, CacheRequest request, CancellationToken token = default)
    {
        var gameRepository = scope.ServiceProvider.GetRequiredService<IGameRepository>();

        try
        {
            BasicGameInfoModel[] games = await gameRepository.GenRecentGames(token);
            return MemoryPackSerializer.Serialize(games);
        }
        catch (Exception e)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<RecentGamesCacheHandler>>();
            logger.LogError(e, "{msg}",
                Program.StaticLocalizer[nameof(Resources.Program.Cache_GenerationFailed), CacheKey(request)!]);
            return [];
        }
    }

    public static CacheRequest MakeCacheRequest() =>
        new(Cache.CacheKey.RecentGames,
            new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(3) });
}
