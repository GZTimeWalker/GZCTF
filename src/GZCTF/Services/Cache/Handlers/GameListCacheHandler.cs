using GZCTF.Models.Request.Game;
using GZCTF.Repositories.Interface;
using MemoryPack;

namespace GZCTF.Services.Cache.Handlers;

public class GameListCacheHandler : ICacheRequestHandler
{
    public string CacheKey(CacheRequest request) => Cache.CacheKey.GameList;

    public async Task<byte[]> Handler(AsyncServiceScope scope, CacheRequest request, CancellationToken token = default)
    {
        var gameRepository = scope.ServiceProvider.GetRequiredService<IGameRepository>();

        try
        {
            BasicGameInfoModel[] games = await gameRepository.FetchGameList(100, 0, token);
            return MemoryPackSerializer.Serialize(games);
        }
        catch (Exception e)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<GameListCacheHandler>>();
            logger.LogError(e, "{msg}",
                Program.StaticLocalizer[nameof(Resources.Program.Cache_GenerationFailed), CacheKey(request)!]);
            return [];
        }
    }

    public static CacheRequest MakeCacheRequest() =>
        new(Cache.CacheKey.GameList,
            new() { SlidingExpiration = TimeSpan.FromDays(2) });
}
