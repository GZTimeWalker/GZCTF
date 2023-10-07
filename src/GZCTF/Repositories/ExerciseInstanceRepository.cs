using GZCTF.Models.Request.Edit;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Localization;

namespace GZCTF.Repositories;

public class ExerciseInstanceRepository(AppDbContext context,
    IContainerManager service,
    IContainerRepository containerRepository,
    ILogger<ExerciseInstanceRepository> logger,
    IStringLocalizer<Program> localizer
) : RepositoryBase(context),
    IExerciseInstanceRepository
{
    public async Task<ExerciseInstance?> GetInstance(UserInfo user, int exerciseId, CancellationToken token = default)
    {
        await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(token);

        ExerciseInstance? instance = await context.ExerciseInstances
            .Include(i => i.FlagContext)
            .Where(e => e.ExerciseId == exerciseId && e.UserId == user.Id)
            .SingleOrDefaultAsync(token);

        if (instance is null)
            // we assume that the user has no permission to access the challenge
            return null;

        if (instance.IsLoaded)
        {
            await transaction.CommitAsync(token);
            return instance;
        }

        var exercise = instance.Exercise;

        if (exercise is null || !exercise.IsEnabled)
        {
            await transaction.CommitAsync(token);
            return null;
        }

        try
        {
            // dynamic flag dispatch
            if (instance.Exercise.Type == ChallengeType.DynamicContainer)
            {
                instance.FlagContext = new()
                {
                    Exercise = exercise,
                    // tiny probability will produce the same FLAG,
                    // but this will not affect the correctness of the answer
                    Flag = exercise.GenerateDynamicFlag(),
                    IsOccupied = true
                };
            }

            // instance.FlagContext is null by default
            // static flag does not need to be dispatched

            instance.IsLoaded = true;
            await SaveAsync(token);
            await transaction.CommitAsync(token);
        }
        catch
        {
            logger.SystemLog(localizer[nameof(Resources.Program.InstanceRepository_GetInstanceFailed), user.UserName!, exercise.Title, exercise.Id],
                TaskStatus.Failed, LogLevel.Warning);
            await transaction.RollbackAsync(token);
            return null;
        }

        return instance;
    }
    public Task<TaskResult<Container>> CreateContainer(ExerciseInstance instance, UserInfo user, int containerLimit = 3, CancellationToken token = default) => throw new NotImplementedException();
    public Task<ExerciseInstance[]> GetExerciseInstances(UserInfo user, CancellationToken token = default) => throw new NotImplementedException();
    public Task ProlongContainer(Container container, TimeSpan time, CancellationToken token = default) => throw new NotImplementedException();
    public Task<VerifyResult> VerifyAnswer(string answer, CancellationToken token = default) => throw new NotImplementedException();
}
