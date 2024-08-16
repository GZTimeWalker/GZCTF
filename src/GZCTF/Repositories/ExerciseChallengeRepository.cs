using GZCTF.Models.Request.Edit;
using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class ExerciseChallengeRepository(AppDbContext context, IFileRepository fileRepository)
    : RepositoryBase(context),
        IExerciseChallengeRepository
{
    public async Task<ExerciseChallenge> CreateExercise(ExerciseChallenge exercise, CancellationToken token = default)
    {
        await Context.AddAsync(exercise, token);
        await SaveAsync(token);
        return exercise;
    }

    public Task<ExerciseChallenge[]> GetExercises(CancellationToken token = default) =>
        Context.ExerciseChallenges.OrderBy(e => e.Id).ToArrayAsync(token);

    public async Task RemoveExercise(ExerciseChallenge exercise, CancellationToken token = default)
    {
        await fileRepository.DeleteAttachment(exercise.Attachment, token);

        Context.Remove(exercise);
        await SaveAsync(token);
    }

    public async Task UpdateAttachment(ExerciseChallenge exercise, AttachmentCreateModel model,
        CancellationToken token = default)
    {
        var attachment = model.ToAttachment(await fileRepository.GetFileByHash(model.FileHash, token));

        await fileRepository.DeleteAttachment(exercise.Attachment, token);

        if (attachment is not null)
            await Context.AddAsync(attachment, token);

        exercise.Attachment = attachment;

        await SaveAsync(token);
    }
}
