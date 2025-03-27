using GZCTF.Repositories.Interface;
using MemoryPack;

namespace GZCTF.Services.Cache.Handlers;

public class ScoreboardCacheHandler : ICacheRequestHandler
{
    public string? CacheKey(CacheRequest request)
        => request.Params.Length switch
        {
            1 => Cache.CacheKey.ScoreBoard(request.Params[0]),
            _ => null
        };

    public async Task<byte[]> Handler(AsyncServiceScope scope, CacheRequest request, CancellationToken token = default)
    {
        if (!int.TryParse(request.Params[0], out var id))
            return [];

        var gameRepository = scope.ServiceProvider.GetRequiredService<IGameRepository>();
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return [];

        try
        {
            var scoreboard = await gameRepository.GenScoreboard(game, token);
            return MemoryPackSerializer.Serialize(scoreboard);
        }
        catch (Exception e)
        {
            var logger =
                scope.ServiceProvider.GetRequiredService<ILogger<ScoreboardCacheHandler>>();
            logger.LogErrorMessage(e,
                StaticLocalizer[nameof(Resources.Program.Cache_GenerationFailed), CacheKey(request)!]);
            return [];
        }
    }

    public static CacheRequest MakeCacheRequest(int id) =>
        new(Cache.CacheKey.ScoreBoardBase,
            new() { SlidingExpiration = TimeSpan.FromDays(14) }, id.ToString());
}
