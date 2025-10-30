using GZCTF.Models.Request.Game;
using GZCTF.Repositories.Interface;
using MemoryPack;

namespace GZCTF.Services.Cache.Handlers;

public class RecentGamesCacheHandler : ICacheRequestHandler
{
    public string CacheKey(CacheRequest request) => Cache.CacheKey.RecentGames;

    public async Task<byte[]> Handle(AsyncServiceScope scope, CacheRequest request, CancellationToken token = default)
    {
        var gameRepository = scope.ServiceProvider.GetRequiredService<IGameRepository>();

        try
        {
            var games = await gameRepository.GenRecentGames(token);
            var data = new DataWithModifiedTime<BasicGameInfoModel[]>(games, DateTimeOffset.UtcNow);
            return MemoryPackSerializer.Serialize(data);
        }
        catch (Exception e)
        {
            var logger =
                scope.ServiceProvider.GetRequiredService<ILogger<RecentGamesCacheHandler>>();
            logger.LogErrorMessage(e,
                StaticLocalizer[nameof(Resources.Program.Cache_GenerationFailed), CacheKey(request)!]);
            return [];
        }
    }

    public static CacheRequest MakeCacheRequest() =>
        new(Cache.CacheKey.RecentGames,
            new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(3) });
}
