using System.ComponentModel.DataAnnotations;
using FluentStorage;
using FluentStorage.Blobs;

namespace GZCTF.Models.Request.Game;

public class ChallengeTrafficModel
{
    /// <summary>
    /// Challenge ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Challenge title
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_TitleRequired),
        ErrorMessageResourceType = typeof(Resources.Program))]
    [MinLength(1, ErrorMessageResourceName = nameof(Resources.Program.Model_TitleTooShort),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Challenge category
    /// </summary>
    public ChallengeCategory Category { get; set; } = ChallengeCategory.Misc;

    /// <summary>
    /// Challenge type
    /// </summary>
    public ChallengeType Type { get; set; } = ChallengeType.StaticAttachment;

    /// <summary>
    /// Is the challenge enabled
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Number of team traffic captured by the challenge
    /// </summary>
    public int Count { get; set; }

    internal static async Task<ChallengeTrafficModel> FromChallengeAsync(GameChallenge chal, IBlobStorage storage,
        CancellationToken token)
    {
        var path = StoragePath.Combine(PathHelper.Capture, chal.Id.ToString());

        return new()
        {
            Id = chal.Id,
            Title = chal.Title,
            Category = chal.Category,
            Type = chal.Type,
            IsEnabled = chal.IsEnabled,
            Count = (await storage.ListAsync(path, cancellationToken: token)).Count
        };
    }
}
