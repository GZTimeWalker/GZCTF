using GZCTF.Models.Request.Edit;
using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class ExerciseInstanceRepository(AppDbContext context) : RepositoryBase(context),
    IExerciseInstanceRepository
{
    public Task<TaskResult<Container>> CreateContainer(ExerciseInstance instance, UserInfo user, int containerLimit = 3, CancellationToken token = default) => throw new NotImplementedException();
    public Task<bool> DestroyContainer(Container container, CancellationToken token = default) => throw new NotImplementedException();
    public Task<ExerciseInstance[]> GetExerciseInstances(UserInfo user, CancellationToken token = default) => throw new NotImplementedException();
    public Task ProlongContainer(Container container, TimeSpan time, CancellationToken token = default) => throw new NotImplementedException();
    public Task<VerifyResult> VerifyAnswer(string answer, CancellationToken token = default) => throw new NotImplementedException();
}
