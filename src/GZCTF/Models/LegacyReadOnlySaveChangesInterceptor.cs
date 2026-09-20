using GZCTF.Models.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GZCTF.Models;

public sealed class LegacyReadOnlySaveChangesInterceptor : SaveChangesInterceptor
{
    private static readonly HashSet<Type> LegacyTypes =
    [
        typeof(Game), typeof(Team), typeof(GameChallenge), typeof(GameInstance),
        typeof(GameEvent), typeof(GameNotice), typeof(Submission), typeof(Participation),
        typeof(FirstSolve), typeof(ExerciseChallenge), typeof(ExerciseInstance),
        typeof(ExerciseDependency), typeof(UserParticipation), typeof(Division),
        typeof(CheatInfo), typeof(FlagContext)
    ];

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        RejectWrites(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        RejectWrites(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void RejectWrites(DbContext? context)
    {
        if (context?.ChangeTracker.Entries().Any(entry =>
                LegacyTypes.Contains(entry.Metadata.ClrType) &&
                entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted) == true)
            throw new InvalidOperationException("legacy_data_read_only");
    }
}
