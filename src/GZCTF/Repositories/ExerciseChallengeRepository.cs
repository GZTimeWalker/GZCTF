using GZCTF.Models.Request.Edit;
using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class ExerciseChallengeRepository(AppDbContext context, IFileRepository fileRepository) : RepositoryBase(context),
    IExerciseChallengeRepository
{
    public Task<ExerciseChallenge> CreateExercise(ExerciseChallenge exercise, CancellationToken token = default) => throw new NotImplementedException();
    public Task<ExerciseChallenge[]> GetExercises(CancellationToken token = default) => throw new NotImplementedException();
    public Task RemoveExercise(ExerciseChallenge exercise, CancellationToken token = default) => throw new NotImplementedException();
    public Task UpdateAttachment(ExerciseChallenge exercise, AttachmentCreateModel model, CancellationToken token = default) => throw new NotImplementedException();
}
