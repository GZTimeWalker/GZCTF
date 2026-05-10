using GZCTF.Repositories.Interface;
using MemoryPack;

namespace GZCTF.Services.Cache.Handlers;

/// <summary>
/// Builds the frozen-view scoreboard for ICPC-style scoreboard freeze.
/// Identical to <see cref="ScoreboardCacheHandler"/> except the build passes the game's
/// FreezeTimeUtc as the snapshot cutoff so post-freeze submissions are excluded from
/// scoring, dynamic challenge solve counts, blood, ranks, and timelines.
/// </summary>
public class ScoreboardFrozenCacheHandler : ICacheRequestHandler
{
    public string? CacheKey(CacheRequest request)
        => request.Params.Length switch
        {
            1 => Cache.CacheKey.ScoreBoardFrozen(request.Params[0]),
            _ => null
        };

    public async Task<byte[]> Handle(AsyncServiceScope scope, CacheRequest request, CancellationToken token = default)
    {
        if (!int.TryParse(request.Params[0], out var id))
            return [];

        var gameRepository = scope.ServiceProvider.GetRequiredService<IGameRepository>();
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return [];

        try
        {
            var scoreboard = await gameRepository.GenScoreboard(game, game.FreezeTimeUtc, token);
            return MemoryPackSerializer.Serialize(scoreboard);
        }
        catch (Exception e)
        {
            var logger =
                scope.ServiceProvider.GetRequiredService<ILogger<ScoreboardFrozenCacheHandler>>();
            logger.LogErrorMessage(e,
                StaticLocalizer[nameof(Resources.Program.Cache_GenerationFailed), CacheKey(request)!]);
            return [];
        }
    }

    public static CacheRequest MakeCacheRequest(int id) =>
        new(Cache.CacheKey.ScoreBoardFrozenBase,
            new() { SlidingExpiration = TimeSpan.FromDays(14) }, id.ToString());
}
