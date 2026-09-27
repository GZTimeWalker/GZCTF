using GZCTF.Features.ChallengeLibrary.Domain;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Features.ChallengeRuntime.Application;

public sealed class ChallengeAccessPolicy(AppDbContext db)
{
    public Task<bool> CanAccessAsync(Guid challengeId, UserInfo user, CancellationToken token) =>
        user.Role == Role.Admin
            ? db.Challenges.AsNoTracking().AnyAsync(item => item.Id == challengeId, token)
            : db.Challenges.AsNoTracking().AnyAsync(item =>
                item.Id == challengeId &&
                item.PublicationState == ChallengePublicationState.Published &&
                item.IsEnabled, token);
}
