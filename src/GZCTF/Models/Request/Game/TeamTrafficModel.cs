using FluentStorage;
using FluentStorage.Blobs;

namespace GZCTF.Models.Request.Game;

/// <summary>
/// Team traffic information
/// </summary>
public class TeamTrafficModel
{
    /// <summary>
    /// Participation ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Team Id
    /// </summary>
    public int TeamId { get; set; }

    /// <summary>
    /// Team name
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Division of participation
    /// </summary>
    public string? Division { get; set; }

    /// <summary>
    /// Avatar URL
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// Number of traffic captured by the challenge
    /// </summary>
    public int Count { get; set; }

    internal static async Task<TeamTrafficModel> FromParticipationAsync(Participation part, int challengeId,
        IBlobStorage storage, CancellationToken token)
    {
        var path = StoragePath.Combine(PathHelper.Capture, challengeId.ToString(), part.Id.ToString());

        return new()
        {
            Id = part.Id,
            TeamId = part.Team.Id,
            Name = part.Team.Name,
            Division = part.Division,
            Avatar = part.Team.AvatarUrl,
            Count = (await storage.ListAsync(path, cancellationToken: token)).Count
        };
    }
}
